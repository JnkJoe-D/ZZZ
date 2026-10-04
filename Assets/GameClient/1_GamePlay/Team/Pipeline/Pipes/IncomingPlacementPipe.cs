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

            // 1. 检查切入角色是否正处于待切出 (Pending) 阶段
            bool isPendingSwitchOut = ctx.IsIncomingPendingSwitchOut ||
                                      (ctx.Manager.SwitchExecutor?.IsPendingSwitchOut(inMember) ?? false);
            ctx.IsIncomingPendingSwitchOut = isPendingSwitchOut;

            // 2. 如果切入角色还在切出队列中，取消其切出任务（复活保护）
            ctx.Manager.SwitchExecutor?.TryCancelSwitchOut(inMember);

            // 3. 若切入角色正处于 Pending 待切出状态，且非紧急招架支援：
            //    角色仍在场上继续运行当前动作，直接保持其现存位置和朝向
            if (isPendingSwitchOut && ctx.Type != SwitchType.ParryAid)
            {
                ctx.SpawnPosition = inEntity.transform.position;
                ctx.SpawnRotation = inEntity.transform.rotation;

                if (!inEntity.gameObject.activeSelf)
                {
                    inEntity.gameObject.SetActive(true);
                }

                inEntity.EnsureRuntimeInitialized();
                inEntity.Presentation?.SetColliderActive(true);
                inEntity.Presentation?.SetPresentationVisible(true);

                if (ctx.TargetAttacker != null)
                {
                    inEntity.TargetFinder?.SetCombatContextTarget(ctx.TargetAttacker);
                }

                GLog.Info(LogTags.Team, $"切入角色 {inMember.Config?.Name} 处于 Pending 待切出状态切回，保持原位与朝向，不更新 Transform");
                return;
            }

            // 4. 计算安全的切入位置和朝向（胶囊体探测避障防卡）
            Vector3 originPos = outEntity != null ? outEntity.transform.position : inEntity.transform.position;
            Quaternion originRot = outEntity != null ? outEntity.transform.rotation : inEntity.transform.rotation;

            Vector3 spawnPos = originPos;
            Quaternion spawnRot = originRot;

            if (ctx.Type == SwitchType.ParryAid && ctx.WarningMarker != null && ctx.TargetAttacker != null)
            {
                // 1. 动态时间差计算：获取怪物攻击帧到来的剩余物理时间
                float expectedHitTime = ctx.WarningMarker.ExpectedHitTime > 0f ? ctx.WarningMarker.ExpectedHitTime : Time.time + ctx.WarningMarker.Duration;
                float remainTimeToHit = Mathf.Max(0f, expectedHitTime - Time.time);

                var assistCfg = inEntity.Config?.AssistConfig;
                float readyDuration = assistCfg != null && assistCfg.ParryReadyDuration > 0f ? assistCfg.ParryReadyDuration : 0.2f;
                float diff = remainTimeToHit - readyDuration;

                // 差值大于等于 0 说明时间充裕，从 0 帧开始播；差值小于 0 说明需快进以对齐刀尖
                float calculatedStartTime = diff >= 0f ? 0f : Mathf.Min(-diff, readyDuration);
                ctx.CalculatedStartTime = calculatedStartTime;

                // 2. 招架支援身位裁决与位移补偿（三维严谨裁决：禁区外 + 有效范围内 + 怪物与接刀点连线上）
                bool alreadyInCoverage = ctx.WarningMarker.CanPerformInPlaceParry(originPos);
                Vector3 basePos = alreadyInCoverage ? originPos : ctx.WarningMarker.GetWorldClashPosition(inEntity);

                Vector3 lookDir = ctx.TargetAttacker.transform.position - basePos;
                lookDir.y = 0f;
                spawnRot = lookDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(lookDir) : originRot;

                // 根运动前向滑步位移补偿算法：无论是就地格挡还是远距切入，统一扣除起手动作产生的根运动滑步位移
                float rootMotionZ = assistCfg != null ? assistCfg.ParryRootMotionZOffset : 0f;
                float displacementRatio = readyDuration > 0.001f ? (readyDuration - calculatedStartTime) / readyDuration : 0f;
                displacementRatio = Mathf.Clamp01(displacementRatio);
                float actualDisplacement = rootMotionZ * displacementRatio;

                // 在切入角色朝向的反方向（即后退方向）扣除该位移量，确保滑步结束后刚好到达 basePos
                spawnPos = basePos - (spawnRot * Vector3.forward) * actualDisplacement;

                GLog.Info(LogTags.Team, $"[ParryPlacement] {(alreadyInCoverage ? "就地格挡" : "远距切入")}: Base={basePos}, ActualSpawn={spawnPos}, RootMotionOffset={actualDisplacement:F2}m (Ratio={displacementRatio:P0}), CalculatedStartTime={calculatedStartTime:F3}s");
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
            inEntity.Presentation?.SetColliderActive(true);
            ctx.Manager.SynchronizePartyMemberTransform(inEntity, spawnPos, spawnRot);
            inEntity.RouteArbitrator?.Clear();
            inEntity.Presentation?.SetPresentationVisible(true);

            // 4. 注入战斗上下文目标与警示标记（供动作时间轴中的 MovementHandler / CameraControlHandler 读取）
            if (ctx.TargetAttacker != null)
            {
                inEntity.TargetFinder?.SetCombatContextTarget(ctx.TargetAttacker);
            }
            if (ctx.WarningMarker != null && inEntity.DataModule != null)
            {
                var actionData = inEntity.DataModule.Get<ActionRuntimeData>();
                if (actionData != null)
                {
                    actionData.Set(nameof(actionData.MatchedWarningMarker), ctx.WarningMarker);
                }
            }

            GLog.Info(LogTags.Team, $"Incoming 安全落点同步完成: {inMember.Config?.Name} @ {spawnPos}");
        }
    }
}
