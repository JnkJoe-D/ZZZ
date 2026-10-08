using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using ATEditor;
using ATEditor.Editor;
using Game.GamePlay;

namespace Game.Editor.ActionTransition
{
    public enum WorkbenchPreviewMode
    {
        Normal,         // 常规预览模式 (默认)
        SimulateInput   // 模拟输入模式
    }

    /// <summary>
    /// 动作过渡可视化工作台 (Action Transition Workbench)。
    /// 复合 UI Toolkit 架构：
    /// - 角色工作区体系：深度对接 ATEditor 角色数据库，切换工作区自动换 Prefab 模型与角色动作候选池
    /// - 左侧：工作区管理、源动作快捷选择/拖拽、预览模型装配、拓扑目标白名单列表（实时搜索过滤）
    /// - 右侧：3D 角色视口 (自适应双层地坪网格、镜头跟随、复位模型)、播放控制栏、双轨联动时间轴（带局部裁剪保护、精致对称手柄）、过渡参数微调与反向保存
    /// </summary>
    public sealed class ActionTransitionWorkbenchWindow : EditorWindow
    {
        [MenuItem("Tools/Action/动作过渡工作台", priority = 300)]
        public static void OpenWindow()
        {
            var window = GetWindow<ActionTransitionWorkbenchWindow>("动作过渡工作台");
            window.minSize = new Vector2(850, 620);
            window.Show();
        }

        // 核心服务与视口
        private ActionTransitionPreviewViewport _viewport;
        private ActionTransitionTimelineDrawer _timelineDrawer;

        // 工作区与工作数据
        private ATWorkspaceDefinition _activeWorkspace;
        private List<ActionConfigAsset> _workspaceActions = new();
        private ActionConfigAsset _sourceAction;
        private GameObject _previewPrefab;
        private List<TransitionTargetInfo> _allTargets = new();
        private List<TransitionTargetInfo> _filteredTargets = new();
        private int _selectedTargetIndex = -1;
        private string _searchFilter = string.Empty;

        // 当前选中的目标配置数据缓存
        private ActionClipInfoExtractor.ExtractedClipData _sourceClipData;
        private ActionClipInfoExtractor.ExtractedClipData _targetClipData;
        private ActionClipInfoExtractor.RouteWindowRange _currentRouteWindow;

        private float _currentExitTime = 0.5f;
        private float _currentCrossfade = 0.2f;
        private bool _hasCustomExitTime = false;
        private float _customExitTimeValue = 0f;
        private bool _hasStartTime = false;
        private float _startTimeValue = 0f;

        // AutoRouteWindow 同步覆写 UI 元素
        private VisualElement _autoRouteSyncContainer;
        private Toggle _autoRouteSyncToggle;
        private VisualElement _autoRouteWindowPopupContainer;
        private PopupField<string> _autoRouteWindowPopup;
        private VisualElement _autoRoutePropertyPopupContainer;
        private PopupField<string> _autoRoutePropertyPopup;

        private List<RouteWindowClip> _availableAutoRouteClips = new();
        private int _selectedAutoRouteClipIndex = -1;
        private WindowTimingTarget _selectedTimingTarget = WindowTimingTarget.StartTime;

        // UI Toolkit 元素引用
        private PopupField<string> _workspacePopup;
        private PopupField<string> _actionQuickSelectPopup;
        private ToolbarSearchField _searchField;
        private ScrollView _targetListContainer;
        private IMGUIContainer _viewportContainer;
        private IMGUIContainer _timelineContainer;

        // 控制栏元素
        private WorkbenchPreviewMode _previewMode = WorkbenchPreviewMode.Normal;
        private Button _previewModeBtn;
        private Button _previewChannelBtn;
        private Button _playPauseBtn;
        private Button _restartBtn;
        private Button _speedMenuBtn;
        private Button _rulerFpsMenuBtn;
        private float _currentPlaybackSpeed = 1.0f;
        private Toggle _loopFocusToggle;
        private Label _timeDisplayLabel;

        // N-Panel 侧边收纳组件
        private VisualElement _npanelContainer;
        private VisualElement _npanelContent;
        private Button _npanelTabBtn;
        private bool _isNPanelExpanded = true;
        private const string PrefsKeyNPanelExpanded = "AT_Workbench_NPanel_Expanded";

        // 属性微调元素
        private Label _targetHeaderLabel;
        private Slider _crossfadeSlider;
        private FloatField _crossfadeField;
        private Button _resetDefaultFadeBtn;

        private Toggle _customExitToggle;
        private PopupField<string> _exitUnitPopup;
        private MiniSliderUnit _exitUnit = MiniSliderUnit.Seconds;
        private IMGUIContainer _exitMiniSliderContainer;
        private ActionTransitionMiniSlider _exitMiniSlider;
        private FloatField _customExitField;

        private Toggle _startTimeToggle;
        private PopupField<string> _startUnitPopup;
        private MiniSliderUnit _startUnit = MiniSliderUnit.Seconds;
        private IMGUIContainer _startMiniSliderContainer;
        private ActionTransitionMiniSlider _startMiniSlider;
        private FloatField _startTimeField;

        private Button _saveBtn;

        private static readonly List<string> UnitChoices = new List<string> { "秒 (s)", "帧 (f)", "归一化 (0~1)" };

        private double _lastUpdateTime;

        private void OnEnable()
        {
            _isNPanelExpanded = EditorPrefs.GetBool(PrefsKeyNPanelExpanded, true);

            _viewport = new ActionTransitionPreviewViewport();
            _timelineDrawer = new ActionTransitionTimelineDrawer();

            _exitMiniSlider = new ActionTransitionMiniSlider
            {
                Theme = MiniSliderTheme.Source,
                Unit = MiniSliderUnit.Seconds,
                Fps = 60,
                Duration = 1.0f,
                CurrentTime = 0f,
                IsEnabled = false
            };
            _exitMiniSlider.OnValueChanged = (val) => ApplyExitTimeChange(val);

            _startMiniSlider = new ActionTransitionMiniSlider
            {
                Theme = MiniSliderTheme.Target,
                Unit = MiniSliderUnit.Seconds,
                Fps = 60,
                Duration = 1.0f,
                CurrentTime = 0f,
                IsEnabled = false
            };
            _startMiniSlider.OnValueChanged = (val) => ApplyStartTimeChange(val);

            // 绑定时间轴交互回调
            _timelineDrawer.OnCrossfadeChanged += HandleTimelineCrossfadeChanged;
            _timelineDrawer.OnExitTimeChanged += HandleTimelineExitTimeChanged;
            _timelineDrawer.OnScrubTimeChanged += HandleTimelineScrubChanged;

            if (_viewport != null)
            {
                _viewport.OnSimulateInputClicked += HandleSimulateInputClicked;
                if (_viewport.Player != null)
                {
                    _viewport.Player.OnLoopCycleReset += HandleLoopCycleReset;
                    _viewport.Player.OnPlaybackEnded += HandlePlaybackEnded;
                }
            }

            EditorApplication.update += OnEditorUpdate;

            // 尝试自动匹配选中或默认资源
            TryAutoSelectInitialAssets();

            // 构建 UI Toolkit
            BuildUI();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            if (_viewport != null)
            {
                _viewport.OnSimulateInputClicked -= HandleSimulateInputClicked;
                if (_viewport.Player != null)
                {
                    _viewport.Player.OnLoopCycleReset -= HandleLoopCycleReset;
                    _viewport.Player.OnPlaybackEnded -= HandlePlaybackEnded;
                }
            }

            if (_timelineDrawer != null)
            {
                _timelineDrawer.OnCrossfadeChanged -= HandleTimelineCrossfadeChanged;
                _timelineDrawer.OnExitTimeChanged -= HandleTimelineExitTimeChanged;
                _timelineDrawer.OnScrubTimeChanged -= HandleTimelineScrubChanged;
            }

            _viewport?.Dispose();
            _viewport = null;
        }

        private void TryAutoSelectInitialAssets()
        {
            var workspaces = ActionTransitionWorkspaceService.GetAllWorkspaces();

            if (Selection.activeObject is ActionConfigAsset selectedAction)
            {
                _sourceAction = selectedAction;
                _activeWorkspace = ActionTransitionWorkspaceService.InferWorkspace(_sourceAction);
            }

            if (_activeWorkspace == null && workspaces.Count > 0)
            {
                _activeWorkspace = workspaces[0];
            }

            if (_activeWorkspace != null)
            {
                if (_previewPrefab == null && _activeWorkspace.PreviewPrefab != null)
                {
                    _previewPrefab = _activeWorkspace.PreviewPrefab;
                }
                _workspaceActions = ActionTransitionWorkspaceService.GetActionsForWorkspace(_activeWorkspace);
                if (_sourceAction == null && _workspaceActions.Count > 0)
                {
                    _sourceAction = _workspaceActions[0];
                }
            }

            // 兜底找默认模型
            if (_previewPrefab == null)
            {
                string defaultPrefabPath = "Assets/Resources/Prefab/Role/Ellen/Avatar_Female_Size02_Ellen.prefab";
                _previewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(defaultPrefabPath);
            }

            if (_viewport != null && _previewPrefab != null)
            {
                _viewport.SetPreviewPrefab(_previewPrefab);
            }
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/ActionTransitionWorkbench/ActionTransitionWorkbench.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ActionTransitionWorkbench/ActionTransitionWorkbench.uxml");
            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
            }

            BindUI();
            RefreshTopologyTargets();
        }

        private void BindUI()
        {
            // 0. 角色工作区选择
            var manageWsBtn = rootVisualElement.Q<Button>("manage-ws-btn");
            if (manageWsBtn != null)
            {
                manageWsBtn.clicked += () => ActionTransitionWorkspaceService.OpenWorkspaceManager();
            }

            var wsContainer = rootVisualElement.Q<VisualElement>("workspace-popup-container");
            var allWs = ActionTransitionWorkspaceService.GetAllWorkspaces();
            List<string> wsChoices = new List<string>();
            int currentWsIndex = 0;
            for (int i = 0; i < allWs.Count; i++)
            {
                var w = allWs[i];
                string choice = $"[{w.Category}] {w.DisplayName}";
                wsChoices.Add(choice);
                if (_activeWorkspace != null && _activeWorkspace.Id == w.Id)
                {
                    currentWsIndex = i;
                }
            }
            if (wsChoices.Count == 0) wsChoices.Add("(未配置工作区)");

            _workspacePopup = new PopupField<string>(wsChoices, currentWsIndex);
            _workspacePopup.RegisterValueChangedCallback(evt =>
            {
                int idx = _workspacePopup.index;
                if (idx >= 0 && idx < allWs.Count)
                {
                    SwitchWorkspace(allWs[idx], true);
                }
            });
            wsContainer?.Add(_workspacePopup);

            // 1. 源动作快捷选择下拉列表 (彻底移除手拖选择框)
            var actContainer = rootVisualElement.Q<VisualElement>("action-popup-container");
            List<string> actionChoices = GetWorkspaceActionChoices();
            int actionIndex = GetSelectedActionIndexInWorkspace();
            _actionQuickSelectPopup = new PopupField<string>(actionChoices, actionIndex);
            _actionQuickSelectPopup.RegisterValueChangedCallback(evt =>
            {
                int idx = _actionQuickSelectPopup.index;
                if (idx >= 0 && idx < _workspaceActions.Count)
                {
                    _sourceAction = _workspaceActions[idx];
                    RefreshTopologyTargets();
                }
            });
            actContainer?.Add(_actionQuickSelectPopup);

            // 2. 有效转移目标列表
            var refreshTargetsBtn = rootVisualElement.Q<Button>("refresh-targets-btn");
            if (refreshTargetsBtn != null)
            {
                refreshTargetsBtn.clicked += () => RefreshTopologyTargets();
            }

            _searchField = rootVisualElement.Q<ToolbarSearchField>("targets-search");
            _searchField?.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue;
                ApplySearchFilter();
            });

            _targetListContainer = rootVisualElement.Q<ScrollView>("targets-scrollview");

            // 3. 右上角 N-Panel 侧边收纳组件绑定
            _npanelContainer = rootVisualElement.Q<VisualElement>("npanel-container");
            _npanelContent = rootVisualElement.Q<VisualElement>("npanel-content");
            _npanelTabBtn = rootVisualElement.Q<Button>("npanel-tab-btn");
            if (_npanelTabBtn != null)
            {
                _npanelTabBtn.clicked += ToggleNPanelExpanded;
                UpdateNPanelVisualState();
            }

            // 过渡参数微调与保存面板
            _targetHeaderLabel = rootVisualElement.Q<Label>("inspector-header");

            _crossfadeSlider = rootVisualElement.Q<Slider>("crossfade-slider");
            _crossfadeSlider?.RegisterValueChangedCallback(evt =>
            {
                if (Mathf.Abs(evt.newValue - _currentCrossfade) > 0.001f)
                {
                    ApplyCrossfadeChange(evt.newValue);
                }
            });

            _crossfadeField = rootVisualElement.Q<FloatField>("crossfade-field");
            _crossfadeField?.RegisterValueChangedCallback(evt =>
            {
                float clamped = Mathf.Clamp(evt.newValue, 0f, 2.0f);
                if (Mathf.Abs(clamped - _currentCrossfade) > 0.001f)
                {
                    ApplyCrossfadeChange(clamped);
                }
            });

            _resetDefaultFadeBtn = rootVisualElement.Q<Button>("reset-fade-btn");
            if (_resetDefaultFadeBtn != null)
            {
                _resetDefaultFadeBtn.clicked += ResetToDefaultFade;
            }

            // EndTime (源动作退出时间)
            _customExitToggle = rootVisualElement.Q<Toggle>("custom-exit-toggle");
            _customExitToggle?.RegisterValueChangedCallback(evt =>
            {
                _hasCustomExitTime = evt.newValue;
                _customExitField?.SetEnabled(_hasCustomExitTime);
                _exitUnitPopup?.SetEnabled(_hasCustomExitTime);
                if (_exitMiniSlider != null) _exitMiniSlider.IsEnabled = _hasCustomExitTime;
                _exitMiniSliderContainer?.MarkDirtyRepaint();
                PersistCurrentTransition();
            });

            var exitUnitContainer = rootVisualElement.Q<VisualElement>("exit-unit-dropdown-container");
            _exitUnitPopup = new PopupField<string>(UnitChoices, (int)_exitUnit);
            _exitUnitPopup.RegisterValueChangedCallback(evt =>
            {
                _exitUnit = (MiniSliderUnit)_exitUnitPopup.index;
                if (_exitMiniSlider != null) _exitMiniSlider.Unit = _exitUnit;
                UpdateExitTimeFieldDisplay();
                _exitMiniSliderContainer?.MarkDirtyRepaint();
            });
            _exitUnitPopup.SetEnabled(false);
            exitUnitContainer?.Add(_exitUnitPopup);

            _exitMiniSliderContainer = rootVisualElement.Q<IMGUIContainer>("exit-mini-slider");
            if (_exitMiniSliderContainer != null)
            {
                _exitMiniSliderContainer.onGUIHandler = () =>
                {
                    if (_exitMiniSlider != null)
                    {
                        float dur = _sourceClipData.TimelineDuration > 0.001f ? _sourceClipData.TimelineDuration : 1f;
                        _exitMiniSlider.Duration = dur;
                        _exitMiniSlider.Fps = _timelineDrawer != null ? _timelineDrawer.CurrentFps : 60;
                        _exitMiniSlider.CurrentTime = _currentExitTime;
                        _exitMiniSlider.IsEnabled = _hasCustomExitTime && HasValidTarget();
                        _exitMiniSlider.Draw(_exitMiniSliderContainer.contentRect);
                    }
                };
            }

            _customExitField = rootVisualElement.Q<FloatField>("custom-exit-field");
            if (_customExitField != null)
            {
                _customExitField.SetEnabled(false);
                _customExitField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        CommitExitTimeFromField();
                        evt.StopPropagation();
                    }
                });
                _customExitField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    CommitExitTimeFromField();
                });
            }

            // StartTime (目标动作切入时间)
            _startTimeToggle = rootVisualElement.Q<Toggle>("start-time-toggle");
            _startTimeToggle?.RegisterValueChangedCallback(evt =>
            {
                _hasStartTime = evt.newValue;
                _startTimeField?.SetEnabled(_hasStartTime);
                _startUnitPopup?.SetEnabled(_hasStartTime);
                if (_startMiniSlider != null) _startMiniSlider.IsEnabled = _hasStartTime;
                _startMiniSliderContainer?.MarkDirtyRepaint();

                if (_viewport?.Player != null)
                {
                    _viewport.Player.HasStartTime = _hasStartTime;
                    _viewport.Player.EvaluateAt(_viewport.Player.CurrentTime);
                }
                PersistCurrentTransition();
                _timelineContainer?.MarkDirtyRepaint();
            });

            var startUnitContainer = rootVisualElement.Q<VisualElement>("start-unit-dropdown-container");
            _startUnitPopup = new PopupField<string>(UnitChoices, (int)_startUnit);
            _startUnitPopup.RegisterValueChangedCallback(evt =>
            {
                _startUnit = (MiniSliderUnit)_startUnitPopup.index;
                if (_startMiniSlider != null) _startMiniSlider.Unit = _startUnit;
                UpdateStartTimeFieldDisplay();
                _startMiniSliderContainer?.MarkDirtyRepaint();
            });
            _startUnitPopup.SetEnabled(false);
            startUnitContainer?.Add(_startUnitPopup);

            _startMiniSliderContainer = rootVisualElement.Q<IMGUIContainer>("start-mini-slider");
            if (_startMiniSliderContainer != null)
            {
                _startMiniSliderContainer.onGUIHandler = () =>
                {
                    if (_startMiniSlider != null)
                    {
                        float dur = _targetClipData.TimelineDuration > 0.001f ? _targetClipData.TimelineDuration : 1f;
                        _startMiniSlider.Duration = dur;
                        _startMiniSlider.Fps = _timelineDrawer != null ? _timelineDrawer.CurrentFps : 60;
                        _startMiniSlider.CurrentTime = _startTimeValue;
                        _startMiniSlider.IsEnabled = _hasStartTime && HasValidTarget();
                        _startMiniSlider.Draw(_startMiniSliderContainer.contentRect);
                    }
                };
            }

            _startTimeField = rootVisualElement.Q<FloatField>("start-time-field");
            if (_startTimeField != null)
            {
                _startTimeField.SetEnabled(false);
                _startTimeField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        CommitStartTimeFromField();
                        evt.StopPropagation();
                    }
                });
                _startTimeField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    CommitStartTimeFromField();
                });
            }

            // 4. AutoRouteWindow 同步覆写面板初始化
            _autoRouteSyncContainer = rootVisualElement.Q<VisualElement>("autoroute-sync-container");
            _autoRouteSyncToggle = rootVisualElement.Q<Toggle>("autoroute-sync-toggle");
            _autoRouteWindowPopupContainer = rootVisualElement.Q<VisualElement>("autoroute-window-popup-container");
            _autoRoutePropertyPopupContainer = rootVisualElement.Q<VisualElement>("autoroute-property-popup-container");

            if (_autoRouteSyncToggle != null)
            {
                _autoRouteSyncToggle.RegisterValueChangedCallback(evt =>
                {
                    bool enabled = evt.newValue;
                    _autoRouteWindowPopup?.SetEnabled(enabled);
                    _autoRoutePropertyPopup?.SetEnabled(enabled);
                });
            }

            // 初始化覆写目标下拉框
            List<string> propChoices = new List<string> { "StartTime (进入时刻)", "EndTime (退出时刻)" };
            _autoRoutePropertyPopup = new PopupField<string>(propChoices, 0);
            _autoRoutePropertyPopup.RegisterValueChangedCallback(evt =>
            {
                _selectedTimingTarget = evt.newValue.StartsWith("StartTime") ? WindowTimingTarget.StartTime : WindowTimingTarget.EndTime;
            });
            _autoRoutePropertyPopupContainer?.Add(_autoRoutePropertyPopup);

            // 初始化窗口选择下拉框
            _autoRouteWindowPopup = new PopupField<string>(new List<string> { "(未检测到窗口)" }, 0);
            _autoRouteWindowPopup.RegisterValueChangedCallback(evt =>
            {
                if (_autoRouteWindowPopup != null && _availableAutoRouteClips.Count > 0)
                {
                    _selectedAutoRouteClipIndex = Mathf.Clamp(_autoRouteWindowPopup.index, 0, _availableAutoRouteClips.Count - 1);
                }
            });
            _autoRouteWindowPopupContainer?.Add(_autoRouteWindowPopup);

            _saveBtn = rootVisualElement.Q<Button>("save-transition-btn");
            if (_saveBtn != null)
            {
                _saveBtn.clicked += SaveTransitionAssets;
            }

            // 4. 右侧主区域：3D 视口容器
            _viewportContainer = rootVisualElement.Q<IMGUIContainer>("viewport-container");
            if (_viewportContainer != null)
            {
                _viewportContainer.onGUIHandler = () =>
                {
                    Rect r = _viewportContainer.contentRect;
                    _viewport?.OnGUI(r);
                };
            }

            // 5. 播放控制条
            _playPauseBtn = rootVisualElement.Q<Button>("play-pause-btn");
            if (_playPauseBtn != null) _playPauseBtn.clicked += TogglePlayPause;

            _restartBtn = rootVisualElement.Q<Button>("restart-btn");
            if (_restartBtn != null) _restartBtn.clicked += RestartPlay;

            _previewModeBtn = rootVisualElement.Q<Button>("preview-mode-btn");
            if (_previewModeBtn != null) _previewModeBtn.clicked += ShowPreviewModeMenu;

            _previewChannelBtn = rootVisualElement.Q<Button>("preview-channel-btn");
            if (_previewChannelBtn != null) _previewChannelBtn.clicked += ShowPreviewChannelMenu;

            _speedMenuBtn = rootVisualElement.Q<Button>("speed-menu-btn");
            if (_speedMenuBtn != null) _speedMenuBtn.clicked += ShowSpeedMenu;

            _rulerFpsMenuBtn = rootVisualElement.Q<Button>("ruler-fps-btn");
            if (_rulerFpsMenuBtn != null) _rulerFpsMenuBtn.clicked += ShowRulerFpsMenu;

            _timeDisplayLabel = rootVisualElement.Q<Label>("time-display-label");

            var followCamToggle = rootVisualElement.Q<Toggle>("follow-cam-toggle");
            if (followCamToggle != null)
            {
                followCamToggle.value = _viewport?.CameraFollowCharacter ?? true;
                followCamToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_viewport != null) _viewport.CameraFollowCharacter = evt.newValue;
                });
            }

            var loopPlaybackToggle = rootVisualElement.Q<Toggle>("loop-playback-toggle");
            if (loopPlaybackToggle != null)
            {
                loopPlaybackToggle.value = _viewport?.Player?.IsLooping ?? true;
                loopPlaybackToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_viewport?.Player != null) _viewport.Player.IsLooping = evt.newValue;
                });
            }

            var resetOnLoopToggle = rootVisualElement.Q<Toggle>("reset-on-loop-toggle");
            if (resetOnLoopToggle != null)
            {
                resetOnLoopToggle.value = _viewport?.Player?.ResetPoseOnLoop ?? true;
                resetOnLoopToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_viewport?.Player != null) _viewport.Player.ResetPoseOnLoop = evt.newValue;
                });
            }

            _loopFocusToggle = rootVisualElement.Q<Toggle>("loop-focus-toggle");
            if (_loopFocusToggle != null)
            {
                _loopFocusToggle.value = false;
                _loopFocusToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_viewport?.Player != null)
                    {
                        _viewport.Player.IsLoopFocus = evt.newValue;
                        if (evt.newValue) _viewport.Player.ResetToStart();
                    }
                });
            }

            // 6. 双轨时间轴容器
            _timelineContainer = rootVisualElement.Q<IMGUIContainer>("timeline-container");
            if (_timelineContainer != null)
            {
                _timelineContainer.onGUIHandler = () =>
                {
                    Rect r = _timelineContainer.contentRect;
                    DrawTimelineArea(r);
                };
            }
        }

        private List<string> GetWorkspaceActionChoices()
        {
            List<string> list = new List<string>();
            if (_workspaceActions == null || _workspaceActions.Count == 0)
            {
                list.Add("(当前工作区无动作)");
                return list;
            }

            foreach (var act in _workspaceActions)
            {
                list.Add(act != null ? act.name : "(null)");
            }
            return list;
        }

        private int GetSelectedActionIndexInWorkspace()
        {
            if (_sourceAction == null || _workspaceActions == null || _workspaceActions.Count == 0) return 0;
            int idx = _workspaceActions.IndexOf(_sourceAction);
            return idx >= 0 ? idx : 0;
        }

        private void UpdateActionQuickSelectChoices()
        {
            if (_actionQuickSelectPopup == null) return;
            var choices = GetWorkspaceActionChoices();
            _actionQuickSelectPopup.choices = choices;
            int idx = GetSelectedActionIndexInWorkspace();
            _actionQuickSelectPopup.SetValueWithoutNotify(choices[Mathf.Clamp(idx, 0, choices.Count - 1)]);
        }

        private void SwitchWorkspace(ATWorkspaceDefinition ws, bool autoSelectFirstAction)
        {
            if (ws == null) return;
            _activeWorkspace = ws;

            // 1. 自动切换绑定模型
            if (ws.PreviewPrefab != null)
            {
                _previewPrefab = ws.PreviewPrefab;
                _viewport?.SetPreviewPrefab(_previewPrefab);
                _viewport?.ResetCharacterTransform();
            }

            // 2. 扫描加载当前角色的动作资产
            _workspaceActions = ActionTransitionWorkspaceService.GetActionsForWorkspace(ws);
            UpdateActionQuickSelectChoices();

            // 3. 自动载入首个动作
            if (autoSelectFirstAction && _workspaceActions.Count > 0)
            {
                _sourceAction = _workspaceActions[0];
                RefreshTopologyTargets();
            }

            // 4. 同步工作区选择框
            var allWs = ActionTransitionWorkspaceService.GetAllWorkspaces();
            int wsIdx = -1;
            for (int i = 0; i < allWs.Count; i++)
            {
                if (allWs[i].Id == ws.Id) { wsIdx = i; break; }
            }
            if (wsIdx >= 0 && _workspacePopup != null && _workspacePopup.index != wsIdx)
            {
                _workspacePopup.SetValueWithoutNotify(_workspacePopup.choices[wsIdx]);
            }
        }



        private void ShowPreviewModeMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("常规预览模式 (默认)"), _previewMode == WorkbenchPreviewMode.Normal, () => SetPreviewMode(WorkbenchPreviewMode.Normal));
            menu.AddItem(new GUIContent("模拟输入模式"), _previewMode == WorkbenchPreviewMode.SimulateInput, () => SetPreviewMode(WorkbenchPreviewMode.SimulateInput));
            menu.DropDown(_previewModeBtn.worldBound);
        }

        private void SetPreviewMode(WorkbenchPreviewMode mode)
        {
            _previewMode = mode;
            if (_previewModeBtn != null)
            {
                _previewModeBtn.text = mode == WorkbenchPreviewMode.Normal ? "模式: 常规预览 ▾" : "模式: 模拟输入 ▾";
            }

            if (_viewport != null)
            {
                _viewport.IsSimulateInputMode = (mode == WorkbenchPreviewMode.SimulateInput);
            }

            if (mode == WorkbenchPreviewMode.SimulateInput)
            {
                // 为保证不出异常，切换时先暂停然后重置
                if (_viewport?.Player != null)
                {
                    _viewport.Player.Pause();
                    _viewport.Player.ResetToStart();
                }
                _viewport?.ResetCharacterTransform();
                if (_playPauseBtn != null) _playPauseBtn.text = "播放";

                // 不支持局部循环开关（如果打开则要关掉，并隐藏）
                if (_loopFocusToggle != null)
                {
                    _loopFocusToggle.value = false;
                    _loopFocusToggle.style.display = DisplayStyle.None;
                }
                if (_viewport?.Player != null)
                {
                    _viewport.Player.IsLoopFocus = false;
                }

                // 播放时间轴逻辑：默认只播原动作
                if (_viewport?.Player != null)
                {
                    _viewport.Player.IsSimulateInputMode = true;
                    _viewport.Player.HasSimulatedInputTriggered = false;
                    _viewport.Player.SetupClips(_sourceClipData, _targetClipData, _currentExitTime, _currentCrossfade);
                }
            }
            else
            {
                // 恢复常规预览模式：重新显示局部循环开关
                if (_loopFocusToggle != null)
                {
                    _loopFocusToggle.style.display = DisplayStyle.Flex;
                }
                if (_viewport?.Player != null)
                {
                    _viewport.Player.IsSimulateInputMode = false;
                    _viewport.Player.HasSimulatedInputTriggered = false;
                    _viewport.Player.SetupClips(_sourceClipData, _targetClipData, _currentExitTime, _currentCrossfade);
                }
            }

            _viewportContainer?.MarkDirtyRepaint();
            _timelineContainer?.MarkDirtyRepaint();
            UpdateTimeDisplayLabel();
        }

        private void HandleSimulateInputClicked()
        {
            if (_previewMode != WorkbenchPreviewMode.SimulateInput || _viewport?.Player == null)
                return;

            // 需求 1：处于播放目标动作时不允许响应（只有当前时间指针处于原动作时间内才允许响应）
            if (_viewport.Player.HasSimulatedInputTriggered)
                return;
            if (_viewport.Player.CurrentTime >= _sourceClipData.TimelineDuration)
                return;

            // 1. 若处于暂停状态，自动切为播放状态
            if (!_viewport.Player.IsPlaying)
            {
                _viewport.Player.Play();
                if (_playPauseBtn != null) _playPauseBtn.text = "暂停";
            }

            // 2. 获取当前时间轴指针时间
            float clickTime = _viewport.Player.CurrentTime;

            // 3. 将过渡区域左边界 ExitTime 自动设置为接收到回调时的时间轴指针时间
            _currentExitTime = clickTime;
            _hasCustomExitTime = true;
            _customExitTimeValue = _currentExitTime;

            // 4. 保持时间轴与左边界、右边界与左边界的距离不变（Crossfade 保持不变），在此刻自动开始衔接
            _viewport.Player.TriggerSimulatedInput(clickTime);

            // 5. 同步 UI 与持久化
            _customExitToggle?.SetValueWithoutNotify(true);
            _customExitField?.SetEnabled(true);
            _exitUnitPopup?.SetEnabled(true);
            if (_exitMiniSlider != null)
            {
                _exitMiniSlider.IsEnabled = true;
                _exitMiniSlider.CurrentTime = _currentExitTime;
            }
            _exitMiniSliderContainer?.MarkDirtyRepaint();
            UpdateExitTimeFieldDisplay();
            PersistCurrentTransition();

            _timelineContainer?.MarkDirtyRepaint();
            _viewportContainer?.MarkDirtyRepaint();
            UpdateTimeDisplayLabel();
        }

        private void ShowPreviewChannelMenu()
        {
            GenericMenu menu = new GenericMenu();
            var cur = _viewport?.Player != null ? _viewport.Player.ActiveChannel : PreviewChannel.All;

            menu.AddItem(new GUIContent("全部 (双轨过渡混合)"), cur == PreviewChannel.All, () => SetPreviewChannel(PreviewChannel.All));
            menu.AddItem(new GUIContent("仅源动作 (Solo Source)"), cur == PreviewChannel.SourceOnly, () => SetPreviewChannel(PreviewChannel.SourceOnly));
            menu.AddItem(new GUIContent("仅目标动作 (Solo Target)"), cur == PreviewChannel.TargetOnly, () => SetPreviewChannel(PreviewChannel.TargetOnly));

            menu.DropDown(_previewChannelBtn.worldBound);
        }

        private void SetPreviewChannel(PreviewChannel channel)
        {
            if (_viewport?.Player == null) return;
            _viewport.Player.ActiveChannel = channel;

            if (_previewChannelBtn != null)
            {
                _previewChannelBtn.text = channel switch
                {
                    PreviewChannel.SourceOnly => "通道: 仅源动作 ▾",
                    PreviewChannel.TargetOnly => "通道: 仅目标动作 ▾",
                    _ => "通道: 全部 ▾"
                };
            }

            _timelineContainer?.MarkDirtyRepaint();
            _viewportContainer?.MarkDirtyRepaint();
            UpdateTimeDisplayLabel();
        }

        private void ShowSpeedMenu()
        {
            GenericMenu menu = new GenericMenu();
            float[] speedValues = new float[] { 0.10f, 0.25f, 0.50f, 1.00f, 1.50f, 2.00f };
            for (int i = 0; i < speedValues.Length; i++)
            {
                float spd = speedValues[i];
                bool isSelected = Mathf.Approximately(_currentPlaybackSpeed, spd);
                menu.AddItem(new GUIContent($"{spd:0.00}x"), isSelected, () =>
                {
                    _currentPlaybackSpeed = spd;
                    if (_speedMenuBtn != null) _speedMenuBtn.text = $"倍速: {spd:0.00}x ▾";
                    if (_viewport?.Player != null)
                    {
                        _viewport.Player.PlaybackSpeed = spd;
                    }
                });
            }
            menu.DropDown(_speedMenuBtn.worldBound);
        }

        private void ShowRulerFpsMenu()
        {
            GenericMenu menu = new GenericMenu();
            TimelineRulerUnit currentUnit = _timelineDrawer != null ? _timelineDrawer.RulerUnit : TimelineRulerUnit.Frames;
            TimelineFpsMode currentFps = _timelineDrawer != null ? _timelineDrawer.FpsMode : TimelineFpsMode.Fps60;

            // 60 FPS (帧模式 - 默认)
            bool is60 = currentUnit == TimelineRulerUnit.Frames && currentFps == TimelineFpsMode.Fps60;
            menu.AddItem(new GUIContent("帧模式: 60 FPS (默认)"), is60, () => SetTimelineMode(TimelineRulerUnit.Frames, TimelineFpsMode.Fps60));

            // 30 FPS (帧模式)
            bool is30 = currentUnit == TimelineRulerUnit.Frames && currentFps == TimelineFpsMode.Fps30;
            menu.AddItem(new GUIContent("帧模式: 30 FPS"), is30, () => SetTimelineMode(TimelineRulerUnit.Frames, TimelineFpsMode.Fps30));

            // 15 FPS (帧模式)
            bool is15 = currentUnit == TimelineRulerUnit.Frames && currentFps == TimelineFpsMode.Fps15;
            menu.AddItem(new GUIContent("帧模式: 15 FPS"), is15, () => SetTimelineMode(TimelineRulerUnit.Frames, TimelineFpsMode.Fps15));

            menu.AddSeparator("");

            // 秒模式
            bool isSec = currentUnit == TimelineRulerUnit.Seconds;
            menu.AddItem(new GUIContent("秒模式: 秒 (s)"), isSec, () => SetTimelineMode(TimelineRulerUnit.Seconds, currentFps));

            menu.DropDown(_rulerFpsMenuBtn.worldBound);
        }

        private void SetTimelineMode(TimelineRulerUnit unit, TimelineFpsMode fps)
        {
            if (_timelineDrawer != null)
            {
                _timelineDrawer.RulerUnit = unit;
                _timelineDrawer.FpsMode = fps;
            }
            if (_rulerFpsMenuBtn != null)
            {
                _rulerFpsMenuBtn.text = unit == TimelineRulerUnit.Frames 
                    ? $"标尺: 帧 ({(int)fps} FPS) ▾" 
                    : "标尺: 秒 (s) ▾";
            }
            _timelineContainer?.MarkDirtyRepaint();
            UpdateTimeDisplayLabel();
        }



        private void DrawTimelineArea(Rect rect)
        {
            if (_timelineDrawer == null) return;

            string srcName = ActionTransitionTopologyService.GetActionDisplayName(_sourceAction);
            float srcDur = _sourceClipData.TimelineDuration;

            string tgtName = "";
            float tgtDur = 1.0f;
            if (HasValidTarget())
            {
                var targetInfo = _filteredTargets[_selectedTargetIndex];
                tgtName = ActionTransitionTopologyService.GetActionDisplayName(targetInfo.TargetAction);
                tgtDur = _targetClipData.TimelineDuration;
            }

            float currentTime = _viewport?.Player != null ? _viewport.Player.CurrentTime : 0f;
            bool isFocus = _viewport?.Player != null && _viewport.Player.IsLoopFocus;
            float focusStart = _viewport?.Player != null ? _viewport.Player.FocusStartTime : 0f;
            float focusEnd = _viewport?.Player != null ? _viewport.Player.FocusEndTime : 1f;

            _timelineDrawer.DrawTimeline(
                rect,
                srcName,
                srcDur,
                _currentRouteWindow,
                tgtName,
                tgtDur,
                _currentExitTime,
                _currentCrossfade,
                currentTime,
                isFocus,
                focusStart,
                focusEnd,
                _hasStartTime,
                _startTimeValue
            );

            // 当发生滚轮缩放、横轴滚动或手柄平移拖拽时，主动标记重绘以保证时间轴交互极其平滑
            Event evt = Event.current;
            if (evt.type == EventType.ScrollWheel || evt.type == EventType.MouseDrag)
            {
                _timelineContainer?.MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// 扫描并整理源动作的所有合法转移目标白名单，并按字母序智能分类
        /// </summary>
        public void RefreshTopologyTargets()
        {
            _allTargets.Clear();
            _filteredTargets.Clear();
            _selectedTargetIndex = -1;

            if (_sourceAction == null)
            {
                _targetListContainer.Clear();
                Label emptyLabel = new Label("请先拖入或选择 Source Action");
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.marginTop = 10;
                emptyLabel.style.marginBottom = 10;
                emptyLabel.style.marginLeft = 10;
                emptyLabel.style.marginRight = 10;
                _targetListContainer.Add(emptyLabel);
                UpdateInspectorTargetDisplay();
                return;
            }

            _sourceClipData = ActionClipInfoExtractor.Extract(_sourceAction);

            // 调用拓扑发现服务
            _allTargets = ActionTransitionTopologyService.DiscoverTargets(_sourceAction);

            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            _targetListContainer.Clear();
            _filteredTargets.Clear();

            if (_allTargets.Count == 0)
            {
                Label noTargetLabel = new Label("当前动作未配置任何派生路由或后续动作");
                noTargetLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                noTargetLabel.style.marginTop = 10;
                noTargetLabel.style.marginBottom = 10;
                noTargetLabel.style.marginLeft = 10;
                noTargetLabel.style.marginRight = 10;
                _targetListContainer.Add(noTargetLabel);
                UpdateInspectorTargetDisplay();
                return;
            }

            // 根据过滤关键词筛选
            for (int i = 0; i < _allTargets.Count; i++)
            {
                var info = _allTargets[i];
                string displayName = ActionTransitionTopologyService.GetActionDisplayName(info.TargetAction);
                if (string.IsNullOrEmpty(_searchFilter) || displayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _filteredTargets.Add(info);
                }
            }

            if (_filteredTargets.Count == 0)
            {
                Label emptySearchLabel = new Label($"未找到匹配 \"{_searchFilter}\" 的动作");
                emptySearchLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptySearchLabel.style.marginTop = 10;
                emptySearchLabel.style.marginBottom = 10;
                emptySearchLabel.style.marginLeft = 10;
                emptySearchLabel.style.marginRight = 10;
                _targetListContainer.Add(emptySearchLabel);
                UpdateInspectorTargetDisplay();
                return;
            }

            // 构建列表项
            for (int i = 0; i < _filteredTargets.Count; i++)
            {
                int index = i;
                var info = _filteredTargets[i];

                VisualElement itemElem = new VisualElement();
                itemElem.AddToClassList("target-item");

                // Badge
                Label badge = new Label();
                badge.AddToClassList("badge");
                switch (info.Kind)
                {
                    case TransitionTargetKind.RouteBranch:
                        badge.AddToClassList("badge-route");
                        badge.text = "Branch";
                        break;
                    case TransitionTargetKind.SelfLoop:
                        badge.AddToClassList("badge-loop");
                        badge.text = "Loop";
                        break;
                    case TransitionTargetKind.CompleteAction:
                        badge.AddToClassList("badge-complete");
                        badge.text = "Complete";
                        break;
                }
                itemElem.Add(badge);

                // 动作名称（使用名称自愈）
                string actionName = ActionTransitionTopologyService.GetActionDisplayName(info.TargetAction);
                Label nameLabel = new Label(actionName);
                nameLabel.AddToClassList("target-name");
                itemElem.Add(nameLabel);

                // 混合时长摘要
                float currentFade = _sourceAction.GetTransitionCrossfade(info.TargetAction);
                string fadeText = currentFade >= 0f ? $"{currentFade:0.00}s" : "Default";
                Label fadeLabel = new Label(fadeText);
                fadeLabel.AddToClassList("target-fade-summary");
                itemElem.Add(fadeLabel);

                itemElem.RegisterCallback<ClickEvent>(evt => SelectTargetIndex(index));
                _targetListContainer.Add(itemElem);
            }

            // 选中第一个有效目标
            SelectTargetIndex(0);
        }

        private void SelectTargetIndex(int index)
        {
            if (index < 0 || index >= _filteredTargets.Count)
            {
                _selectedTargetIndex = -1;
                UpdateInspectorTargetDisplay();
                return;
            }

            _selectedTargetIndex = index;

            for (int i = 0; i < _targetListContainer.childCount; i++)
            {
                var child = _targetListContainer.ElementAt(i);
                if (i == index) child.AddToClassList("target-item-selected");
                else child.RemoveFromClassList("target-item-selected");
            }

            var info = _filteredTargets[index];
            _targetClipData = ActionClipInfoExtractor.Extract(info.TargetAction);

            // 读取已配置参数
            var itemConfig = _sourceAction.GetTransition(info.TargetAction);
            if (itemConfig != null)
            {
                _currentCrossfade = itemConfig.BlendDuration >= 0f ? itemConfig.BlendDuration : _targetClipData.DefaultBlendIn;
                _hasCustomExitTime = itemConfig.HasEndTime;
                _customExitTimeValue = itemConfig.EndTime;
                _hasStartTime = itemConfig.HasStartTime;
                _startTimeValue = itemConfig.StartTime;
            }
            else
            {
                _currentCrossfade = _targetClipData.DefaultBlendIn;
                _hasCustomExitTime = false;
                _customExitTimeValue = 0f;
                _hasStartTime = false;
                _startTimeValue = 0f;
            }

            // ExitTime 与 RouteWindow
            if (info.Kind == TransitionTargetKind.RouteBranch)
            {
                _currentRouteWindow = ActionClipInfoExtractor.ExtractRouteWindow(_sourceAction, info.MatchedRoute);
                if (_currentRouteWindow.HasWindow)
                {
                    _currentExitTime = (_currentRouteWindow.StartTime + _currentRouteWindow.EndTime) * 0.5f;
                }
                else
                {
                    _currentExitTime = Mathf.Min(0.4f, _sourceClipData.TimelineDuration * 0.5f);
                }
            }
            else
            {
                _currentRouteWindow = default;
                if (_hasCustomExitTime && _customExitTimeValue > 0f)
                {
                    _currentExitTime = _customExitTimeValue;
                }
                else
                {
                    _currentExitTime = _sourceClipData.TimelineDuration;
                }
            }

            SyncPlayerClips();
            if (_viewport?.Player != null)
            {
                _viewport.Player.HasStartTime = _hasStartTime;
                _viewport.Player.StartTime = _startTimeValue;
            }
            _viewport?.ResetCharacterTransform();
            _viewportContainer?.MarkDirtyRepaint();
            UpdateInspectorTargetDisplay();
        }

        private void SyncPlayerClips()
        {
            if (_viewport?.Player == null) return;

            _viewport.Player.SetupClips(
                _sourceClipData,
                _targetClipData,
                _currentExitTime,
                _currentCrossfade
            );
        }

        private void UpdateInspectorTargetDisplay()
        {
            if (!HasValidTarget())
            {
                _targetHeaderLabel.text = "当前微调: 未选择目标";
                _crossfadeSlider.SetEnabled(false);
                _crossfadeField.SetEnabled(false);
                _resetDefaultFadeBtn.SetEnabled(false);
                _customExitToggle.SetEnabled(false);
                _customExitField.SetEnabled(false);
                _exitUnitPopup?.SetEnabled(false);
                if (_exitMiniSlider != null) _exitMiniSlider.IsEnabled = false;

                _startTimeToggle?.SetEnabled(false);
                _startTimeField?.SetEnabled(false);
                _startUnitPopup?.SetEnabled(false);
                if (_startMiniSlider != null) _startMiniSlider.IsEnabled = false;

                _saveBtn.SetEnabled(false);
                _autoRouteSyncContainer?.SetEnabled(false);
                _exitMiniSliderContainer?.MarkDirtyRepaint();
                _startMiniSliderContainer?.MarkDirtyRepaint();
                return;
            }

            var info = _filteredTargets[_selectedTargetIndex];
            string typeStr = info.Kind switch
            {
                TransitionTargetKind.RouteBranch => "派生分支",
                TransitionTargetKind.SelfLoop => "自身循环 (Loop)",
                TransitionTargetKind.CompleteAction => "自然顺承 (CompleteAction)",
                _ => "未知"
            };

            string targetDisplayName = ActionTransitionTopologyService.GetActionDisplayName(info.TargetAction);
            _targetHeaderLabel.text = $"微调: [{typeStr}] -> {targetDisplayName}";
            _targetHeaderLabel.tooltip = $"{typeStr} -> {targetDisplayName}";
            _crossfadeSlider.SetEnabled(true);
            _crossfadeField.SetEnabled(true);
            _resetDefaultFadeBtn.SetEnabled(true);
            _customExitToggle.SetEnabled(true);
            _startTimeToggle?.SetEnabled(true);
            _saveBtn.SetEnabled(true);

            _crossfadeSlider.SetValueWithoutNotify(_currentCrossfade);
            _crossfadeField.SetValueWithoutNotify(_currentCrossfade);
            _resetDefaultFadeBtn.text = $"恢复默认 ({_targetClipData.DefaultBlendIn:0.00}s)";

            if (info.Kind == TransitionTargetKind.CompleteAction)
            {
                _customExitToggle.text = "提前退出 (EndTime) [运行时]:";
            }
            else
            {
                _customExitToggle.text = "模拟按键 (EndTime) [预览]:";
            }

            _customExitToggle.SetValueWithoutNotify(_hasCustomExitTime);
            _customExitField.SetEnabled(_hasCustomExitTime);
            _exitUnitPopup?.SetEnabled(_hasCustomExitTime);
            _exitUnitPopup?.SetValueWithoutNotify(UnitChoices[(int)_exitUnit]);
            if (_exitMiniSlider != null)
            {
                _exitMiniSlider.IsEnabled = _hasCustomExitTime;
                _exitMiniSlider.CurrentTime = _currentExitTime;
                _exitMiniSlider.Unit = _exitUnit;
            }
            UpdateExitTimeFieldDisplay();

            if (_startTimeToggle != null)
            {
                _startTimeToggle.SetValueWithoutNotify(_hasStartTime);
                _startTimeField?.SetEnabled(_hasStartTime);
                _startUnitPopup?.SetEnabled(_hasStartTime);
                _startUnitPopup?.SetValueWithoutNotify(UnitChoices[(int)_startUnit]);
                if (_startMiniSlider != null)
                {
                    _startMiniSlider.IsEnabled = _hasStartTime;
                    _startMiniSlider.CurrentTime = _startTimeValue;
                    _startMiniSlider.Unit = _startUnit;
                }
                UpdateStartTimeFieldDisplay();
            }

            _exitMiniSliderContainer?.MarkDirtyRepaint();
            _startMiniSliderContainer?.MarkDirtyRepaint();

            // 联动刷新可覆写的 AutoRouteWindow 候选与智能推荐
            UpdateAutoRouteWindowChoices(info);
        }

        private void UpdateAutoRouteWindowChoices(TransitionTargetInfo info)
        {
            _availableAutoRouteClips.Clear();
            _selectedAutoRouteClipIndex = -1;

            if (_sourceClipData.TimelineSO != null)
            {
                _availableAutoRouteClips = _sourceClipData.TimelineSO.FindAutoRouteWindowClips();
            }

            if (_autoRouteSyncContainer == null) return;

            if (_availableAutoRouteClips.Count == 0)
            {
                _autoRouteSyncContainer.SetEnabled(false);
                _autoRouteSyncToggle?.SetValueWithoutNotify(false);
                if (_autoRouteWindowPopup != null)
                {
                    _autoRouteWindowPopup.choices = new List<string> { "(无可用 AutoRouteWindow)" };
                    _autoRouteWindowPopup.SetValueWithoutNotify("(无可用 AutoRouteWindow)");
                }
                return;
            }

            _autoRouteSyncContainer.SetEnabled(true);

            List<string> choices = new List<string>();
            int defaultMatchIndex = 0;
            string matchedTag = null;
            RouteSingleModifierCheckTiming? detectedTiming = null;

            if (info != null && info.Kind == TransitionTargetKind.RouteBranch && info.MatchedRoute != null)
            {
                if (info.MatchedRoute.TriggerStrategy is AutoTransitionTrigger autoTrig)
                {
                    matchedTag = autoTrig.RequiredWindow?.Tag;
                    detectedTiming = autoTrig.Timing;
                }
                else if (info.MatchedRoute.TriggerStrategy is ConditionOnlyTrigger condTrig)
                {
                    matchedTag = condTrig.RequiredWindow?.Tag;
                    detectedTiming = condTrig.Timing;
                }
            }

            for (int i = 0; i < _availableAutoRouteClips.Count; i++)
            {
                var clip = _availableAutoRouteClips[i];
                string tag = clip.routewindow != null ? clip.routewindow.Tag : "None";
                string itemText = $"[{tag}] ({clip.StartTime:0.00}s~{clip.EndTime:0.00}s)";
                choices.Add(itemText);

                if (!string.IsNullOrEmpty(matchedTag) && string.Equals(tag, matchedTag, StringComparison.OrdinalIgnoreCase))
                {
                    defaultMatchIndex = i;
                }
            }

            _selectedAutoRouteClipIndex = defaultMatchIndex;
            if (_autoRouteWindowPopup != null)
            {
                _autoRouteWindowPopup.choices = choices;
                _autoRouteWindowPopup.SetValueWithoutNotify(choices[defaultMatchIndex]);
            }

            // 智能根据 Trigger 的 Timing 决策默认覆写目标与是否开启同步
            if (detectedTiming == RouteSingleModifierCheckTiming.OnWindowExit)
            {
                _selectedTimingTarget = WindowTimingTarget.EndTime;
                _autoRoutePropertyPopup?.SetValueWithoutNotify("EndTime (退出时刻)");
                _autoRouteSyncToggle?.SetValueWithoutNotify(true);
            }
            else if (detectedTiming == RouteSingleModifierCheckTiming.OnWindowEnter)
            {
                _selectedTimingTarget = WindowTimingTarget.StartTime;
                _autoRoutePropertyPopup?.SetValueWithoutNotify("StartTime (进入时刻)");
                _autoRouteSyncToggle?.SetValueWithoutNotify(true);
            }
            else
            {
                // EveryFrameInWindow 无法决定也不该决定，默认不自动勾选同步，但允许用户手动开启
                _autoRouteSyncToggle?.SetValueWithoutNotify(false);
            }

            bool syncActive = _autoRouteSyncToggle?.value ?? false;
            _autoRouteWindowPopup?.SetEnabled(syncActive);
            _autoRoutePropertyPopup?.SetEnabled(syncActive);
        }

        private void ToggleNPanelExpanded()
        {
            _isNPanelExpanded = !_isNPanelExpanded;
            EditorPrefs.SetBool(PrefsKeyNPanelExpanded, _isNPanelExpanded);
            UpdateNPanelVisualState();
        }

        private void UpdateNPanelVisualState()
        {
            if (_npanelContent != null)
            {
                _npanelContent.style.display = _isNPanelExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_npanelTabBtn != null)
            {
                if (_isNPanelExpanded) _npanelTabBtn.AddToClassList("active");
                else _npanelTabBtn.RemoveFromClassList("active");
            }
        }

        private void CommitExitTimeFromField()
        {
            if (_customExitField == null) return;
            float rawValue = _customExitField.value;
            float dur = _sourceClipData.TimelineDuration > 0.001f ? _sourceClipData.TimelineDuration : 1f;
            int fps = _timelineDrawer != null ? _timelineDrawer.CurrentFps : 60;
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(dur * fps));

            float clampedTime;
            switch (_exitUnit)
            {
                case MiniSliderUnit.Frames:
                    int frame = Mathf.Clamp(Mathf.RoundToInt(rawValue), 0, totalFrames);
                    clampedTime = (float)frame / fps;
                    break;
                case MiniSliderUnit.Normalized:
                    float norm = Mathf.Clamp01(rawValue);
                    clampedTime = norm * dur;
                    break;
                case MiniSliderUnit.Seconds:
                default:
                    clampedTime = Mathf.Clamp(rawValue, 0f, dur);
                    break;
            }

            ApplyExitTimeChange(clampedTime);
        }

        private void CommitStartTimeFromField()
        {
            if (_startTimeField == null) return;
            float rawValue = _startTimeField.value;
            float tgtDur = _targetClipData.TimelineDuration > 0.001f ? _targetClipData.TimelineDuration : 1f;
            int fps = _timelineDrawer != null ? _timelineDrawer.CurrentFps : 60;
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(tgtDur * fps));

            float clampedTime;
            switch (_startUnit)
            {
                case MiniSliderUnit.Frames:
                    int frame = Mathf.Clamp(Mathf.RoundToInt(rawValue), 0, totalFrames);
                    clampedTime = (float)frame / fps;
                    break;
                case MiniSliderUnit.Normalized:
                    float norm = Mathf.Clamp01(rawValue);
                    clampedTime = norm * tgtDur;
                    break;
                case MiniSliderUnit.Seconds:
                default:
                    clampedTime = Mathf.Clamp(rawValue, 0f, Mathf.Max(0f, tgtDur - 0.05f));
                    break;
            }

            ApplyStartTimeChange(clampedTime);
        }

        private void UpdateExitTimeFieldDisplay()
        {
            if (_customExitField == null) return;
            float dur = _sourceClipData.TimelineDuration > 0.001f ? _sourceClipData.TimelineDuration : 1f;
            float val = _hasCustomExitTime ? _customExitTimeValue : _currentExitTime;
            int fps = _timelineDrawer != null ? _timelineDrawer.CurrentFps : 60;

            switch (_exitUnit)
            {
                case MiniSliderUnit.Frames:
                    int frame = Mathf.RoundToInt(val * fps);
                    _customExitField.SetValueWithoutNotify(frame);
                    break;
                case MiniSliderUnit.Normalized:
                    float norm = dur > 0.001f ? Mathf.Clamp01(val / dur) : 0f;
                    _customExitField.SetValueWithoutNotify((float)Math.Round(norm, 3));
                    break;
                case MiniSliderUnit.Seconds:
                default:
                    _customExitField.SetValueWithoutNotify((float)Math.Round(val, 3));
                    break;
            }
        }

        private void UpdateStartTimeFieldDisplay()
        {
            if (_startTimeField == null) return;
            float dur = _targetClipData.TimelineDuration > 0.001f ? _targetClipData.TimelineDuration : 1f;
            float val = _startTimeValue;
            int fps = _timelineDrawer != null ? _timelineDrawer.CurrentFps : 60;

            switch (_startUnit)
            {
                case MiniSliderUnit.Frames:
                    int frame = Mathf.RoundToInt(val * fps);
                    _startTimeField.SetValueWithoutNotify(frame);
                    break;
                case MiniSliderUnit.Normalized:
                    float norm = dur > 0.001f ? Mathf.Clamp01(val / dur) : 0f;
                    _startTimeField.SetValueWithoutNotify((float)Math.Round(norm, 3));
                    break;
                case MiniSliderUnit.Seconds:
                default:
                    _startTimeField.SetValueWithoutNotify((float)Math.Round(val, 3));
                    break;
            }
        }

        private void UpdateTimeDisplayLabel()
        {
            if (_timeDisplayLabel == null || _viewport?.Player == null) return;
            int fps = _timelineDrawer != null ? _timelineDrawer.CurrentFps : 60;
            float curT = _viewport.Player.CurrentTime;
            float totT = _viewport.Player.TotalDuration;
            int curF = Mathf.RoundToInt(curT * fps);
            int totF = Mathf.RoundToInt(totT * fps);

            if (_timelineDrawer != null && _timelineDrawer.RulerUnit == TimelineRulerUnit.Frames)
            {
                _timeDisplayLabel.text = $"{curF}F ({curT:0.00}s) / {totF}F ({totT:0.00}s)";
            }
            else
            {
                _timeDisplayLabel.text = $"{curT:0.00}s ({curF}F) / {totT:0.00}s ({totF}F)";
            }
        }

        private void HandleTimelineCrossfadeChanged(float newFade)
        {
            ApplyCrossfadeChange(newFade);
        }

        private void HandleTimelineExitTimeChanged(float newExit)
        {
            ApplyExitTimeChange(newExit);
        }

        private void HandleTimelineScrubChanged(float newTime)
        {
            if (_viewport?.Player != null)
            {
                _viewport.Player.EvaluateAt(newTime);
                UpdateTimeDisplayLabel();
                _timelineContainer?.MarkDirtyRepaint();
            }
        }

        private void ApplyCrossfadeChange(float newFade)
        {
            _currentCrossfade = Mathf.Max(0f, newFade);
            _crossfadeSlider.SetValueWithoutNotify(_currentCrossfade);
            _crossfadeField.SetValueWithoutNotify(_currentCrossfade);

            if (_viewport?.Player != null)
            {
                _viewport.Player.CrossfadeDuration = _currentCrossfade;
                _viewport.Player.EvaluateAt(_viewport.Player.CurrentTime);
            }

            PersistCurrentTransition();
            _timelineContainer?.MarkDirtyRepaint();
        }

        private void ApplyExitTimeChange(float newExit)
        {
            float srcDur = _sourceClipData.TimelineDuration > 0.001f ? _sourceClipData.TimelineDuration : 1f;
            _currentExitTime = Mathf.Clamp(newExit, 0f, srcDur);
            _hasCustomExitTime = true;
            _customExitTimeValue = _currentExitTime;
            _customExitToggle?.SetValueWithoutNotify(true);
            _customExitField?.SetEnabled(true);
            _exitUnitPopup?.SetEnabled(true);
            if (_exitMiniSlider != null)
            {
                _exitMiniSlider.IsEnabled = true;
                _exitMiniSlider.CurrentTime = _currentExitTime;
            }
            UpdateExitTimeFieldDisplay();

            if (_viewport?.Player != null)
            {
                _viewport.Player.ExitTime = _currentExitTime;
                _viewport.Player.EvaluateAt(_viewport.Player.CurrentTime);
            }

            PersistCurrentTransition();
            _exitMiniSliderContainer?.MarkDirtyRepaint();
            _timelineContainer?.MarkDirtyRepaint();
        }

        private void ApplyStartTimeChange(float newStart)
        {
            float tgtDur = _targetClipData.TimelineDuration > 0.001f ? _targetClipData.TimelineDuration : 1f;
            _startTimeValue = Mathf.Clamp(newStart, 0f, Mathf.Max(0f, tgtDur - 0.05f));
            _hasStartTime = true;
            _startTimeToggle?.SetValueWithoutNotify(true);
            _startTimeField?.SetEnabled(true);
            _startUnitPopup?.SetEnabled(true);
            if (_startMiniSlider != null)
            {
                _startMiniSlider.IsEnabled = true;
                _startMiniSlider.CurrentTime = _startTimeValue;
            }
            UpdateStartTimeFieldDisplay();

            if (_viewport?.Player != null)
            {
                _viewport.Player.StartTime = _startTimeValue;
                _viewport.Player.EvaluateAt(_viewport.Player.CurrentTime);
            }

            PersistCurrentTransition();
            _startMiniSliderContainer?.MarkDirtyRepaint();
            _timelineContainer?.MarkDirtyRepaint();
        }

        private void ResetToDefaultFade()
        {
            if (!HasValidTarget()) return;

            var targetAction = _filteredTargets[_selectedTargetIndex].TargetAction;
            _sourceAction.SetTransition(targetAction, -1f, _hasCustomExitTime, _customExitTimeValue, _hasStartTime, _startTimeValue);
            EditorUtility.SetDirty(_sourceAction);

            _currentCrossfade = _targetClipData.DefaultBlendIn;
            _crossfadeSlider.SetValueWithoutNotify(_currentCrossfade);
            _crossfadeField.SetValueWithoutNotify(_currentCrossfade);

            if (_viewport?.Player != null)
            {
                _viewport.Player.CrossfadeDuration = _currentCrossfade;
                _viewport.Player.EvaluateAt(_viewport.Player.CurrentTime);
            }

            UpdateSidebarItemFadeText(_selectedTargetIndex, "Default");
            _timelineContainer?.MarkDirtyRepaint();
        }

        private void PersistCurrentTransition()
        {
            if (!HasValidTarget()) return;

            var targetAction = _filteredTargets[_selectedTargetIndex].TargetAction;
            _sourceAction.SetTransition(targetAction, _currentCrossfade, _hasCustomExitTime, _customExitTimeValue, _hasStartTime, _startTimeValue);
            EditorUtility.SetDirty(_sourceAction);

            UpdateSidebarItemFadeText(_selectedTargetIndex, $"{_currentCrossfade:0.00}s");
        }

        private void UpdateSidebarItemFadeText(int index, string text)
        {
            if (index >= 0 && index < _targetListContainer.childCount)
            {
                var item = _targetListContainer.ElementAt(index);
                var fadeSummary = item.Q<Label>(className: "target-fade-summary");
                if (fadeSummary != null)
                {
                    fadeSummary.text = text;
                }
            }
        }

        private void SaveTransitionAssets()
        {
            if (_sourceAction == null) return;

            PersistCurrentTransition();
            EditorUtility.SetDirty(_sourceAction);
            string saveMsg = $"已成功保存 {_sourceAction.Name} 的过渡配置表！";

            // 若勾选了同步覆写 AutoRouteWindow
            if (_autoRouteSyncToggle != null && _autoRouteSyncToggle.value)
            {
                if (_selectedAutoRouteClipIndex >= 0 && _selectedAutoRouteClipIndex < _availableAutoRouteClips.Count && _sourceClipData.TimelineSO != null)
                {
                    var clip = _availableAutoRouteClips[_selectedAutoRouteClipIndex];
                    bool ok = ActionTimelineWindowOperations.AdjustRouteWindowTiming(
                        _sourceClipData.TimelineSO,
                        clip,
                        _selectedTimingTarget,
                        _currentExitTime,
                        out string adjustMsg
                    );

                    if (ok)
                    {
                        // 采用 ATEditor 标准的双轨保存（同时写出 SO 与 JSON 文件）
                        string knownPath = string.Empty;
                        if (_sourceAction.TimelineAsset != null)
                        {
                            knownPath = AssetDatabase.GetAssetPath(_sourceAction.TimelineAsset);
                        }
                        else if (_sourceAction.actionTimelineSO != null)
                        {
                            knownPath = AssetDatabase.GetAssetPath(_sourceAction.actionTimelineSO);
                        }

                        bool dualSaved = ActionTimelineWindowOperations.SaveTimelineDual(
                            _sourceClipData.TimelineSO,
                            knownPath,
                            _activeWorkspace,
                            out string savedJsonPath,
                            out string savedAssetPath
                        );

                        if (dualSaved)
                        {
                            // 若 ActionConfigAsset 原先缺少 SO 或 JSON 引用，自动对齐补全
                            if (_sourceAction.TimelineAsset == null && !string.IsNullOrEmpty(savedJsonPath))
                            {
                                _sourceAction.TimelineAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(savedJsonPath);
                            }
                            if (_sourceAction.actionTimelineSO == null && !string.IsNullOrEmpty(savedAssetPath))
                            {
                                _sourceAction.actionTimelineSO = AssetDatabase.LoadAssetAtPath<ActionTimeline>(savedAssetPath);
                            }
                            EditorUtility.SetDirty(_sourceAction);
                            saveMsg += "\n[双轨保存] 已同步更新 SO 与 JSON 时间轴资产！";
                        }
                        else
                        {
                            EditorUtility.SetDirty(_sourceClipData.TimelineSO);
                        }

                        saveMsg += "\n" + adjustMsg;

                        // 重新提取并刷新时间轴显示
                        _sourceClipData = ActionClipInfoExtractor.Extract(_sourceAction);
                        if (HasValidTarget())
                        {
                            var info = _filteredTargets[_selectedTargetIndex];
                            if (info.Kind == TransitionTargetKind.RouteBranch)
                            {
                                _currentRouteWindow = ActionClipInfoExtractor.ExtractRouteWindow(_sourceAction, info.MatchedRoute);
                            }
                        }
                        _timelineContainer?.MarkDirtyRepaint();
                    }
                }
            }

            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent(saveMsg));
        }

        private void TogglePlayPause()
        {
            if (_viewport?.Player == null) return;

            if (_viewport.Player.IsPlaying)
            {
                _viewport.Player.Pause();
                _playPauseBtn.text = "播放";
            }
            else
            {
                // 若停在末尾（单次播放结束），重新点击播放时自动从头开始
                if (_viewport.Player.CurrentTime >= _viewport.Player.TotalDuration - 0.001f)
                {
                    _viewport.Player.ResetToStart();
                }
                _viewport.Player.Play();
                _playPauseBtn.text = "暂停";
            }
        }

        /// <summary>
        /// 重置播放：将时间轴指针与角色位置复位到原点，模拟输入模式下重置等待回绕复位状态
        /// </summary>
        private void RestartPlay()
        {
            if (_viewport?.Player != null)
            {
                if (_viewport.Player.IsSimulateInputMode)
                {
                    _viewport.Player.ResetSimulatedInputState();
                }
                _viewport.Player.ResetToStart();
                // 1. 重置角色位置到原点
                _viewport.ResetCharacterTransform();
                // 2. 保持当前的播放/暂停状态与文案
                _playPauseBtn.text = _viewport.Player.IsPlaying ? "暂停" : "播放";
                _viewportContainer?.MarkDirtyRepaint();
                _timelineContainer?.MarkDirtyRepaint();
                UpdateTimeDisplayLabel();
            }
        }

        /// <summary>
        /// 单次播放完毕（非循环模式）暂停时回调更新 UI
        /// </summary>
        private void HandlePlaybackEnded()
        {
            if (_playPauseBtn != null) _playPauseBtn.text = "播放";
            _viewportContainer?.MarkDirtyRepaint();
            _timelineContainer?.MarkDirtyRepaint();
            UpdateTimeDisplayLabel();
        }

        /// <summary>
        /// 循环周期结束回绕时触发：根据开关复位角色位置
        /// </summary>
        private void HandleLoopCycleReset()
        {
            _viewport?.ResetCharacterTransform();
            _viewportContainer?.MarkDirtyRepaint();
        }

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastUpdateTime);
            _lastUpdateTime = now;

            if (_viewport?.Player != null && _viewport.Player.IsPlaying)
            {
                _viewport.Player.Tick(dt);

                UpdateTimeDisplayLabel();

                _viewportContainer?.MarkDirtyRepaint();
                _timelineContainer?.MarkDirtyRepaint();
            }
        }

        private bool HasValidTarget()
        {
            return _sourceAction != null && _selectedTargetIndex >= 0 && _selectedTargetIndex < _filteredTargets.Count && _filteredTargets[_selectedTargetIndex].TargetAction != null;
        }
    }
}
