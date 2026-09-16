using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
{
    public class MonsterHitReactionModule : HitReactionModule
    {
        public event System.Action OnHitTimestampChanged;

        public override void Init(CharacterEntity entity)
        {
            base.Init(entity);
            if (_entity != null && _entity.ActionPlayer != null)
            {
                _entity.ActionPlayer.OnActionComplete -= HandleActionComplete;
                _entity.ActionPlayer.OnActionComplete += HandleActionComplete;
            }
        }

        private void OnDestroy()
        {
            if (_entity != null && _entity.ActionPlayer != null)
            {
                _entity.ActionPlayer.OnActionComplete -= HandleActionComplete;
            }
        }

        protected override void OnInterrupted(HitContext ctx)
        {
            if (_entity is MonsterEntity monster)
            {
                var hitAction = ctx.resolvedHitAction ?? monster.Config?.hitReactionConfig?.GetHitAction(ctx.reactionType);
                if (hitAction != null && monster.ActionController != null)
                {
                    GLog.Info(LogTags.Combat, $"怪物播放受击动作: {monster.name} → {hitAction.name} (类型: {ctx.reactionType})");
                    var hitCommand = CharacterCommandFactory.CreateDirectAssetCommand(hitAction);
                    monster.ActionController.OnInput(hitCommand);
                }

                if (_hitData != null)
                {
                    _hitData.InHitReaction = true;
                    _hitData.HitSequenceId++;
                    _hitData.HitTriggerTimestamp = Time.frameCount;
                    _hitData.ResolvedHitAction = hitAction;
                    OnHitTimestampChanged?.Invoke();
                }

                if (monster.BTRunner?.RuntimeBlackboard != null && _hitData != null)
                {
                    monster.BTRunner.RuntimeBlackboard.Set("HitTriggerTimestamp", _hitData.HitTriggerTimestamp);
                }
            }
        }

        private void HandleActionComplete()
        {
            if (_hitData != null && _hitData.InHitReaction)
            {
                _hitData.InHitReaction = false;
                _hitData.ResolvedHitAction = null;
                _hitData.CurrentHitStunDuration = 0f;
            }
        }
    }
}
