using System;
using System.Collections.Generic;
using Game.Editor.ActionTransition;
using Game.Framework;
using Game.GamePlay;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.ActionConfig
{
    /// <summary>
    /// 动作配置资产自定义检视面板
    /// 包含核心看板 快捷工具栏与分区卡片化布局
    /// </summary>
    [CustomEditor(typeof(ActionConfigAsset), true)]
    public class ActionConfigAssetEditor : UnityEditor.Editor
    {
        // 序列化属性缓存
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _timelineAssetProp;
        private SerializedProperty _actionTimelineSOProp;
        private SerializedProperty _domainIdProp;
        private SerializedProperty _completeModeProp;
        private SerializedProperty _completeActionProp;
        private SerializedProperty _routesProp;
        private SerializedProperty _routeSetsProp;
        private SerializedProperty _inheritModeProp;

        // 折叠卡片状态缓存
        private bool _coreAssetsExpanded = true;
        private bool _stateTransitionExpanded = true;
        private bool _transitionsOverviewExpanded = true;
        private bool _actionRoutesExpanded = true;
        private bool _sharedRouteSetsExpanded = true;

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("ID");
            _nameProp = serializedObject.FindProperty("Name");
            _timelineAssetProp = serializedObject.FindProperty("TimelineAsset");
            _actionTimelineSOProp = serializedObject.FindProperty("actionTimelineSO");
            _domainIdProp = serializedObject.FindProperty("_domainId");
            _completeModeProp = serializedObject.FindProperty("CompleteMode");
            _completeActionProp = serializedObject.FindProperty("CompleteAction");
            _routesProp = serializedObject.FindProperty("Routes");
            _routeSetsProp = serializedObject.FindProperty("RouteSets");
            _inheritModeProp = serializedObject.FindProperty("InheritMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ActionConfigAsset asset = (ActionConfigAsset)target;
            if (asset == null) return;

            // 顶部核心看板
            DrawHeroBanner(asset);

            EditorGUILayout.Space(6f);

            // 快捷工具栏
            DrawQuickActionsToolbar(asset);

            EditorGUILayout.Space(8f);

            // 卡片一 核心资产与时间轴
            DrawCoreAssetsCard(asset);

            EditorGUILayout.Space(6f);

            // 卡片二 动作完成策略
            DrawCompletionCard(asset);

            EditorGUILayout.Space(6f);

            // 卡片三 过渡表现概览
            DrawTransitionsOverviewCard(asset);

            EditorGUILayout.Space(6f);

            // 卡片四 派生路由大盘
            DrawActionRoutesCard(asset);

            EditorGUILayout.Space(6f);

            // 卡片五 通用路由集复用
            DrawSharedRouteSetsCard(asset);

            EditorGUILayout.Space(10f);

            serializedObject.ApplyModifiedProperties();
        }

        // 核心看板
        private void DrawHeroBanner(ActionConfigAsset asset)
        {
            bool isRole = asset is RoleActionConfigAsset;
            bool isMonster = asset is MonsterActionConfigAsset;

            Color bannerBg = EditorGUIUtility.isProSkin
                ? (isRole ? new Color(0.12f, 0.22f, 0.32f, 0.9f) : (isMonster ? new Color(0.32f, 0.16f, 0.16f, 0.9f) : new Color(0.18f, 0.22f, 0.28f, 0.9f)))
                : (isRole ? new Color(0.82f, 0.90f, 0.98f, 0.95f) : (isMonster ? new Color(0.98f, 0.86f, 0.86f, 0.95f) : new Color(0.88f, 0.90f, 0.94f, 0.95f)));

            Color accentBarColor = isRole
                ? new Color(0.20f, 0.65f, 0.95f, 1f)
                : (isMonster ? new Color(0.95f, 0.35f, 0.25f, 1f) : new Color(0.40f, 0.70f, 0.85f, 1f));

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 48f);
                EditorGUI.DrawRect(rect, bannerBg);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 5f, rect.height), accentBarColor);

                string badge = isRole ? "角色动作" : (isMonster ? "怪物动作" : "通用动作");

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

                Rect titleRect = new Rect(rect.x + 12f, rect.y + 4f, rect.width - 20f, 20f);
                EditorGUI.LabelField(titleRect, $"{badge}  {asset.name}", titleStyle);

                string timelineStatus = asset.actionTimelineSO != null
                    ? "时间轴资产正常"
                    : (asset.TimelineAsset != null ? "文本已绑定待生成资产" : "未绑定时间轴");

                string domainDesc = $"领域: {asset.DomainId}";
                string idDesc = $"编号: {(asset.ID > 0 ? asset.ID.ToString() : "未指定")}";
                string metaLine = $"{idDesc}     {domainDesc}     {timelineStatus}";

                Rect metaRect = new Rect(rect.x + 12f, rect.y + 26f, rect.width - 20f, 16f);
                EditorGUI.LabelField(metaRect, metaLine, subStyle);
            }
        }

        // 快捷工具栏
        private void DrawQuickActionsToolbar(ActionConfigAsset asset)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUIContent skillEditorBtn = new GUIContent("打开时间轴编辑器", "在时间轴编辑器中载入当前动作进行编辑与视口预览");
                if (GUILayout.Button(skillEditorBtn, EditorStyles.miniButtonLeft, GUILayout.Height(24f)))
                {
                    OpenInSkillEditor(asset);
                }

                GUIContent workbenchBtn = new GUIContent("打开过渡工作台", "打开动作过渡工作台并将当前动作设为源动作调整前后过渡参数");
                if (GUILayout.Button(workbenchBtn, EditorStyles.miniButtonMid, GUILayout.Height(24f)))
                {
                    OpenInTransitionWorkbench(asset);
                }

                GUIContent healthCheckBtn = new GUIContent("路由健康体检", "检查当前动作派生路由是否存在未配置动作或非法按键条件等隐患");
                if (GUILayout.Button(healthCheckBtn, EditorStyles.miniButtonRight, GUILayout.Height(24f)))
                {
                    PerformRoutesHealthCheck(asset);
                }
            }
        }

        // 卡片一 核心资产与时间轴
        private void DrawCoreAssetsCard(ActionConfigAsset asset)
        {
            Color accentColor = new Color(0.35f, 0.70f, 0.95f, 1f);
            BeginCard("核心资产与时间轴", ref _coreAssetsExpanded, accentColor);
            if (_coreAssetsExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_idProp, new GUIContent("动作编号", "动作在数据系统中的唯一数字编号"));
                EditorGUILayout.PropertyField(_nameProp, new GUIContent("动作标识", "动作的描述性文本别名"));
                EditorGUILayout.PropertyField(_domainIdProp, new GUIContent("动作领域", "宏观动作领域分类 供阻尼 受击衰减 闪避计时等业务使用"));

                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(_actionTimelineSOProp, new GUIContent("时间轴资产", "时间轴数据资产 播放器优先解析此资源"));
                EditorGUILayout.PropertyField(_timelineAssetProp, new GUIContent("时间轴文本", "标准化文本数据资产"));

                if (asset.actionTimelineSO == null && asset.TimelineAsset == null)
                {
                    EditorGUILayout.HelpBox("当前动作尚未配置任何时间轴资源 动作播放器无法解析动作帧与关键事件", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
            EndCard(_coreAssetsExpanded);
        }

        // 卡片二 动作完成策略
        private void DrawCompletionCard(ActionConfigAsset asset)
        {
            Color accentColor = new Color(0.40f, 0.85f, 0.55f, 1f);
            BeginCard("动作完成策略", ref _stateTransitionExpanded, accentColor);
            if (_stateTransitionExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_completeModeProp, new GUIContent("完成退出策略", "当前动作播放结束自然退出时的流转逻辑"));

                ActionCompleteMode mode = (ActionCompleteMode)_completeModeProp.enumValueIndex;
                if (mode == ActionCompleteMode.TransitToAction)
                {
                    EditorGUILayout.Space(2f);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("自动衔接后续动作配置", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(_completeActionProp, new GUIContent("目标动作", "动作自然播放完成后强制衔接的目标动作"));
                        if (_completeActionProp.objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox("未配置目标动作 退出策略设为了衔接动作但目标动作未配置 动作结束后将失去流转目标", MessageType.Error);
                        }
                    }
                }
                else if (mode == ActionCompleteMode.Stay)
                {
                    EditorGUILayout.HelpBox("动作结束后将保持停留在最后一帧或循环播放 需由监控器或外部指令强制打断退出", MessageType.None);
                }

                if (_inheritModeProp != null)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.PropertyField(_inheritModeProp, new GUIContent("派生继承策略", "当前动作对于前置动作派生路由的继承策略"));
                }
                EditorGUI.indentLevel--;
            }
            EndCard(_stateTransitionExpanded);
        }

        // 卡片三 过渡表现概览
        private void DrawTransitionsOverviewCard(ActionConfigAsset asset)
        {
            var table = asset.TransitionTable;
            int count = table?.Items != null ? table.Items.Count : 0;
            string countBadge = count > 0 ? $"已配置 {count} 个目标" : "使用默认起手混合";

            Color accentColor = new Color(0.95f, 0.70f, 0.25f, 1f);
            BeginCard($"过渡表现概览  {countBadge}", ref _transitionsOverviewExpanded, accentColor);
            if (_transitionsOverviewExpanded)
            {
                if (count == 0)
                {
                    EditorGUILayout.HelpBox("暂未配置针对任何特定动作的过渡覆盖参数 流转到其他动作时将使用目标动作自身的起手混合时长", MessageType.None);
                }
                else
                {
                    EditorGUILayout.LabelField("当前动作支持的定制过渡目标明细", EditorStyles.miniBoldLabel);
                    for (int i = 0; i < count; i++)
                    {
                        var item = table.Items[i];
                        if (item == null) continue;

                        string targetName = item.TargetAction != null ? item.TargetAction.name : "未配置目标动作";
                        string blendStr = item.BlendDuration >= 0f ? $"{item.BlendDuration:0.00}秒" : "默认";
                        string exitStr = item.HasEndTime ? $"{item.EndTime:0.00}秒 提前退出" : "自然退出";
                        string startStr = item.HasStartTime ? $"{item.StartTime:0.00}秒 跳过起手" : "起手为零";

                        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                        {
                            EditorGUILayout.LabelField(targetName, EditorStyles.boldLabel, GUILayout.Width(220f));
                            EditorGUILayout.LabelField($"混合 {blendStr}", GUILayout.Width(100f));
                            EditorGUILayout.LabelField($"退出 {exitStr}", GUILayout.Width(120f));
                            EditorGUILayout.LabelField($"切入 {startStr}");
                            if (item.TargetAction != null)
                            {
                                if (GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(40f)))
                                {
                                    EditorGUIUtility.PingObject(item.TargetAction);
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space(4f);
                if (GUILayout.Button("在动作过渡工作台中精确调整曲线与起止点", EditorStyles.miniButton, GUILayout.Height(22f)))
                {
                    OpenInTransitionWorkbench(asset);
                }
            }
            EndCard(_transitionsOverviewExpanded);
        }

        // 卡片四 派生路由大盘
        private void DrawActionRoutesCard(ActionConfigAsset asset)
        {
            int routeCount = _routesProp != null && _routesProp.isArray ? _routesProp.arraySize : 0;

            int immediateCount = 0;
            int deferredCount = 0;
            if (asset.Routes != null)
            {
                foreach (var r in asset.Routes)
                {
                    if (r == null) continue;
                    if (r.ArbitrationTiming == RouteArbitrationTiming.Immediate) immediateCount++;
                    else deferredCount++;
                }
            }

            string statsBadge = $"共 {routeCount} 条 即时 {immediateCount} 延迟 {deferredCount}";
            Color accentColor = new Color(0.25f, 0.65f, 0.95f, 1f);

            BeginCard($"派生路由大盘  {statsBadge}", ref _actionRoutesExpanded, accentColor);
            if (_actionRoutesExpanded)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("全部展开", EditorStyles.miniButtonLeft, GUILayout.Height(20f)))
                    {
                        SetAllRoutesExpanded(true);
                    }
                    if (GUILayout.Button("全部折叠", EditorStyles.miniButtonMid, GUILayout.Height(20f)))
                    {
                        SetAllRoutesExpanded(false);
                    }
                    if (GUILayout.Button("新增派生路由", EditorStyles.miniButtonRight, GUILayout.Height(20f)))
                    {
                        AddNewRoute();
                    }
                }

                EditorGUILayout.Space(4f);

                if (routeCount == 0)
                {
                    EditorGUILayout.HelpBox("当前动作暂无配置任何派生路由 角色在此动作期间将无法通过输入或窗口触发连段流转", MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < routeCount; i++)
                    {
                        SerializedProperty routeElem = _routesProp.GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(routeElem, true);
                        EditorGUILayout.Space(2f);
                    }
                }
            }
            EndCard(_actionRoutesExpanded);
        }

        // 卡片五 通用路由集复用
        private void DrawSharedRouteSetsCard(ActionConfigAsset asset)
        {
            int setCount = _routeSetsProp != null && _routeSetsProp.isArray ? _routeSetsProp.arraySize : 0;
            string countBadge = $"已挂载 {setCount} 个集合";

            Color accentColor = new Color(0.70f, 0.45f, 0.90f, 1f);
            BeginCard($"通用路由集复用  {countBadge}", ref _sharedRouteSetsExpanded, accentColor);
            if (_sharedRouteSetsExpanded)
            {
                EditorGUILayout.HelpBox("通用路由集通常用于闪避 移动 转向等大盘共享动作 挂载后此动作会自动继承对应集合中的全部有效路由", MessageType.None);
                EditorGUILayout.PropertyField(_routeSetsProp, new GUIContent("挂载通用路由集"), true);
            }
            EndCard(_sharedRouteSetsExpanded);
        }

        // 辅助卡片容器
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

        // 快捷操作交互
        private void OpenInSkillEditor(ActionConfigAsset asset)
        {
            if (asset == null) return;

            if (asset.actionTimelineSO != null)
            {
                AssetDatabase.OpenAsset(asset.actionTimelineSO);
                return;
            }

            if (asset.TimelineAsset != null)
            {
                string jsonPath = AssetDatabase.GetAssetPath(asset.TimelineAsset);
                if (!string.IsNullOrEmpty(jsonPath))
                {
                    string soPath = jsonPath
                        .Replace("/JSON/ActionTimelines/", "/ScriptableObjects/ActionTimelines/")
                        .Replace(".json", ".asset");

                    var so = AssetDatabase.LoadAssetAtPath<ATEditor.ActionTimeline>(soPath);
                    if (so != null)
                    {
                        asset.actionTimelineSO = so;
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.OpenAsset(so);
                        return;
                    }

                    AssetDatabase.OpenAsset(asset.TimelineAsset);
                    return;
                }
            }

            EditorUtility.DisplayDialog("提示", "当前动作资产尚未关联任何时间轴数据 请先在核心资产与时间轴卡片中配置时间轴资产", "确定");
        }

        private void OpenInTransitionWorkbench(ActionConfigAsset asset)
        {
            if (asset == null) return;
            Selection.activeObject = asset;
            ActionTransitionWorkbenchWindow.OpenWindow();
        }

        private void PerformRoutesHealthCheck(ActionConfigAsset asset)
        {
            if (asset == null) return;

            int totalRoutes = asset.Routes != null ? asset.Routes.Count : 0;
            int missingTargetCount = 0;
            int monsterInvalidCount = 0;
            bool isMonster = asset is MonsterActionConfigAsset;

            if (asset.Routes != null)
            {
                foreach (var route in asset.Routes)
                {
                    if (route == null) continue;
                    var actTarget = route.GetActionTarget();
                    if (actTarget == null || actTarget.Action == null)
                    {
                        missingTargetCount++;
                    }

                    if (isMonster)
                    {
                        if (route.TriggerStrategy is IntentCommandTrigger)
                        {
                            monsterInvalidCount++;
                        }
                    }
                }
            }

            if (missingTargetCount == 0 && monsterInvalidCount == 0)
            {
                EditorUtility.DisplayDialog("路由健康体检结果", $"检查完成 共检测 {totalRoutes} 条派生路由 全部配置合规有效 未发现异常问题", "确定");
            }
            else
            {
                string issues = "检查完成 共发现以下配置异常\n\n";
                if (missingTargetCount > 0)
                {
                    issues += $"发现 {missingTargetCount} 条路由未配置目标动作资产\n";
                }
                if (monsterInvalidCount > 0)
                {
                    issues += $"发现 {monsterInvalidCount} 条怪物路由非法配置了玩家输入触发器\n";
                }
                issues += "\n建议在下方派生路由大盘中修正上述异常";
                EditorUtility.DisplayDialog("路由健康体检结果", issues, "确定");
            }
        }

        private void SetAllRoutesExpanded(bool expand)
        {
            if (_routesProp == null || !_routesProp.isArray) return;
            for (int i = 0; i < _routesProp.arraySize; i++)
            {
                _routesProp.GetArrayElementAtIndex(i).isExpanded = expand;
            }
        }

        private void AddNewRoute()
        {
            if (_routesProp == null || !_routesProp.isArray) return;
            int newIdx = _routesProp.arraySize;
            _routesProp.InsertArrayElementAtIndex(newIdx);
            var newRoute = _routesProp.GetArrayElementAtIndex(newIdx);
            newRoute.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
