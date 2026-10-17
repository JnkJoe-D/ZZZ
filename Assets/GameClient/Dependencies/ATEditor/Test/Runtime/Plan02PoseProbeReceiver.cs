using UnityEngine;

namespace ATEditor.Test
{
    /// <summary>PlayMode 采样探针，只记录被授权的逻辑增量；不参与正式宿主装配。</summary>
    public sealed class Plan02PoseProbeReceiver : MonoBehaviour
    {
        public bool Capture;
        public Vector3 Delta;
        public Quaternion Rotation = Quaternion.identity;
        private void OnAnimatorMove()
        {
            if (!Capture) return;
            var animator = GetComponent<Animator>();
            Delta += animator.deltaPosition;
            Rotation *= animator.deltaRotation;
        }
    }
}
