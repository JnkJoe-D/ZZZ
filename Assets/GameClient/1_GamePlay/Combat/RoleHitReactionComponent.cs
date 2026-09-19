using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    public class RoleHitReactionComponent : HitReactionComponent
    {
        protected override void OnInterrupted(HitContext ctx)
        {
            if (_entity is RoleEntity role)
            {
                var hitAction = ctx.resolvedHitAction ?? role.Config?.hitReactionConfig?.GetHitAction(ctx.reactionType);
                if (hitAction != null && role.ActionController != null)
                {
                    GLog.Info(LogTags.Combat, $"播放受击动作: {role.name} → {hitAction.name} (类型: {ctx.reactionType})");
                    var hitCommand = CharacterCommandFactory.CreateDirectAssetCommand(hitAction);
                    role.ActionController.OnInput(hitCommand);
                }
            }
        }
    }
}
