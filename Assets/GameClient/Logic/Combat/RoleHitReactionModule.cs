using UnityEngine;
using Game.Input;

namespace Game.Logic
{
    public class RoleHitReactionModule : HitReactionModule
    {
        protected override void OnInterrupted(HitContext ctx)
        {
            if (_entity is RoleEntity role)
            {
                var hitAction = ctx.resolvedHitAction ?? role.Config?.hitReactionConfig?.GetHitAction(ctx.reactionType);
                if (hitAction != null && role.ActionController != null)
                {
                    Debug.Log($"<color=orange>[HitReaction] 播放受击动作: {role.name} → {hitAction.name} (类型: {ctx.reactionType})</color>");
                    var hitCommand = CharacterCommandFactory.CreateDirectAssetCommand(hitAction);
                    role.ActionController.OnInput(hitCommand);
                }
            }
        }
    }
}
