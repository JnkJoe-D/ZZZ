using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>挂载于 Animator 节点的根运动桥接器；仅在所属 Director 需要拦截过滤时消费根运动，未绑定/未拦截时保留 Unity 原生根运动行为。</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class AnimatorRootMotionBridge : MonoBehaviour
    {
        private object _owner;
        private Action<Animator> _consume;
        private Animator _animator;
        internal bool Capture { get; set; }

        internal void Bind(object owner, Action<Animator> consume)
        {
            if (_owner != null && !ReferenceEquals(_owner, owner))
                throw new InvalidOperationException("同一 Animator 的根运动只能由一个 ActionDirector 持有。");
            _animator = GetComponent<Animator>();
            _owner = owner;
            _consume = consume;
        }

        internal void Unbind(object owner)
        {
            if (!ReferenceEquals(_owner, owner)) return;
            Capture = false;
            _consume = null;
            _owner = null;
        }

        private void OnAnimatorMove()
        {
            if (_animator == null) return;
            if (_owner == null)
            {
                if (_animator.applyRootMotion) _animator.ApplyBuiltinRootMotion();
            }
            else if (Capture) _consume?.Invoke(_animator);
            else if (_animator.applyRootMotion) _animator.ApplyBuiltinRootMotion();
        }
    }
}
