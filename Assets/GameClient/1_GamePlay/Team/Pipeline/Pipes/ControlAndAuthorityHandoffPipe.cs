using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 主控权限、输入提供器与相机交接过滤器
    /// </summary>
    public class ControlAndAuthorityHandoffPipe : ISwitchPipe
    {
        public string PipeName => "ControlAndAuthorityHandoffPipe";
        public int Priority => 500;

        public void Process(SwitchPipelineContext ctx)
        {
            if (ctx.IsAborted || ctx.IncomingEntity == null || ctx.Manager == null) return;

            RoleEntity inEntity = ctx.IncomingEntity;
            PartyMember inMember = ctx.IncomingMember;

            // 1. 绑定共享相机与队伍上下文
            ctx.Manager.AssignSharedPartyCamera(inEntity);
            ctx.Manager.AssignTeamContext(inEntity);

            // 2. 启用切入角色的主控相机 Rig 与上下文
            inEntity.Presentation?.SetCameraActive(true);
            ctx.Manager.TeamContext?.SetActiveRole(inEntity);

            // 3. 挂载玩家输入提供器、切换主镜头目标并接管控制权
            GameCameraManager.Instance?.SetTarget(inEntity.transform);
            inEntity.SetControlActive(true);

            // 4. 更新调试 HUD
            ctx.Manager.UpdatePartyDebugHudVisibility(inEntity);

            // 5. 递增时效版本号，防止异步回包/过时切入干扰
            inMember.ActivationVersion++;

            // 6. 更新当前主控成员并发布身份变更事件
            ctx.Manager.LocalCharacter = inEntity;
            int oldSlotIndex = ctx.Manager.ActiveSlotIndex;
            ctx.Manager.SetActiveSlotIndex(inMember.SlotIndex);

            if (oldSlotIndex != ctx.Manager.ActiveSlotIndex)
            {
                EventCenter.Publish(new ActiveRoleChangedEvent
                {
                    OldSlotIndex = oldSlotIndex,
                    NewSlotIndex = ctx.Manager.ActiveSlotIndex,
                    NewEntity = inEntity
                });
            }

            GLog.Info(LogTags.Team, $"主控权限交接完成: {inMember.Config?.Name} (Slot {inMember.SlotIndex})");
        }
    }
}
