using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - Overview 概览看板视图
    /// 呈现当前选定角色的四维核心统计卡片、元数据身份与四级生效路径快照。
    /// 遵循严格固定布局，杜绝文本长度波动引起的界面抖动。
    /// </summary>
    public static class WorkbenchOverviewView
    {
        private static Vector2 _scrollPos;

        public static void Draw(WorkbenchCharacterContext context, System.Action onRequireRefresh)
        {
            if (context == null)
            {
                EditorGUILayout.HelpBox("请先在顶部上下文栏选择一个有效的角色工作区。", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                // 1. 顶部四格固定宽度指标卡片
                DrawStatCardsRow(context);

                EditorGUILayout.Space(6f);

                // 2. 角色身份与 Luban 数据绑定卡片
                DrawCharacterIdentityCard(context);

                // 3. 生效配置与路径快照卡片 (带固定宽度来源徽标)
                DrawEffectiveConfigSnapshotCard(context);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawStatCardsRow(WorkbenchCharacterContext context)
        {
            EditorGUILayout.BeginHorizontal();
            {
                float cardWidth = 180f;
                WorkbenchUIStyles.DrawStatCard("动作资产总数", $"{context.ActionCount} 项", "动作资产条目", cardWidth, new Color(0.2f, 0.6f, 1f));
                WorkbenchUIStyles.DrawStatCard("双轨完全绑定", $"{context.FullyLinkedActionCount} 项", "双轨数据齐全", cardWidth, new Color(0.2f, 0.75f, 0.4f));
                WorkbenchUIStyles.DrawStatCard("时间轴缺失", $"{context.MissingJsonActionCount + context.MissingSoActionCount} 项", "待对齐或补全", cardWidth, new Color(0.9f, 0.5f, 0.1f));
                WorkbenchUIStyles.DrawStatCard("时间轴总计", $"{context.Timelines.Count} 份", "已索引时间轴", cardWidth, new Color(0.6f, 0.4f, 0.8f));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawCharacterIdentityCard(WorkbenchCharacterContext context)
        {
            WorkbenchUIStyles.BeginCard("角色身份与配表关联");
            {
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = WorkbenchUIStyles.LabelWidth;

                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("工作区标识", "当前角色工作区的全局唯一标识"), context.Workspace.Id);
                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("角色分类", "当前角色所属的类别：Role、Monster 或 Common"), context.Workspace.Category);
                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("相对目录", "角色资产在工程根目录下的相对文件夹路径"), context.Workspace.FolderName);

                EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
                {
                    var labelContent = new GUIContent("配表角色绑定", "关联的 Luban 配表角色实体");
                    EditorGUILayout.LabelField(labelContent, GUILayout.Width(WorkbenchUIStyles.LabelWidth));
                    if (context.LubanInfo != null)
                    {
                        EditorGUILayout.LabelField($"[{context.LubanInfo.CharacterId}] {context.LubanInfo.DisplayName}", EditorStyles.boldLabel, GUILayout.Height(18f));
                        WorkbenchUIStyles.DrawStatusBadge(true, "已绑定配表", 80f);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("未绑定配表角色", EditorStyles.label, GUILayout.Height(18f));
                        WorkbenchUIStyles.DrawStatusBadge(false, "未绑定", 80f);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
            WorkbenchUIStyles.EndCard();
        }

        private static void DrawEffectiveConfigSnapshotCard(WorkbenchCharacterContext context)
        {
            WorkbenchUIStyles.BeginCard("四级生效配置快照");
            {
                var cfg = context.EffectiveConfig;

                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("动作资产目录", "生效的角色动作配置文件物理目录"), cfg.ActionConfigDirectory, cfg.ActionConfigDirectorySource);
                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("时间轴数据目录", "生效的时间轴 JSON 序列化数据目录"), cfg.TimelineJsonDirectory, cfg.TimelineJsonDirectorySource);
                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("时间轴资产目录", "生效的时间轴 ScriptableObject 资产目录"), cfg.TimelineAssetDirectory, cfg.TimelineAssetDirectorySource);
                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("配表数据源路径", "当前角色生效的技能/动作配表 JSON 文件路径"), cfg.SkillTablePath, cfg.SkillTablePathSource);
                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("动作命名模板", "当前生效的动作文件命名规则模板"), cfg.NamingTemplate, cfg.NamingTemplateSource);
                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("冲突处理策略", "目标资产已存在时的冲突处理规则：跳过、覆盖或阻断"), cfg.ConflictPolicy.ToString(), cfg.ConflictPolicySource);
                WorkbenchUIStyles.DrawReadOnlyField(new GUIContent("配表角色绑定", "当前生效绑定的配表角色标识"), !string.IsNullOrEmpty(cfg.LubanCharacterId) ? cfg.LubanCharacterId : "未设置", cfg.LubanCharacterIdSource);
            }
            WorkbenchUIStyles.EndCard();
        }
    }
}
