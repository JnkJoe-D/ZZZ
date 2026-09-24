using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 角色受击表现基类模块。
    /// 战斗判定与数值结算完全由 HitPipeline 权威掌管；
    /// 本组件仅作为纯粹的只读表现接收者，负责将打断决策转化为受击动作派发。
    /// </summary>
    public abstract class HitReactionComponent : MonoBehaviour, IEntityComponent
    {
        public CharacterEntity OwnerEntity => _entity;

        protected CharacterEntity _entity;
        protected HitReactionRuntimeData _hitData;

        public void OnComponentInit(CharacterEntity owner)
        {
            Init(owner);
        }

        public void OnComponentSpawn() { }
        public void OnComponentDespawn() { }

        public virtual void Init(CharacterEntity entity)
        {
            _entity = entity;
            _hitData = _entity.DataModule?.Get<HitReactionRuntimeData>();
        }

        /// <summary>
        /// 由命中流水线（MotionAndActionPipe）驱动的打断切入钩子。
        /// 直接接收管线上下文，无翻译层。
        /// </summary>
        public virtual void TriggerInterruptedHook(HitPipelineContext ctx)
        {
            OnInterrupted(ctx);
        }

        protected abstract void OnInterrupted(HitPipelineContext ctx);
    }
}
