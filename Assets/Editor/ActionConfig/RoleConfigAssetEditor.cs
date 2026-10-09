using System;
using System.Collections.Generic;
using Game.Framework;
using Game.GamePlay;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.ActionConfig
{
    /// <summary>
    /// 角色大盘资产自定义检视面板
    /// 打造集角色基础看板 手感阻尼调优 支援招架矩阵 动作拓扑全景图与健康体检于一体的大盘控制台
    /// </summary>
    [CustomEditor(typeof(RoleConfigAsset))]
    public class RoleConfigAssetEditor : UnityEditor.Editor
    {
        // 基础与核心属性
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _prefabProp;
        private SerializedProperty _actionRootProp;
        private SerializedProperty _hitReactionConfigProp;

        // 手感与输入属性
        private SerializedProperty _inputConfigProp;
        private SerializedProperty _moveShortInputThresholdProp;
        private SerializedProperty _moveInputDecelerationDurationProp;
        private SerializedProperty _moveInputResidualDurationProp;

        // 闪避属性
        private SerializedProperty _evadeConfigProp;
        private SerializedProperty _limitedTimesProp;
        private SerializedProperty _coolDownProp;

        // 支援与招架属性
        private SerializedProperty _assistConfigProp;
        private SerializedProperty _supportTypeProp;
        private SerializedProperty _parryStartActionProp;
        private SerializedProperty _parryReadyDurationProp;
        private SerializedProperty _parryRootMotionZOffsetProp;
        private SerializedProperty _parryLightProp;
        private SerializedProperty _parryHeavyProp;

        // 视听微件与物理属性
        private SerializedProperty _uiConfigProp;
        private SerializedProperty _groundRadiusProp;
        private SerializedProperty _groundHeightProp;
        private SerializedProperty _groundOffsetProp;
        private SerializedProperty _groundLayerProp;

        // 卡片折叠状态
        private bool _coreAssetsExpanded = true;
        private bool _inputFeelExpanded = true;
        private bool _assistMatrixExpanded = true;
        private bool _actionTopologyExpanded = true;
        private bool _uiPresentationExpanded = false;
        private bool _physicsCheckExpanded = false;

        // 动作拓扑网络分析缓存
        private readonly List<ActionConfigAsset> _cachedActionList = new();
        private readonly Dictionary<ActionDomainId, List<ActionConfigAsset>> _domainBuckets = new();
        private bool _topologyDirty = true;
        private readonly Dictionary<ActionDomainId, bool> _domainFoldoutStates = new();

        private void OnEnable()
        {
            // 绑定基础信息
            _idProp = serializedObject.FindProperty("ID");
            _nameProp = serializedObject.FindProperty("Name");
            _prefabProp = serializedObject.FindProperty("Prefab");
            _actionRootProp = serializedObject.FindProperty("ActionRoot");
            _hitReactionConfigProp = serializedObject.FindProperty("hitReactionConfig");

            // 绑定手感与输入
            _inputConfigProp = serializedObject.FindProperty("InputConfig");
            if (_inputConfigProp != null)
            {
                _moveShortInputThresholdProp = _inputConfigProp.FindPropertyRelative("MoveShortInputThreshold");
                _moveInputDecelerationDurationProp = _inputConfigProp.FindPropertyRelative("MoveInputDecelerationDuration");
                _moveInputResidualDurationProp = _inputConfigProp.FindPropertyRelative("MoveInputResidualDuration");
            }

            // 绑定闪避
            _evadeConfigProp = serializedObject.FindProperty("EvadeConfig");
            if (_evadeConfigProp != null)
            {
                _limitedTimesProp = _evadeConfigProp.FindPropertyRelative("LimitedTimes");
                _coolDownProp = _evadeConfigProp.FindPropertyRelative("CoolDown");
            }

            // 绑定支援招架
            _assistConfigProp = serializedObject.FindProperty("AssistConfig");
            if (_assistConfigProp != null)
            {
                _supportTypeProp = _assistConfigProp.FindPropertyRelative("SupportType");
                _parryStartActionProp = _assistConfigProp.FindPropertyRelative("ParryStartAction");
                _parryReadyDurationProp = _assistConfigProp.FindPropertyRelative("ParryReadyDuration");
                _parryRootMotionZOffsetProp = _assistConfigProp.FindPropertyRelative("ParryRootMotionZOffset");
                _parryLightProp = _assistConfigProp.FindPropertyRelative("ParryLight");
                _parryHeavyProp = _assistConfigProp.FindPropertyRelative("ParryHeavy");
            }

            // 绑定视听与物理
            _uiConfigProp = serializedObject.FindProperty("UIConfig");
            _groundRadiusProp = serializedObject.FindProperty("GroundRadius");
            _groundHeightProp = serializedObject.FindProperty("GroundHeight");
            _groundOffsetProp = serializedObject.FindProperty("GroundOffset");
            _groundLayerProp = serializedObject.FindProperty("GroundLayer");

            _topologyDirty = true;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RoleConfigAsset roleAsset = (RoleConfigAsset)target;
            if (roleAsset == null) return;

            if (_topologyDirty)
            {
                RebuildActionTopology(roleAsset);
            }

            // 顶部核心大盘看板
            DrawHeroBanner(roleAsset);

            EditorGUILayout.Space(6f);

            // 快捷工具栏
            DrawQuickActionsToolbar(roleAsset);

            EditorGUILayout.Space(8f);

            // 模块一 核心资产与大盘入口
            DrawCoreAssetsCard(roleAsset);

            EditorGUILayout.Space(6f);

            // 模块二 手感阻尼与闪避控制台
            DrawInputFeelCard();

            EditorGUILayout.Space(6f);

            // 模块三 支援与招架反制矩阵
            DrawAssistMatrixCard();

            EditorGUILayout.Space(6f);

            // 模块四 全角色动作拓扑全景图
            DrawActionTopologyCard(roleAsset);

            EditorGUILayout.Space(6f);

            // 模块五 视听微件与地面物理检测
            DrawPresentationAndPhysicsCards();

            EditorGUILayout.Space(10f);

            serializedObject.ApplyModifiedProperties();
        }

        // 核心看板
        private void DrawHeroBanner(RoleConfigAsset roleAsset)
        {
            Color bannerBg = EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.22f, 0.32f, 0.9f)
                : new Color(0.82f, 0.90f, 0.98f, 0.95f);

            Color accentBarColor = new Color(0.20f, 0.65f, 0.95f, 1f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 50f);
                EditorGUI.DrawRect(rect, bannerBg);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 5f, rect.height), accentBarColor);

                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft
                };
                if (EditorGUIUtility.isProSkin) titleStyle.normal.textColor = Color.white;

                GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft
                };

                string roleDisplayName = !string.IsNullOrEmpty(roleAsset.Name) ? roleAsset.Name : roleAsset.name;
                string titleText = $"角色战斗大盘控制台  {roleDisplayName}";
                Rect titleRect = new Rect(rect.x + 12f, rect.y + 4f, rect.width - 20f, 20f);
                EditorGUI.LabelField(titleRect, titleText, titleStyle);

                string assistTypeName = "未指定";
                if (_supportTypeProp != null && _supportTypeProp.enumValueIndex >= 0 && _supportTypeProp.enumValueIndex < _supportTypeProp.enumDisplayNames.Length)
                {
                    assistTypeName = _supportTypeProp.enumDisplayNames[_supportTypeProp.enumValueIndex];
                }

                string idDesc = $"编号 {roleAsset.ID}";
                string assistDesc = $"支援形态 {assistTypeName}";
                string countDesc = $"拓扑动作 {_cachedActionList.Count} 项";
                string rootStatus = roleAsset.ActionRoot != null ? $"根动作 {roleAsset.ActionRoot.name}" : "未配置根动作";

                string metaLine = $"{idDesc}     {assistDesc}     {countDesc}     {rootStatus}";
                Rect metaRect = new Rect(rect.x + 12f, rect.y + 26f, rect.width - 20f, 18f);
                EditorGUI.LabelField(metaRect, metaLine, subStyle);
            }
        }

        // 快捷工具栏
        private void DrawQuickActionsToolbar(RoleConfigAsset roleAsset)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUIContent pingAssetBtn = new GUIContent("定位角色资产", "在工程资源目录中高亮并选中当前角色配置文件");
                if (GUILayout.Button(pingAssetBtn, EditorStyles.miniButtonLeft, GUILayout.Height(24f)))
                {
                    EditorGUIUtility.PingObject(roleAsset);
                    Selection.activeObject = roleAsset;
                }

                GUIContent pingRootBtn = new GUIContent("定位动作根节点", "在工程资源目录中高亮并选中当前角色的动作树根资产");
                using (new EditorGUI.DisabledScope(roleAsset.ActionRoot == null))
                {
                    if (GUILayout.Button(pingRootBtn, EditorStyles.miniButtonMid, GUILayout.Height(24f)))
                    {
                        if (roleAsset.ActionRoot != null)
                        {
                            EditorGUIUtility.PingObject(roleAsset.ActionRoot);
                            Selection.activeObject = roleAsset.ActionRoot;
                        }
                    }
                }

                GUIContent refreshTopologyBtn = new GUIContent("刷新动作拓扑", "重新递归解析动作树与派生路由网络更新大盘动作总表");
                if (GUILayout.Button(refreshTopologyBtn, EditorStyles.miniButtonMid, GUILayout.Height(24f)))
                {
                    RebuildActionTopology(roleAsset);
                }

                GUIContent healthCheckBtn = new GUIContent("角色大盘体检", "对角色动作网络 支援配置 手感参数与界面资源进行全方位合规性诊断");
                if (GUILayout.Button(healthCheckBtn, EditorStyles.miniButtonRight, GUILayout.Height(24f)))
                {
                    PerformRoleHealthCheck(roleAsset);
                }
            }
        }

        // 模块一 核心资产与大盘入口
        private void DrawCoreAssetsCard(RoleConfigAsset roleAsset)
        {
            Color accentColor = new Color(0.35f, 0.70f, 0.95f, 1f);
            BeginCard("核心资产与大盘入口", ref _coreAssetsExpanded, accentColor);
            if (_coreAssetsExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_idProp, new GUIContent("角色编号", "角色在战斗数值与数据系统中的唯一编号"));
                EditorGUILayout.PropertyField(_nameProp, new GUIContent("角色标识", "角色的描述性文本别名"));
                EditorGUILayout.PropertyField(_prefabProp, new GUIContent("角色预制体", "角色的视觉模型 骨骼与物理渲染预制体"));

                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(_actionRootProp, new GUIContent("动作树根节点", "全角色动作状态流转的起始入口 通常为基础待机动作"));

                if (roleAsset.ActionRoot == null)
                {
                    EditorGUILayout.HelpBox("尚未配置动作树根节点 实体初始化时将无法启动默认动作与派生路由网络", MessageType.Warning);
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(_hitReactionConfigProp, new GUIContent("受击表现配置", "角色的受击方位 浮空与击飞表现资产引用"));

                EditorGUI.indentLevel--;
            }
            EndCard(_coreAssetsExpanded);
        }

        // 模块二 手感阻尼与闪避控制台
        private void DrawInputFeelCard()
        {
            Color accentColor = new Color(0.95f, 0.65f, 0.20f, 1f);
            BeginCard("手感阻尼与闪避控制台", ref _inputFeelExpanded, accentColor);
            if (_inputFeelExpanded)
            {
                EditorGUI.indentLevel++;

                // 移动手感阻尼
                EditorGUILayout.LabelField("移动输入与转向阻尼调优", EditorStyles.boldLabel);

                if (_moveInputDecelerationDurationProp != null)
                {
                    EditorGUILayout.Slider(_moveInputDecelerationDurationProp, 0f, 0.3f, new GUIContent("按键松开衰减阻尼", "松开移动按键后移动向量平滑归零的衰减时长 单位秒 推荐区间零点零六至零点零八秒"));
                    DrawFeelGaugeBar(_moveInputDecelerationDurationProp.floatValue, 0f, 0.3f, 0.06f, 0.08f, "阻尼衰减风格", "硬性切断 街机手感", "推荐手感 物理惯性", "强阻尼 慢节奏");
                }

                if (_moveInputResidualDurationProp != null)
                {
                    EditorGUILayout.Slider(_moveInputResidualDurationProp, 0.01f, 0.5f, new GUIContent("输入残留滞后阻尼", "输入残留缓存的平滑阻尼时长 单位秒 推荐区间零点一至零点一五秒 用于大幅度掉头与急停转向判定"));
                    DrawFeelGaugeBar(_moveInputResidualDurationProp.floatValue, 0.01f, 0.5f, 0.10f, 0.15f, "掉头判定窗口", "紧凑灵敏 快速掉头", "推荐窗口 稳定识别", "宽松宽容 易误触");
                }

                if (_moveShortInputThresholdProp != null)
                {
                    EditorGUILayout.PropertyField(_moveShortInputThresholdProp, new GUIContent("短按轻推时间阈值", "短按移动按键的时间判定阈值 单位秒 低于此时长判定为轻推"));
                }

                EditorGUILayout.Space(6f);

                // 闪避手感与限制
                EditorGUILayout.LabelField("闪避频次与冷却限制", EditorStyles.boldLabel);
                if (_limitedTimesProp != null)
                {
                    EditorGUILayout.IntSlider(_limitedTimesProp, 0, 5, new GUIContent("连续闪避次数上限", "允许连续触发闪避的最大次数 零表示无限制自由闪避 默认为两次"));
                }
                if (_coolDownProp != null)
                {
                    EditorGUILayout.Slider(_coolDownProp, 0.1f, 3f, new GUIContent("闪避冷却恢复时间", "达到闪避次数上限后的冷却恢复时间 单位秒"));
                }

                EditorGUI.indentLevel--;
            }
            EndCard(_inputFeelExpanded);
        }

        // 模块三 支援与招架反制矩阵
        private void DrawAssistMatrixCard()
        {
            Color accentColor = new Color(0.40f, 0.85f, 0.55f, 1f);
            BeginCard("支援与招架反制矩阵", ref _assistMatrixExpanded, accentColor);
            if (_assistMatrixExpanded)
            {
                EditorGUI.indentLevel++;

                if (_supportTypeProp != null)
                {
                    EditorGUILayout.PropertyField(_supportTypeProp, new GUIContent("支援形态分类", "近战角色通常为招架支援 远程角色通常为回避支援"));
                }

                bool isParry = _supportTypeProp == null || _supportTypeProp.enumValueIndex == (int)RoleAssistType.ParryAid;

                if (isParry)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("近战招架起手与接刀位移", EditorStyles.boldLabel);

                    if (_parryStartActionProp != null)
                    {
                        EditorGUILayout.PropertyField(_parryStartActionProp, new GUIContent("招架起手动作", "切入招架时优先播放的起手动作资产"));
                    }
                    if (_parryReadyDurationProp != null)
                    {
                        EditorGUILayout.Slider(_parryReadyDurationProp, 0.05f, 0.5f, new GUIContent("防御准备完成时长", "招架起手举刀完成防御姿态的时间点 单位秒 推荐零点二秒"));
                    }
                    if (_parryRootMotionZOffsetProp != null)
                    {
                        EditorGUILayout.Slider(_parryRootMotionZOffsetProp, 0f, 3f, new GUIContent("前向根运动总位移", "招架起手完整播放时产生的位移偏移 单位米 推荐一米"));
                    }

                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("轻重招架反制双列对比矩阵", EditorStyles.boldLabel);

                    float origLabelWidth = EditorGUIUtility.labelWidth;
                    int origIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    float viewWidth = EditorGUIUtility.currentViewWidth;
                    float totalAvailableWidth = Mathf.Max(240f, viewWidth - 52f);
                    float columnGap = 4f;
                    float columnWidth = Mathf.Floor((totalAvailableWidth - columnGap) * 0.5f);
                    bool useDoubleColumn = totalAvailableWidth >= 300f;

                    if (useDoubleColumn)
                    {
                        // 双列横向对比卡片
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            // 左列 轻招架
                            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(columnWidth), GUILayout.ExpandWidth(false)))
                            {
                                EditorGUI.indentLevel = 0;
                                EditorGUIUtility.labelWidth = 56f;
                                DrawMatrixSubHeader("轻招架反制  消耗一点支援点数", new Color(0.25f, 0.65f, 0.95f, 1f));
                                if (_parryLightProp != null)
                                {
                                    DrawParryEntryFields(_parryLightProp);
                                }
                            }

                            GUILayout.Space(columnGap);

                            // 右列 重招架
                            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(columnWidth), GUILayout.ExpandWidth(false)))
                            {
                                EditorGUI.indentLevel = 0;
                                EditorGUIUtility.labelWidth = 56f;
                                DrawMatrixSubHeader("重招架反制  消耗两点支援点数", new Color(0.95f, 0.55f, 0.20f, 1f));
                                if (_parryHeavyProp != null)
                                {
                                    DrawParryEntryFields(_parryHeavyProp);
                                }
                            }
                        }
                    }
                    else
                    {
                        // 超窄窗口自动采用垂直单列排布
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            EditorGUI.indentLevel = 0;
                            EditorGUIUtility.labelWidth = 65f;
                            DrawMatrixSubHeader("轻招架反制  消耗一点支援点数", new Color(0.25f, 0.65f, 0.95f, 1f));
                            if (_parryLightProp != null)
                            {
                                DrawParryEntryFields(_parryLightProp);
                            }
                        }

                        EditorGUILayout.Space(4f);

                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            EditorGUI.indentLevel = 0;
                            EditorGUIUtility.labelWidth = 65f;
                            DrawMatrixSubHeader("重招架反制  消耗两点支援点数", new Color(0.95f, 0.55f, 0.20f, 1f));
                            if (_parryHeavyProp != null)
                            {
                                DrawParryEntryFields(_parryHeavyProp);
                            }
                        }
                    }

                    EditorGUI.indentLevel = origIndent;
                    EditorGUIUtility.labelWidth = origLabelWidth;
                }
                else
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.HelpBox("当前角色为远程回避支援形态 回避支援无需配置接刀位移与轻重招架矩阵 收到支援请求时直接切入回避支援动作", MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }
            EndCard(_assistMatrixExpanded);
        }

        // 模块四 全角色动作拓扑全景图
        private void DrawActionTopologyCard(RoleConfigAsset roleAsset)
        {
            Color accentColor = new Color(0.85f, 0.45f, 0.95f, 1f);
            string countText = $"共搜集 {_cachedActionList.Count} 个动作资产";
            BeginCard($"全角色动作拓扑全景图  {countText}", ref _actionTopologyExpanded, accentColor);
            if (_actionTopologyExpanded)
            {
                if (_cachedActionList.Count == 0)
                {
                    EditorGUILayout.HelpBox("当前尚未解析到任何动作资产 请先配置动作树根节点并点击上方刷新动作拓扑按钮", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("基于动作树根节点与派生路由网络自动递归收集 按宏观动作领域分桶归类 可点击右侧定位按钮直接在工程中选中对应动作资产", MessageType.None);

                    foreach (var kvp in _domainBuckets)
                    {
                        ActionDomainId domainId = kvp.Key;
                        List<ActionConfigAsset> actions = kvp.Value;
                        if (actions.Count == 0) continue;

                        if (!_domainFoldoutStates.TryGetValue(domainId, out bool foldout))
                        {
                            foldout = true;
                            _domainFoldoutStates[domainId] = foldout;
                        }

                        string domainTitle = GetDomainDisplayName(domainId);
                        string domainHeader = $"{domainTitle}  包含 {actions.Count} 项动作";

                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            _domainFoldoutStates[domainId] = EditorGUILayout.Foldout(foldout, domainHeader, true, EditorStyles.boldLabel);
                            if (_domainFoldoutStates[domainId])
                            {
                                EditorGUILayout.Space(2f);
                                for (int i = 0; i < actions.Count; i++)
                                {
                                    var act = actions[i];
                                    if (act == null) continue;

                                    DrawActionTopologyRow(act, i);
                                }
                            }
                        }
                        EditorGUILayout.Space(2f);
                    }
                }
            }
            EndCard(_actionTopologyExpanded);
        }

        // 模块五 视听微件与地面物理检测
        private void DrawPresentationAndPhysicsCards()
        {
            // UI 视听微件
            Color uiAccentColor = new Color(0.35f, 0.85f, 0.85f, 1f);
            BeginCard("视听呈现与界面微件配置", ref _uiPresentationExpanded, uiAccentColor);
            if (_uiPresentationExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_uiConfigProp, new GUIContent("角色界面配置资产", "角色专属头像 机制面板与按键面板图标配置资产"));

                if (_uiConfigProp != null && _uiConfigProp.objectReferenceValue is CharacterUIConfigAsset uiAsset)
                {
                    DrawUIAssetPreviewCard(uiAsset);
                }
                else
                {
                    EditorGUILayout.HelpBox("未配置角色界面资产 战斗中左上角角色头像与按键面板将使用全局通用保底图标", MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
            EndCard(_uiPresentationExpanded);

            EditorGUILayout.Space(6f);

            // 地面物理检测
            Color physicsAccentColor = new Color(0.70f, 0.75f, 0.80f, 1f);
            BeginCard("地面碰撞与物理环境检测", ref _physicsCheckExpanded, physicsAccentColor);
            if (_physicsCheckExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(_groundRadiusProp, 0.05f, 1f, new GUIContent("地面检测球半径", "脚底着地射线与球形碰撞检测半径 单位米"));
                EditorGUILayout.Slider(_groundHeightProp, 0.5f, 3f, new GUIContent("角色躯干检测高度", "角色躯干高度判定线 单位米"));
                EditorGUILayout.Slider(_groundOffsetProp, 0f, 0.5f, new GUIContent("脚底离地浮动偏移", "脚底吸附地面的容差偏移量 单位米"));
                EditorGUILayout.PropertyField(_groundLayerProp, new GUIContent("地面物理层级掩码", "判定为可行走地面的物理图层"));
                EditorGUI.indentLevel--;
            }
            EndCard(_physicsCheckExpanded);
        }

        // 辅助绘制 招架条目
        private void DrawParryEntryFields(SerializedProperty parryEntryProp)
        {
            var actionProp = parryEntryProp.FindPropertyRelative("Action");
            var hitEffectProp = parryEntryProp.FindPropertyRelative("HitEffectId");
            var hitStopProp = parryEntryProp.FindPropertyRelative("HitStopDuration");

            if (actionProp != null)
            {
                Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(rowRect, actionProp, new GUIContent("反制动作", "招架成功后触发切入的反制动作资产"));
            }
            if (hitEffectProp != null)
            {
                Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(rowRect, hitEffectProp, new GUIContent("命中效果", "招架命中效果 决定怪物受击与失衡数值"));
            }
            if (hitStopProp != null)
            {
                Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.Slider(rowRect, hitStopProp, 0.01f, 0.5f, new GUIContent("顿帧时长", "招架成功时双方同时顿帧定格的时长 单位秒 推荐零点二秒"));
            }
        }

        private static void DrawMatrixSubHeader(string text, Color accentColor)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            Color bg = EditorGUIUtility.isProSkin
                ? new Color(0.20f, 0.24f, 0.28f, 0.85f)
                : new Color(0.85f, 0.88f, 0.92f, 0.85f);

            EditorGUI.DrawRect(rect, bg);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accentColor);

            Rect labelRect = new Rect(rect.x + 8f, rect.y + 1f, rect.width - 12f, 18f);
            EditorGUI.LabelField(labelRect, new GUIContent(text, text), EditorStyles.boldLabel);
        }

        // 辅助绘制 拓扑动作单行
        private void DrawActionTopologyRow(ActionConfigAsset act, int index)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, 22f);

            Color rowBg = (index % 2 == 0)
                ? (EditorGUIUtility.isProSkin ? new Color(0.18f, 0.22f, 0.25f, 0.5f) : new Color(0.92f, 0.94f, 0.96f, 0.6f))
                : (EditorGUIUtility.isProSkin ? new Color(0.14f, 0.17f, 0.20f, 0.5f) : new Color(0.88f, 0.90f, 0.92f, 0.6f));

            EditorGUI.DrawRect(rowRect, rowBg);

            // 状态徽标与名称
            bool hasTimeline = act.actionTimelineSO != null || act.TimelineAsset != null;
            string statusIcon = hasTimeline ? "●" : "▲";
            Color iconColor = hasTimeline ? new Color(0.35f, 0.85f, 0.45f, 1f) : new Color(0.95f, 0.45f, 0.35f, 1f);

            GUIStyle iconStyle = new GUIStyle(EditorStyles.boldLabel);
            iconStyle.normal.textColor = iconColor;
            Rect iconRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, 16f, 18f);
            EditorGUI.LabelField(iconRect, statusIcon, iconStyle);

            string actionLabel = act.ID > 0 ? $"{act.name}  编号 {act.ID}" : act.name;
            Rect nameRect = new Rect(rowRect.x + 24f, rowRect.y + 2f, rowRect.width - 90f, 18f);
            EditorGUI.LabelField(nameRect, actionLabel, EditorStyles.label);

            // 右侧快捷定位按钮
            Rect btnRect = new Rect(rowRect.xMax - 60f, rowRect.y + 2f, 56f, 18f);
            if (GUI.Button(btnRect, "定位资产", EditorStyles.miniButton))
            {
                EditorGUIUtility.PingObject(act);
                Selection.activeObject = act;
            }
        }

        // 辅助绘制 手感标尺槽
        private static void DrawFeelGaugeBar(float currentVal, float minVal, float maxVal, float recMin, float recMax, string label, string lowDesc, string midDesc, string highDesc)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 14f);
            rect.x += EditorGUIUtility.labelWidth;
            rect.width -= EditorGUIUtility.labelWidth;

            if (rect.width <= 10f) return;

            // 绘制底槽
            Color slotBg = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.14f, 0.16f, 0.8f) : new Color(0.80f, 0.82f, 0.84f, 0.8f);
            EditorGUI.DrawRect(rect, slotBg);

            // 绘制推荐区域指示
            float range = Mathf.Max(0.001f, maxVal - minVal);
            float recStartX = rect.x + ((recMin - minVal) / range) * rect.width;
            float recEndX = rect.x + ((recMax - minVal) / range) * rect.width;
            Rect recRect = new Rect(recStartX, rect.y, Mathf.Max(2f, recEndX - recStartX), rect.height);
            EditorGUI.DrawRect(recRect, new Color(0.35f, 0.75f, 0.45f, 0.35f));

            // 绘制当前指针点
            float pointerX = rect.x + Mathf.Clamp01((currentVal - minVal) / range) * rect.width;
            Rect pointerRect = new Rect(pointerX - 2f, rect.y, 4f, rect.height);
            Color pointerColor = (currentVal >= recMin && currentVal <= recMax)
                ? new Color(0.40f, 0.90f, 0.50f, 1f)
                : new Color(0.95f, 0.65f, 0.25f, 1f);
            EditorGUI.DrawRect(pointerRect, pointerColor);

            // 文字指示
            string currentFeelTag = currentVal < recMin ? lowDesc : (currentVal > recMax ? highDesc : midDesc);
            GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleRight
            };
            EditorGUI.LabelField(rect, $"{label}  {currentFeelTag}", descStyle);
        }

        // 辅助绘制 UI资产概览微件
        private static void DrawUIAssetPreviewCard(CharacterUIConfigAsset ui)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("界面资源齐备度一览", EditorStyles.boldLabel);

                string avatarStatus = ui.RoleIconGeneral != null ? "常规头像已配置" : "常规头像缺失";
                string circleStatus = ui.RoleIconCircle != null ? "圆形头像已配置" : "圆形头像缺失";
                string normalSkillStatus = ui.SpecialAttackNormalIcon != null ? "特殊技图标已配置" : "特殊技使用保底图标";
                string exSkillStatus = ui.SpecialAttackExIcon != null ? "强化特殊技图标已配置" : "强化特殊技使用保底图标";
                string ultStatus = ui.UltimateReadyIcon != null ? "终结技就绪图标已配置" : "终结技使用保底图标";

                EditorGUILayout.LabelField($"头像状态  {avatarStatus}  {circleStatus}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"技能按键图标  {normalSkillStatus}  {exSkillStatus}  {ultStatus}", EditorStyles.miniLabel);

                if (ui.MechanicUIPrefab != null)
                {
                    EditorGUILayout.LabelField($"专属机制面板预制体  {ui.MechanicUIPrefab.name}", EditorStyles.miniLabel);
                }
            }
        }

        // 拓扑收集引擎
        private void RebuildActionTopology(RoleConfigAsset roleAsset)
        {
            _cachedActionList.Clear();
            _domainBuckets.Clear();

            // 初始化所有领域分桶
            foreach (ActionDomainId d in Enum.GetValues(typeof(ActionDomainId)))
            {
                _domainBuckets[d] = new List<ActionConfigAsset>();
            }

            if (roleAsset != null && roleAsset.ActionRoot != null)
            {
                HashSet<ActionConfigAsset> set = new();
                CollectActionRecursive(roleAsset.ActionRoot, set);

                foreach (var act in set)
                {
                    if (act == null) continue;
                    _cachedActionList.Add(act);
                    if (!_domainBuckets.ContainsKey(act.DomainId))
                    {
                        _domainBuckets[act.DomainId] = new List<ActionConfigAsset>();
                    }
                    _domainBuckets[act.DomainId].Add(act);
                }
            }

            _topologyDirty = false;
        }

        private static void CollectActionRecursive(ActionConfigAsset action, HashSet<ActionConfigAsset> collected)
        {
            if (action == null || !collected.Add(action)) return;

            if (action.CompleteAction != null)
            {
                CollectActionRecursive(action.CompleteAction, collected);
            }

            List<ActionRoute> routes = new();
            action.CollectEffectiveRoutes(routes);
            foreach (var r in routes)
            {
                if (r?.ExecuteAction != null)
                {
                    CollectActionRecursive(r.ExecuteAction, collected);
                }
            }
        }

        private static string GetDomainDisplayName(ActionDomainId domainId)
        {
            return domainId switch
            {
                ActionDomainId.Locomotion => "地面与空间位移领域",
                ActionDomainId.Combat => "战斗与技能领域",
                ActionDomainId.Defense => "防御与招架领域",
                ActionDomainId.HitReaction => "受击防御与倒地领域",
                ActionDomainId.Evasion => "闪避与冲刺领域",
                ActionDomainId.Switch => "角色切换领域",
                _ => "其他扩展动作领域"
            };
        }

        // 角色大盘健康体检
        private void PerformRoleHealthCheck(RoleConfigAsset roleAsset)
        {
            if (roleAsset == null) return;

            RebuildActionTopology(roleAsset);

            List<string> issues = new();
            List<string> passes = new();

            // 1. 检查根动作
            if (roleAsset.ActionRoot == null)
            {
                issues.Add("未配置动作树根节点 角色初始化时无法进入默认动作");
            }
            else
            {
                passes.Add($"动作树根节点正常 挂载根动作为 {roleAsset.ActionRoot.name}");
            }

            // 2. 检查拓扑规模
            if (_cachedActionList.Count == 0)
            {
                issues.Add("动作拓扑网络为空 未搜集到任何有效派生动作");
            }
            else
            {
                passes.Add($"动作拓扑网络连通良好 共递归搜集 {_cachedActionList.Count} 项派生动作");
            }

            // 3. 检查缺失时间轴的动作
            int missingTimelineCount = 0;
            foreach (var act in _cachedActionList)
            {
                if (act != null && act.actionTimelineSO == null && act.TimelineAsset == null)
                {
                    missingTimelineCount++;
                }
            }
            if (missingTimelineCount > 0)
            {
                issues.Add($"拓扑中有 {missingTimelineCount} 项动作未配置任何时间轴资产");
            }
            else
            {
                passes.Add("全部拓扑动作均已绑定有效时间轴资源");
            }

            // 4. 检查招架配置
            if (roleAsset.AssistConfig != null && roleAsset.AssistConfig.SupportType == RoleAssistType.ParryAid)
            {
                if (roleAsset.AssistConfig.ParryStartAction == null)
                {
                    issues.Add("招架支援形态未配置招架起手动作");
                }
                if (roleAsset.AssistConfig.ParryLight == null || roleAsset.AssistConfig.ParryLight.Action == null)
                {
                    issues.Add("未配置轻招架反制动作资产");
                }
                if (roleAsset.AssistConfig.ParryHeavy == null || roleAsset.AssistConfig.ParryHeavy.Action == null)
                {
                    issues.Add("未配置重招架反制动作资产");
                }
                if (roleAsset.AssistConfig.ParryStartAction != null &&
                    roleAsset.AssistConfig.ParryLight?.Action != null &&
                    roleAsset.AssistConfig.ParryHeavy?.Action != null)
                {
                    passes.Add("近战招架矩阵起手与轻重反制动作配置完整");
                }
            }

            // 5. 检查UI资产
            if (roleAsset.UIConfig == null)
            {
                issues.Add("未配置角色界面资产 战斗将使用保底微件与图标");
            }
            else
            {
                passes.Add("角色界面资产已挂载");
            }

            // 弹窗汇报
            string reportText = "角色大盘健康体检报告\n\n";
            if (issues.Count == 0)
            {
                reportText += "体检完成 全部核心项目合规达标\n\n";
                foreach (var p in passes)
                {
                    reportText += $"合格  {p}\n";
                }
                EditorUtility.DisplayDialog("角色大盘健康体检", reportText, "确定");
            }
            else
            {
                reportText += $"体检完成 发现 {issues.Count} 项待完善配置\n\n";
                foreach (var issue in issues)
                {
                    reportText += $"待处理  {issue}\n";
                }
                reportText += "\n";
                foreach (var p in passes)
                {
                    reportText += $"合格  {p}\n";
                }
                EditorUtility.DisplayDialog("角色大盘健康体检", reportText, "确定");
            }
        }

        // 卡片辅助容器
        private static void BeginCard(string title, ref bool isExpanded, Color headerAccentColor)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Rect headerRect = EditorGUILayout.GetControlRect(false, 22f);
            Color headerBg = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.22f, 0.26f, 0.75f)
                : new Color(0.85f, 0.88f, 0.92f, 0.85f);

            EditorGUI.DrawRect(headerRect, headerBg);
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 4f, headerRect.height), headerAccentColor);

            Rect foldoutRect = new Rect(headerRect.x + 8f, headerRect.y + 2f, headerRect.width - 16f, 18f);
            isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, title, true, EditorStyles.boldLabel);

            if (isExpanded)
            {
                EditorGUILayout.Space(4f);
            }
        }

        private static void EndCard(bool isExpanded)
        {
            if (isExpanded)
            {
                EditorGUILayout.Space(4f);
            }
            EditorGUILayout.EndVertical();
        }
    }
}
