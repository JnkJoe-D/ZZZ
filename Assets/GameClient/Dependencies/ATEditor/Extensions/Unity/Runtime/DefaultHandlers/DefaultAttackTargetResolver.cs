using UnityEngine;

namespace ATEditor
{
    /// <summary>默认目标解析器：基于 Unity 原生物理刚体/根节点归拢目标；无需受击对象挂载额外组件。</summary>
    public sealed class DefaultAttackTargetResolver : IAttackTargetResolver
    {
        public UnityEngine.Object ResolveTarget(GameObject owner, Collider collider)
        {
            if (collider == null) return null;
            return collider.attachedRigidbody != null
                ? (UnityEngine.Object)collider.attachedRigidbody.gameObject
                : (UnityEngine.Object)collider.transform.root.gameObject;
        }
    }
}
