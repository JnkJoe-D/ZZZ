using UnityEngine;

namespace Game.GamePlay
{
    public class CameraPointBinder : MonoBehaviour
    {
        [SerializeField]
        private Transform followPoint;

        [SerializeField]
        private Transform lookAtPoint;

        public Transform RootTransform => transform;
        public Transform FollowPoint => followPoint != null ? followPoint : transform;
        public Transform LookAtPoint => lookAtPoint != null ? lookAtPoint : FollowPoint;
    }
}
