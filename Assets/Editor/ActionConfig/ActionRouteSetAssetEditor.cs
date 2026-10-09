using System;
using System.Collections.Generic;
using Game.Framework;
using Game.GamePlay;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.ActionConfig
{
    /// <summary>
    /// 通用路由集合资产自定义检视面板
    /// 包含领域看板 统计概览 批量折叠展开与健康体检
    /// </summary>
    [CustomEditor(typeof(ActionRouteSetAsset))]
    public class ActionRouteSetAssetEditor : UnityEditor.Editor
    {
        // 序列化属性缓存
        private SerializedProperty _targetScopeProp;
        private SerializedProperty _routesProp;

        // 折叠卡片状态缓存
        private bool _settingsExpanded = true;
        private bool _routesExpanded = true;

        private void OnEnable()
        {
            _targetScopeProp = serializedObject.FindProperty("TargetScope");
            _routesProp = serializedObject.FindProperty("Routes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ActionRouteSetAsset asset = (ActionRouteSetAsset)target;
            if (asset == null) return;

            // 统计关键指标
            GatherRouteStatistics(asset, out int totalRoutes, out int totalActions, out int validActions, out int totalEvents, out int immediateCount, out int deferredCount, out int inputTriggerCount, out int autoTriggerCount);

            // 顶部核心看板
            DrawHeroBanner(asset, totalRoutes, validActions, totalEvents);

            EditorGUILayout.Space(6f);

            // 快捷工具栏与体检
            DrawQuickActionsToolbar(asset);

            EditorGUILayout.Space(8f);

            // 卡片一 领域与基础配置
            DrawSettingsCard(asset, immediateCount, deferredCount, inputTriggerCount, autoTriggerCount);

            EditorGUILayout.Space(6f);

            // 卡片二 路由大盘
            DrawRoutesCard(asset, totalRoutes, immediateCount, deferredCount);

            EditorGUILayout.Space(10f);

            serializedObject.ApplyModifiedProperties();
        }

        // 核心看板
        private void DrawHeroBanner(ActionRouteSetAsset asset, int totalRoutes, int validActions, int totalEvents)
        {
            bool isMonsterOnly = asset.TargetScope == ConditionScope.Monster;
            bool isRoleOnly = asset.TargetScope == ConditionScope.Role;

            Color bannerBg = EditorGUIUtility.isProSkin
                ? (isRoleOnly ? new Color(0.12f, 0.22f, 0.32f, 0.9f) : (isMonsterOnly ? new Color(0.32f, 0.16f, 0.16f, 0.9f) : new Color(0.18f, 0.22f, 0.28f, 0.9f)))
                : (isRoleOnly ? new Color(0.82f, 0.90f, 0.98f, 0.95f) : (isMonsterOnly ? new Color(0.98f, 0.86f, 0.86f, 0.95f) : new Color(0.88f, 0.90f, 0.94f, 0.95f)));

            Color accentBarColor = isRoleOnly
                ? new Color(0.20f, 0.65f, 0.95f, 1f)
                : (isMonsterOnly ? new Color(0.95f, 0.35f, 0.25f, 1f) : new Color(0.40f, 0.70f, 0.85f, 1f));

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 48f);
                EditorGUI.DrawRect(rect, bannerBg);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 5f, rect.height), accentBarColor);

                string badge = isRoleOnly ? "角色通用路由集" : (isMonsterOnly ? "怪物通用路由集" : "全局通用路由集");

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

                string scopeDesc = $"适用领域: {GetScopeChineseName(asset.TargetScope)}";
                string routesDesc = $"路由总数: {totalRoutes} 条";
                string actionsDesc = $"动作目标: {validActions} 项";
                string eventsDesc = $"伴随事件: {totalEvents} 项";
                string metaLine = $"{scopeDesc}     {routesDesc}     {actionsDesc}     {eventsDesc}";

                Rect metaRect = new Rect(rect.x + 12f, rect.y + 26f, rect.width - 20f, 16f);
                EditorGUI.LabelField(metaRect, metaLine, subStyle);
            }
        }

        // 快捷工具栏
        private void DrawQuickActionsToolbar(ActionRouteSetAsset asset)
        {
            bool hasMonsterIllegal = HasMonsterRouteViolations(asset, out int illegalCount);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUIContent healthCheckBtn = new GUIContent("路由集健康体检", "检查当前路由集中是否存在未配置动作非法按键条件或未指定事件等隐患");
                if (GUILayout.Button(healthCheckBtn, hasMonsterIllegal ? EditorStyles.miniButtonLeft : EditorStyles.miniButton, GUILayout.Height(24f)))
                {
                    PerformRouteSetHealthCheck(asset);
                }

                if (hasMonsterIllegal)
                {
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.55f, 0.55f, 1f);
                    GUIContent fixBtn = new GUIContent($"一键清理非法条件  发现 {illegalCount} 处", "自动清理怪物路由集中误配的按键输入触发器与角色条件");
                    if (GUILayout.Button(fixBtn, EditorStyles.miniButtonRight, GUILayout.Height(24f)))
                    {
                        CleanMonsterViolations(asset);
                    }
                    GUI.backgroundColor = oldColor;
                }
            }
        }

        // 卡片一 领域与基础配置
        private void DrawSettingsCard(ActionRouteSetAsset asset, int immediateCount, int deferredCount, int inputTriggerCount, int autoTriggerCount)
        {
            Color accentColor = new Color(0.35f, 0.70f, 0.95f, 1f);
            BeginCard("领域与配置规范", ref _settingsExpanded, accentColor);
            if (_settingsExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_targetScopeProp, new GUIContent("适用实体领域", "指定本通用路由集的适用实体领域 用于在检视面板中智能过滤条件和触发器"));

                EditorGUILayout.Space(4f);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("路由集规则特征分布", EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"裁决时机: 即时裁决 {immediateCount} 条     延迟裁决 {deferredCount} 条");
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"触发类型: 玩家输入 {inputTriggerCount} 条     自动窗口 {autoTriggerCount} 条");
                    }
                }

                EditorGUI.indentLevel--;
            }
            EndCard(_settingsExpanded);
        }

        // 卡片二 路由大盘
        private void DrawRoutesCard(ActionRouteSetAsset asset, int totalRoutes, int immediateCount, int deferredCount)
        {
            string statsBadge = $"共 {totalRoutes} 条 即时 {immediateCount} 延迟 {deferredCount}";
            Color accentColor = new Color(0.25f, 0.65f, 0.95f, 1f);

            BeginCard($"路由列表大盘  {statsBadge}", ref _routesExpanded, accentColor);
            if (_routesExpanded)
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
                    if (GUILayout.Button("新增路由", EditorStyles.miniButtonRight, GUILayout.Height(20f)))
                    {
                        AddNewRoute();
                    }
                }

                EditorGUILayout.Space(4f);

                if (totalRoutes == 0)
                {
                    EditorGUILayout.HelpBox("当前路由集暂无配置任何路由项 挂载此集合的动作将无法获得派生流转能力", MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < totalRoutes; i++)
                    {
                        SerializedProperty routeElem = _routesProp.GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(routeElem, true);
                        EditorGUILayout.Space(2f);
                    }
                }
            }
            EndCard(_routesExpanded);
        }

        // 统计数据采集
        private static void GatherRouteStatistics(ActionRouteSetAsset asset, out int totalRoutes, out int totalActions, out int validActions, out int totalEvents, out int immediateCount, out int deferredCount, out int inputTriggerCount, out int autoTriggerCount)
        {
            totalRoutes = asset.Routes != null ? asset.Routes.Count : 0;
            totalActions = 0;
            validActions = 0;
            totalEvents = 0;
            immediateCount = 0;
            deferredCount = 0;
            inputTriggerCount = 0;
            autoTriggerCount = 0;

            if (asset.Routes == null) return;

            for (int i = 0; i < asset.Routes.Count; i++)
            {
                var route = asset.Routes[i];
                if (route == null) continue;

                if (route.ArbitrationTiming == RouteArbitrationTiming.Immediate) immediateCount++;
                else deferredCount++;

                if (route.TriggerStrategy is IntentCommandTrigger) inputTriggerCount++;
                else if (route.TriggerStrategy != null) autoTriggerCount++;

                if (route.Targets != null)
                {
                    for (int t = 0; t < route.Targets.Count; t++)
                    {
                        var target = route.Targets[t];
                        if (target is ActionRouteTarget act)
                        {
                            totalActions++;
                            if (act.Action != null) validActions++;
                        }
                        else if (target is EventRouteTarget)
                        {
                            totalEvents++;
                        }
                    }
                }
            }
        }

        // 领域显示名转换
        private static string GetScopeChineseName(ConditionScope scope)
        {
            if (scope == ConditionScope.Role) return "角色专属";
            if (scope == ConditionScope.Monster) return "怪物专属";
            if (scope == ConditionScope.Common) return "角色与怪物通用";
            if (scope == ConditionScope.All) return "全部领域";
            return "未指定领域";
        }

        // 触发策略输入条件提取辅助
        private static List<RouteModifierCheck> GetTriggerInputConditions(IRouteTrigger trigger)
        {
            if (trigger is IntentCommandTrigger ict) return ict.InputConditions;
            if (trigger is SystemEventTrigger set) return set.InputConditions;
            if (trigger is AutoTransitionTrigger att) return att.InputConditions;
            if (trigger is ConditionOnlyTrigger cot) return cot.InputConditions;
            return null;
        }

        // 怪物非法配置检测
        private static bool HasMonsterRouteViolations(ActionRouteSetAsset asset, out int violationCount)
        {
            violationCount = 0;
            if (asset.TargetScope != ConditionScope.Monster || asset.Routes == null) return false;

            for (int i = 0; i < asset.Routes.Count; i++)
            {
                var route = asset.Routes[i];
                if (route == null) continue;

                if (route.TriggerStrategy is IntentCommandTrigger || route.TriggerStrategy is ConditionOnlyTrigger)
                {
                    violationCount++;
                }
                else
                {
                    var conds = GetTriggerInputConditions(route.TriggerStrategy);
                    if (conds != null)
                    {
                        for (int m = 0; m < conds.Count; m++)
                        {
                            var check = conds[m];
                            if (check != null && check.Category == ModifierCategory.KeyState)
                            {
                                violationCount++;
                            }
                        }
                    }
                }

                if (route.ExtraConditions != null)
                {
                    for (int c = 0; c < route.ExtraConditions.Count; c++)
                    {
                        var cond = route.ExtraConditions[c];
                        if (cond is RoleConditionBase) violationCount++;
                    }
                }
            }

            return violationCount > 0;
        }

        // 一键清理怪物非法配置
        private void CleanMonsterViolations(ActionRouteSetAsset asset)
        {
            if (asset == null || asset.Routes == null) return;
            Undo.RecordObject(asset, "清理怪物路由集非法条件");

            for (int i = 0; i < asset.Routes.Count; i++)
            {
                var route = asset.Routes[i];
                if (route == null) continue;

                if (route.TriggerStrategy is IntentCommandTrigger || route.TriggerStrategy is ConditionOnlyTrigger)
                {
                    route.TriggerStrategy = null;
                }
                else
                {
                    var conds = GetTriggerInputConditions(route.TriggerStrategy);
                    if (conds != null)
                    {
                        for (int m = conds.Count - 1; m >= 0; m--)
                        {
                            if (conds[m] != null && conds[m].Category == ModifierCategory.KeyState)
                            {
                                conds.RemoveAt(m);
                            }
                        }
                    }
                }

                if (route.ExtraConditions != null)
                {
                    for (int c = route.ExtraConditions.Count - 1; c >= 0; c--)
                    {
                        if (route.ExtraConditions[c] is RoleConditionBase)
                        {
                            route.ExtraConditions.RemoveAt(c);
                        }
                    }
                }
            }

            EditorUtility.SetDirty(asset);
            serializedObject.Update();
            EditorUtility.DisplayDialog("提示", "已成功清理全部非法配置", "确定");
        }

        // 路由集健康体检
        private void PerformRouteSetHealthCheck(ActionRouteSetAsset asset)
        {
            if (asset == null) return;

            int totalRoutes = asset.Routes != null ? asset.Routes.Count : 0;
            int missingActionCount = 0;
            int missingEventCount = 0;
            int monsterIllegalCount = 0;
            bool isMonster = asset.TargetScope == ConditionScope.Monster;

            if (asset.Routes != null)
            {
                for (int i = 0; i < asset.Routes.Count; i++)
                {
                    var route = asset.Routes[i];
                    if (route == null) continue;

                    var act = route.GetActionTarget();
                    if (act == null || act.Action == null)
                    {
                        missingActionCount++;
                    }

                    if (route.Targets != null)
                    {
                        for (int t = 0; t < route.Targets.Count; t++)
                        {
                            if (route.Targets[t] is EventRouteTarget evt && evt.RouteExecuteEvent == ExecuteEvent.None)
                            {
                                missingEventCount++;
                            }
                        }
                    }

                    if (isMonster)
                    {
                        if (route.TriggerStrategy is IntentCommandTrigger || route.TriggerStrategy is ConditionOnlyTrigger)
                        {
                            monsterIllegalCount++;
                        }
                        else
                        {
                            var conds = GetTriggerInputConditions(route.TriggerStrategy);
                            if (conds != null)
                            {
                                for (int m = 0; m < conds.Count; m++)
                                {
                                    var check = conds[m];
                                    if (check != null && check.Category == ModifierCategory.KeyState)
                                    {
                                        monsterIllegalCount++;
                                    }
                                }
                            }
                        }

                        if (route.ExtraConditions != null)
                        {
                            for (int c = 0; c < route.ExtraConditions.Count; c++)
                            {
                                if (route.ExtraConditions[c] is RoleConditionBase) monsterIllegalCount++;
                            }
                        }
                    }
                }
            }

            if (missingActionCount == 0 && missingEventCount == 0 && monsterIllegalCount == 0)
            {
                EditorUtility.DisplayDialog("路由集健康体检结果", $"检查完成 共检测 {totalRoutes} 条路由 全部配置合规有效 未发现异常隐患", "确定");
            }
            else
            {
                string report = "检查完成 共发现以下配置异常\n\n";
                if (missingActionCount > 0)
                {
                    report += $"发现 {missingActionCount} 条路由未配置目标动作资产\n";
                }
                if (missingEventCount > 0)
                {
                    report += $"发现 {missingEventCount} 处事件目标未选择具体系统事件\n";
                }
                if (monsterIllegalCount > 0)
                {
                    report += $"发现 {monsterIllegalCount} 处怪物路由集非法配置了按键输入或角色条件\n";
                }
                report += "\n建议在下方列表中逐项修正上述异常";
                EditorUtility.DisplayDialog("路由集健康体检结果", report, "确定");
            }
        }

        // 批量折叠展开控制
        private void SetAllRoutesExpanded(bool expand)
        {
            if (_routesProp == null || !_routesProp.isArray) return;
            for (int i = 0; i < _routesProp.arraySize; i++)
            {
                _routesProp.GetArrayElementAtIndex(i).isExpanded = expand;
            }
        }

        // 新增路由
        private void AddNewRoute()
        {
            if (_routesProp == null || !_routesProp.isArray) return;
            int newIdx = _routesProp.arraySize;
            _routesProp.InsertArrayElementAtIndex(newIdx);
            var newRoute = _routesProp.GetArrayElementAtIndex(newIdx);
            newRoute.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
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
    }
}
