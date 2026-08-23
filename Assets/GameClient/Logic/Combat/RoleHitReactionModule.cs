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
                var hitAction = role.Config?.hitReactionConfig?.GetHitAction(ctx.reactionType);
                if (hitAction != null && role.ActionController != null)
                {
                    var hitCommand = CharacterCommandFactory.CreateDirectAssetCommand(hitAction);
                    role.ActionController.OnInput(hitCommand);
                }
            }
        }
    }
}
