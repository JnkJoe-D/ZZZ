using UnityEngine;

namespace Game.Logic.Team.Pipeline.Pipes
{
    /// <summary>
    /// 切出角色输入剥离与自动退场生命周期托管过滤器
    /// （核心解决在动作时间轴反复手拼退场事件的痛点）
    /// </summary>
    public class OutgoingHandlingPipe : ISwitchPipe
    {
        public string PipeName => "OutgoingHandlingPipe";
        public int Priority => 300;

        public void Process(SwitchPipelineContext ctx)
        {
            if (ctx.IsAborted || ctx.OutgoingEntity == null) return;

            RoleEntity outEntity = ctx.OutgoingEntity;
            PartyMember outMember = ctx.OutgoingMember;

            // 1. 切断切出角色的玩家控制输入
            outEntity.SetControlActive(false, assignCameraTarget: false);

            // 2. 根据退场策略调度生命周期
            if (ctx.ExitPolicy == OutgoingExitPolicy.Immediate)
            {
                // 即时退场模式（招架支援/闪避支援）：立即关闭碰撞、隐藏模型、转入待机
                outEntity.SetColliderActive(false);
                outEntity.SetPresentationVisible(false);

                if (outEntity.DataModule != null)
                {
                    var switchData = outEntity.DataModule.Get<SwitchRuntimeData>();
                    if (switchData != null) switchData.IsSwitchOutPending = false;
                }

                if (outEntity.Config?.ActionRoot != null)
                {
                    outEntity.ActionController?.PlayAction(outEntity.Config.ActionRoot);
                }

                Debug.Log($"<color=gray>[SwitchPipeline] Outgoing 即时隐藏退场: {outMember?.Config?.Name}</color>");
            }
            else
            {
                // 动作托管模式 / 存量时间轴事件模式：加入切出队列托管
                ctx.Manager.SwitchExecutor?.EnqueueSwitchOut(outMember, ctx.ExitPolicy);

                // 尝试驱动切出动作路由（若配置了 SwitchOut 路由动作则切入，否则继续打完当前后摇）
                outEntity.ActionController?.TryTriggerEvent(RouteEventType.SwitchOut);

                Debug.Log($"<color=gray>[SwitchPipeline] Outgoing 动作托管退场入队: {outMember?.Config?.Name}</color>");
            }
        }
    }
}
