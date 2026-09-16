using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
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

            if (ctx.Type == SwitchType.ParryAid && ctx.WarningMarker != null && ctx.TargetAttacker != null)
            {
                // 招架支援专属身位裁决（方案 C）
                bool alreadyInCoverage = ctx.WarningMarker.AllowInPlaceParry && ctx.WarningMarker.IsPositionInCoverage(originPos);

                if (alreadyInCoverage)
                {
                    // 就地格挡：保持原身位，仅转向面向怪物
                    spawnPos = originPos;
                    Vector3 lookDir = ctx.TargetAttacker.transform.position - spawnPos;
                    lookDir.y = 0f;
                    spawnRot = lookDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(lookDir) : originRot;
                    GLog.Info(LogTags.Team, $"角色已在攻击威胁覆盖域内，执行【就地格挡】，不发生位移");
                }
                else
                {
                    // 远距切入：精准瞬移至怪物接刀身位（带探地贴合）
                    spawnPos = ctx.WarningMarker.GetWorldClashPosition();
                    Vector3 lookDir = ctx.TargetAttacker.transform.position - spawnPos;
                    lookDir.y = 0f;
                    spawnRot = lookDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(lookDir) : originRot;
                    GLog.Info(LogTags.Team, $"角色在威胁覆盖域外，瞬移至【接刀锚点】: {spawnPos}");
                }
            }
            else if (outEntity != null)
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

            // 4. 注入战斗上下文目标与警示标记（供动作时间轴中的 MovementClip / CameraControlClip 读取）
            if (ctx.TargetAttacker != null)
            {
                inEntity.SetCombatContextTarget(ctx.TargetAttacker);
            }
            if (ctx.WarningMarker != null && inEntity.DataModule != null)
            {
                var actionData = inEntity.DataModule.Get<ActionRuntimeData>();
                if (actionData != null)
                {
                    actionData.MatchedWarningMarker = ctx.WarningMarker;
                }
            }

            GLog.Info(LogTags.Team, $"Incoming 安全落点同步完成: {inMember.Config?.Name} @ {spawnPos}");
        }
    }
}
