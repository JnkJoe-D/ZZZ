using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    public class RoleHitReactionComponent : HitReactionComponent
    {
        protected override void OnInterrupted(HitPipelineContext ctx)
        {
            if (_entity is RoleEntity role)
            {
                var hitAction = ctx.ResolvedHitAction;
                if (hitAction != null && role.ActionController != null)
                {
                    GLog.Info(LogTags.Combat, $"播放受击动作: {role.name} → {hitAction.name} (类型: {ctx.SelectedReactionType})");
                    var hitCommand = CharacterCommandFactory.CreateDirectAssetCommand(hitAction);
                    role.ActionController.OnInput(hitCommand);
                }
            }
        }
    }
}
