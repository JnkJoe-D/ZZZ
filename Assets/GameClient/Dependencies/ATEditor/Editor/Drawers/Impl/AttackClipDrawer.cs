using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(AttackClip))]
    public sealed class AttackClipDrawer : ClipDrawer
    {
        private readonly CombatDetectionCardDrawer _scopeDrawer = new CombatDetectionCardDrawer();
        public override void DrawInspector(ClipBase clip)
        {
            EditorGUILayout.HelpBox("此片段只输出检测观察。通过 Rigidbody/根节点或外部 Resolver 合并受击体；伤害/阵营/配表由外部解释。", MessageType.Info);
            base.DrawInspector(clip);
        }

        public override void DrawSceneGUI(ClipBase clip, ATEditorState state)
        {
            var attack = (AttackClip)clip;
            var preview = state.PreviewContext;
            var owner = preview?.Owner != null ? preview.Owner : state.previewTarget;
            if (owner == null || attack.scope == null) return;
            attack.scope.CaptureMatrix(owner.transform, preview?.GetService<IBoneGetter>(), out Vector3 position, out Quaternion rotation);
            if (state.previewRunner != null)
                foreach (var instance in state.previewRunner.ActiveProcesses)
                    if (instance.clip == clip && instance.isActive && instance.process is EditorAttackProcess process)
                        process.GetMatrix(out position, out rotation);
            _scopeDrawer.BindData(new CombatDetectionCardData(attack.scope, null));
            _scopeDrawer.DrawSceneGUI(new CardDrawContext
            {
                Clip = clip, State = state, PreviewTarget = owner, UndoContext = UndoContext,
                OriginPose = new Pose(position, rotation), NeedsApplyCardOffset = false
            });
        }
    }
}
