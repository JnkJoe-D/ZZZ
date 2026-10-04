using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(ParryWindowClip))]
    public class ParryWindowClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showSettings = true;

        public override void DrawInspector(ClipBase clip)
        {
            var parryClip = clip as ParryWindowClip;
            if (parryClip == null) return;

            EditorGUI.BeginChangeCheck();

            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            _showSettings = EditorGUILayout.Foldout(_showSettings, "招架防御窗口说明", true, EditorStyles.foldoutHeader);
            if (_showSettings)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox(
                    "【纯防御时间窗口声明器】\n" +
                    "• 在此时间窗口内，角色身体处于招架迎击状态 (IsParrying=true)。\n" +
                    "• 怪物打击盒打入时直接触发拼刀；窗口结束后进入收招后摇，不再享受招架。\n" +
                    "• 招架反制动作、失衡效果 (HitEffectId) 与顿帧时长已收敛至角色配置 (RoleAssistConfig.ParryLight / ParryHeavy) 权威驱动，无需在此配置。", 
                    MessageType.Info);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Parry Window Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Parry Window Clip");
            }
        }
    }
}
