using UnityEngine;

namespace ATEditor
{
    /// <summary>默认运动端：存在启用的 CharacterController 时由其碰撞解算，否则直接修改 Transform。</summary>
    public sealed class DefaultTransformHandler : ITransformHandler
    {
        private readonly Transform _root;
        private readonly CharacterController _controller;
        private readonly System.Func<IATTargetFinder> _finderGetter;
        private IATTargetFinder _fallbackFinder;
        private LayerMask _excludeLayers;
        public float DeltaTime { get; set; }
        public float TurnSpeed { get; set; } = 15f;

        /// <summary>外部显式注入的目标寻找器；若未设置且未通过构造注入，则回退内部默认实现。</summary>
        public IATTargetFinder TargetFinder { get; set; }

        public DefaultTransformHandler(Transform root, System.Func<IATTargetFinder> finderGetter = null)
        {
            _root = root != null ? root : throw new System.ArgumentNullException(nameof(root));
            _controller = _root.GetComponent<CharacterController>();
            if (_controller != null) _excludeLayers = _controller.excludeLayers;
            _finderGetter = finderGetter;
        }

        public DefaultTransformHandler(GameObject owner, System.Func<IATTargetFinder> finderGetter = null)
            : this(owner != null ? owner.transform : null, finderGetter) { }

        public void Move(Vector3 delta)
        {
            if (_controller != null && _controller.enabled) _controller.Move(delta);
            else _root.position += delta;
        }

        public void SetPosition(Vector3 position)
        {
            bool enabled = _controller != null && _controller.enabled;
            if (enabled) _controller.enabled = false;
            _root.position = position;
            if (enabled) { _controller.enabled = true; Physics.SyncTransforms(); }
        }

        public Vector3 GetPosition() => _root.position;
        public Quaternion GetRotation() => _root.rotation;
        public void SetRotation(Quaternion rotation) => _root.rotation = rotation;

        public Transform GetTarget()
        {
            IATTargetFinder finder = TargetFinder;
            if (finder == null && _finderGetter != null)
            {
                finder = _finderGetter();
            }
            finder ??= _fallbackFinder ??= new DefaultTargetFinder();
            return finder.FindTarget(_root.gameObject);
        }

        public Collider GetCollider() => _controller != null ? _controller : _root.GetComponent<Collider>();

        public Collider GetTargetCollider()
        {
            var target = GetTarget();
            if (target == null) return null;
            if (target.TryGetComponent<CharacterController>(out var cc)) return cc;
            return target.GetComponentInChildren<Collider>();
        }

        public float GetRadius() => Radius(GetCollider());

        public float GetTargetRadius()
        {
            var collider = GetTargetCollider();
            return collider != null ? Radius(collider) : 0.5f;
        }
        private static float Radius(Collider collider)
        {
            if (collider is CharacterController controller)
                return controller.radius * Mathf.Max(Mathf.Abs(controller.transform.lossyScale.x), Mathf.Abs(controller.transform.lossyScale.z));
            return collider != null ? Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z) : 0.5f;
        }
        public float GetHeight() => _controller != null ? _controller.height * Mathf.Abs(_root.lossyScale.y) : GetCollider() != null ? GetCollider().bounds.size.y : 2f;
        public Vector3 GetCenter() => _controller != null ? _controller.center : GetCollider() != null ? _root.InverseTransformPoint(GetCollider().bounds.center) : Vector3.up;
        public LayerMask GetExcludeLayers() => _controller != null ? _controller.excludeLayers : _excludeLayers;
        public void SetExcludeLayers(LayerMask mask) { _excludeLayers = mask; if (_controller != null) _controller.excludeLayers = mask; }
        public void RotateTowards(Quaternion rotation, float speed)
            => SetRotation(Quaternion.Slerp(GetRotation(), rotation, Mathf.Clamp01(DeltaTime * (speed > 0f ? speed : TurnSpeed))));
        public void RotateTo(Vector3 direction, float speed = -1f, Vector3 localOffset = default)
        {
            if (direction.sqrMagnitude > 0.000001f) RotateTowards(Quaternion.LookRotation(direction) * Quaternion.Euler(localOffset), speed);
        }
        public void RotateToImmediately(Vector3 direction, Vector3 localOffset = default)
        {
            if (direction.sqrMagnitude > 0.000001f) SetRotation(Quaternion.LookRotation(direction) * Quaternion.Euler(localOffset));
        }
        public void FaceTo(Vector3 direction, float speed = -1f, Vector3 localOffset = default) => RotateTo(direction, speed, localOffset);
        public void FaceToImmediately(Vector3 direction, Vector3 localOffset = default) => RotateToImmediately(direction, localOffset);
        public void FaceToTarget(Transform target, float speed = -1f, Vector3 localOffset = default)
        { if (target != null) RotateTo(Vector3.ProjectOnPlane(target.position - GetPosition(), Vector3.up), speed, localOffset); }
        public void FaceToTargetImmediately(Transform target, Vector3 localOffset = default)
        { if (target != null) RotateToImmediately(Vector3.ProjectOnPlane(target.position - GetPosition(), Vector3.up), localOffset); }
        public Vector3 InputDirection { get; set; } = Vector3.zero;
        public Vector3 GetCameraDirection() => _root.forward;
        public Vector3 GetInputDirection(bool withCamera) => InputDirection;
    }
}
