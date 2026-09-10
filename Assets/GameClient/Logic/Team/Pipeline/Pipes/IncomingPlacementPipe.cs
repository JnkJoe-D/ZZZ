using UnityEngine;

namespace Game.Logic.Team.Pipeline.Pipes
{
    /// <summary>
    /// 切入角色防卡碰撞寻位、退场撤销复活与空间坐标同步过滤器
    /// </summary>
    public class IncomingPlacementPipe : ISwitchPipe
    {
        public string PipeName => "IncomingPlacementPipe";
        public int Priority => 400;

        public void Process(SwitchPipelineContext ctx)
        {
            if (ctx.IsAborted || ctx.IncomingEntity == null || ctx.Manager == null) return;

            RoleEntity inEntity = ctx.IncomingEntity;
            PartyMember inMember = ctx.IncomingMember;
            RoleEntity outEntity = ctx.OutgoingEntity;

            // 1. 如果切入角色还在切出队列中，取消其切出任务（复活保护）
            ctx.Manager.SwitchExecutor?.TryCancelSwitchOut(inMember);

            // 2. 计算安全的切入位置和朝向（胶囊体探测避障防卡）
            Vector3 originPos = outEntity != null ? outEntity.transform.position : inEntity.transform.position;
            Quaternion originRot = outEntity != null ? outEntity.transform.rotation : inEntity.transform.rotation;

            Vector3 spawnPos = originPos;
            Quaternion spawnRot = originRot;

            if (outEntity != null)
            {
                ctx.Manager.CalculateSafeSwitchInTransform(outEntity.transform, inEntity, out spawnPos, out spawnRot);
            }

            ctx.SpawnPosition = spawnPos;
            ctx.SpawnRotation = spawnRot;

            // 3. 激活 GameObject 与基础组件状态
            if (!inEntity.gameObject.activeSelf)
            {
                inEntity.gameObject.SetActive(true);
            }

            inEntity.EnsureRuntimeInitialized();
            inEntity.SetColliderActive(true);
            ctx.Manager.SynchronizePartyMemberTransform(inEntity, spawnPos, spawnRot);
            inEntity.ResetSwitchState();
            inEntity.SetPresentationVisible(true);

            Debug.Log($"<color=green>[SwitchPipeline] Incoming 安全落点同步完成: {inMember.Config?.Name} @ {spawnPos}</color>");
        }
    }
}
