using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(MotionFilterClip))]
    public class MotionFilterClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showFilter = true;

        public override void DrawInspector(ClipBase clip)
        {
            var motionClip = clip as MotionFilterClip;
            if (motionClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 位移过滤设置卡片
            _showFilter = EditorGUILayout.Foldout(_showFilter, "动画位移过滤设置", true, EditorStyles.foldoutHeader);
            if (_showFilter)
            {
                EditorGUILayout.BeginVertical("box");
                motionClip.localDeltaFilterMode = (MotionWindowLocalDeltaFilterMode)EditorGUILayout.EnumPopup("局部向轴变化过滤", motionClip.localDeltaFilterMode);
                
                string tip = motionClip.localDeltaFilterMode switch
                {
                    MotionWindowLocalDeltaFilterMode.None => "不进行过滤，保留全部动画 RootMotion 位移",
                    MotionWindowLocalDeltaFilterMode.ZeroLocalX => "锁定局部 X 轴位移（消除左右横移偏差）",
                    MotionWindowLocalDeltaFilterMode.ZeroLocalZ => "锁定局部 Z 轴位移（消除前后移动）",
                    MotionWindowLocalDeltaFilterMode.ZeroLocalXZ => "锁定局部 XZ 平面位移（仅保留垂直 Y 轴运动）",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(tip))
                {
                    EditorGUILayout.HelpBox(tip, MessageType.Info);
                }

                EditorGUILayout.Space();
                motionClip.collisionMode = (RootMotionCollisionMode)EditorGUILayout.EnumPopup("物理约束碰撞策略", motionClip.collisionMode);
                
                string collisionTip = motionClip.collisionMode switch
                {
                    RootMotionCollisionMode.DefaultSlide => "不进行碰撞预检测，完全交给 CharacterController 处理，允许沿碰撞面滑动。",
                    RootMotionCollisionMode.StopAtObstacle => "沿运动方向进行预检测，遇到障碍物截断位移，不允许侧滑。",
                    RootMotionCollisionMode.IgnorePreCheck => "忽略碰撞预检测。",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(collisionTip))
                {
                    EditorGUILayout.HelpBox(collisionTip, MessageType.Info);
                }

                if (motionClip.collisionMode == RootMotionCollisionMode.StopAtObstacle)
                {
                    int obsMask = EditorGUILayout.MaskField("障碍物阻挡层", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(motionClip.obstacleMask), InternalEditorUtility.layers);
                    motionClip.obstacleMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(obsMask);
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Motion Filter Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Motion Filter Clip");
            }
        }
    }
}
