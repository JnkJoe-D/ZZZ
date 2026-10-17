using System;
using UnityEditor;
using UnityEngine;
using ATEditor;
using Object = UnityEngine.Object;

namespace ATEditor.Editor
{
    /// <summary>
    /// 卡片绘制上下文，提供卡片在 Inspector 与 SceneView 绘制所需的宿主环境与变换基准
    /// </summary>
    public class CardDrawContext
    {
        /// <summary> 所属的片段宿主 </summary>
        public ClipBase Clip { get; set; }

        /// <summary> 当前编辑的时间轴资产 </summary>
        public ActionTimeline Timeline { get; set; }

        /// <summary> 编辑器全局状态 </summary>
        public ATEditorState State { get; set; }

        /// <summary> Undo 记录上下文目标对象 </summary>
        public Object[] UndoContext { get; set; }

        /// <summary> 
        /// 宿主提供的空间基准变换（供 SceneView Gizmos/Handles 局部坐标定位）
        /// 例如 HitClip 为检测盒最终世界位姿，SpawnClip 为生成物运动原点位姿
        /// </summary>
        public Pose OriginPose { get; set; } = Pose.identity;

        /// <summary>
        /// 是否需要由卡片叠加其自身的 positionOffset 与 rotationOffset
        /// 默认为 true（如 SpawnClip 传入原点由卡片叠偏）；若宿主已预解算最终盒体（如 HitClip），则设为 false
        /// </summary>
        public bool NeedsApplyCardOffset { get; set; } = true;

        /// <summary> 当前选中的预览角色模型实例 </summary>
        public GameObject PreviewTarget { get; set; }

        /// <summary>
        /// 标记资产已修改并记录 Undo
        /// </summary>
        public void MarkDirty(string undoActionName)
        {
            if (UndoContext != null && UndoContext.Length > 0)
            {
                Undo.RecordObjects(UndoContext, undoActionName);
                foreach (var obj in UndoContext)
                {
                    if (obj != null) EditorUtility.SetDirty(obj);
                }
            }

            if (Timeline != null)
            {
                Undo.RecordObject(Timeline, undoActionName);
                EditorUtility.SetDirty(Timeline);
            }
        }
    }
}
