using UnityEngine;
using Game.Framework;

namespace Game.Logic
{
    public class MonsterHitReactionModule : HitReactionModule
    {
        public event System.Action OnHitTimestampChanged;

        protected override void OnInterrupted(HitContext ctx)
        {
            if (_entity is MonsterEntity monster)
            {
                var hitAction = ctx.resolvedHitAction ?? monster.Config?.hitReactionConfig?.GetHitAction(ctx.reactionType);
                if (hitAction != null && monster.ActionController != null)
                {
                    Debug.Log($"<color=orange>[HitReaction] 怪物播放受击动作: {monster.name} → {hitAction.name} (类型: {ctx.reactionType})</color>");
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

                var aiData = monster.DataModule?.Get<MonSterBehaviorRuntimeData>();
                aiData?.ChangeState(MonsterAIState.HitStun);

                if (monster.BTRunner?.RuntimeBlackboard != null && _hitData != null)
                {
                    monster.BTRunner.RuntimeBlackboard.Set("HitTriggerTimestamp", _hitData.HitTriggerTimestamp);
                }
            }
        }
    }
}
