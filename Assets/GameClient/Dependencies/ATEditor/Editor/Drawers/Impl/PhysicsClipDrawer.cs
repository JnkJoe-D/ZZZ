using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(PhysicsClip))]
    public class PhysicsClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showCollision = true;
        private static bool _showGravity = true;
        private static bool _showPushResistance = true;

        public override void DrawInspector(ClipBase clip)
        {
            var physicsClip = clip as PhysicsClip;
            if (physicsClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 碰撞与忽略层级控制
            _showCollision = EditorGUILayout.Foldout(_showCollision, "碰撞与忽略层级", true, EditorStyles.foldoutHeader);
            if (_showCollision)
            {
                DrawCollisionSection(physicsClip);
            }

            // 3. 重力与空中滞空控制
            _showGravity = EditorGUILayout.Foldout(_showGravity, "重力与空中滞空", true, EditorStyles.foldoutHeader);
            if (_showGravity)
            {
                DrawGravitySection(physicsClip);
            }

            // 4. 推挤与霸体抗性控制
            _showPushResistance = EditorGUILayout.Foldout(_showPushResistance, "推挤与霸体抗性", true, EditorStyles.foldoutHeader);
            if (_showPushResistance)
            {
                DrawPushResistanceSection(physicsClip);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Physics Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Physics Clip");
            }
        }

        private void DrawBaseClipSection(PhysicsClip clip)
        {
            EditorGUILayout.BeginVertical("box");
            clip.clipName = EditorGUILayout.TextField("片段名称", clip.clipName);
            clip.isEnabled = EditorGUILayout.Toggle("启用", clip.isEnabled);
            clip.StartTime = Mathf.Max(0f, EditorGUILayout.FloatField("开始时间", clip.StartTime));
            clip.Duration = Mathf.Max(0.01f, EditorGUILayout.FloatField("持续时间", clip.Duration));
            EditorGUILayout.EndVertical();
        }

        private void DrawCollisionSection(PhysicsClip clip)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("碰撞与忽略层级控制", EditorStyles.boldLabel);

            clip.modifyExcludeLayers = EditorGUILayout.ToggleLeft("修改忽略碰撞层级", clip.modifyExcludeLayers, EditorStyles.boldLabel);

            if (clip.modifyExcludeLayers)
            {
                EditorGUI.indentLevel++;

                // 快捷预设按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("穿队友", EditorStyles.miniButtonLeft))
                {
                    clip.excludeLayers = LayerMask.GetMask("Player");
                }
                if (GUILayout.Button("穿敌人", EditorStyles.miniButtonMid))
                {
                    clip.excludeLayers = LayerMask.GetMask("Enemy");
                }
                if (GUILayout.Button("穿队友+敌人", EditorStyles.miniButtonRight))
                {
                    clip.excludeLayers = LayerMask.GetMask("Player", "Enemy");
                }
                EditorGUILayout.EndHorizontal();

                int currentMask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(clip.excludeLayers);
                int newMask = EditorGUILayout.MaskField("忽略的碰撞层级", currentMask, InternalEditorUtility.layers);
                clip.excludeLayers = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newMask);

                clip.restoreExcludeLayersOnExit = EditorGUILayout.Toggle("退出片段时还原层级", clip.restoreExcludeLayersOnExit);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            clip.modifyCollisionEnabled = EditorGUILayout.ToggleLeft("修改碰撞体开关", clip.modifyCollisionEnabled, EditorStyles.boldLabel);

            if (clip.modifyCollisionEnabled)
            {
                EditorGUI.indentLevel++;
                clip.isCollisionEnabled = EditorGUILayout.Toggle("碰撞体启用", clip.isCollisionEnabled);
                clip.restoreCollisionOnExit = EditorGUILayout.Toggle("退出片段时还原开关", clip.restoreCollisionOnExit);
                if (!clip.isCollisionEnabled)
                {
                    EditorGUILayout.HelpBox("提示：禁用碰撞体将使角色处于完全穿透状态（虚化/无碰撞）。", MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGravitySection(PhysicsClip clip)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("重力与空中滞空控制", EditorStyles.boldLabel);

            clip.modifyGravity = EditorGUILayout.ToggleLeft("修改重力倍率", clip.modifyGravity, EditorStyles.boldLabel);

            if (clip.modifyGravity)
            {
                EditorGUI.indentLevel++;

                // 重力预设
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("完全滞空", EditorStyles.miniButtonLeft))
                {
                    clip.gravityScale = 0f;
                    clip.resetVerticalVelocityOnEnter = true;
                }
                if (GUILayout.Button("浮空缓落", EditorStyles.miniButtonMid))
                {
                    clip.gravityScale = 0.3f;
                }
                if (GUILayout.Button("快速下坠", EditorStyles.miniButtonRight))
                {
                    clip.gravityScale = 2.5f;
                }
                EditorGUILayout.EndHorizontal();

                clip.gravityScale = EditorGUILayout.Slider("重力倍率", clip.gravityScale, 0f, 5f);
                clip.resetVerticalVelocityOnEnter = EditorGUILayout.Toggle("进入时清空下落速度", clip.resetVerticalVelocityOnEnter);
                clip.restoreGravityOnExit = EditorGUILayout.Toggle("退出片段时还原重力", clip.restoreGravityOnExit);

                if (Mathf.Approximately(clip.gravityScale, 0f))
                {
                    EditorGUILayout.HelpBox("当前处于无重力完全滞空状态，角色在空中释放技能时将停留在当前高度。", MessageType.None);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPushResistanceSection(PhysicsClip clip)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("推挤与霸体抗性控制", EditorStyles.boldLabel);

            clip.modifyPushResistance = EditorGUILayout.ToggleLeft("修改推挤抗性", clip.modifyPushResistance, EditorStyles.boldLabel);

            if (clip.modifyPushResistance)
            {
                EditorGUI.indentLevel++;
                clip.pushResistance = EditorGUILayout.Slider("推挤抗性", clip.pushResistance, 0f, 1f);
                clip.restorePushResistanceOnExit = EditorGUILayout.Toggle("退出片段时还原抗性", clip.restorePushResistanceOnExit);

                if (clip.pushResistance >= 0.99f)
                {
                    EditorGUILayout.HelpBox("推挤抗性为 1：角色处于完全霸体推挤免疫状态，不可被其他单位挤开。", MessageType.None);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }
}
