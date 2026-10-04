using UnityEngine;
using cfg.ZZZ;
using ATEditor;

namespace Game.GamePlay
{
    /// <summary>
    /// 招架拼刀预裁决纯数据上下文
    /// 伴随 ParryClashContract 生命周期闭环，纯 POCO 对象，零 GC。
    /// </summary>
    public class ParryPrecomputedData
    {
        public bool IsValid;
        public bool WillInterrupt;              // 数值比对结果：怪物是否会被打断
        public ActionConfigAsset TargetHitAction;// 预先解析出的受击动作资产
        public HitReactionType ReactionType;    // 严格对齐 cfg.ZZZ.HitReactionType.Parried
        public float HitStopDuration;           // 拼刀顿帧时长（默认 0.15s）
        public Vector3 ClashPosition;           // 空间拼刀吸附锚点

        public void Reset()
        {
            IsValid = false;
            WillInterrupt = false;
            TargetHitAction = null;
            ReactionType = HitReactionType.Parried;
            HitStopDuration = 0.15f;
            ClashPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// 招架契约预裁决器：专职在契约建立（RegisterContract）后至相撞前，
    /// 仅对该契约绑定的单体怪物攻击进行纯数值打断力比对与受击动作预解析。
    /// </summary>
    public static class ParryPreArbitrator
    {
        public static void Precompute(ParryClashContract contract)
        {
            if (contract == null || contract.Attacker == null || contract.ParryRole == null) return;

            var data = contract.PrecomputedData ??= new ParryPrecomputedData();
            data.Reset();

            CharacterEntity monster = contract.Attacker;
            CharacterEntity player = contract.ParryRole;

            // 1. 获取防守方招架动作的打断力 (ParryInterruptLevel)
            int parryInterruptLevel = ActionResilienceHelper.GetInterruptLevel(player);
            if (parryInterruptLevel <= 0 && contract.Marker != null)
            {
                // 若切入角色尚未完全切入动作，根据预警强度提供保底判定
                parryInterruptLevel = contract.Marker.ParryWeight == ParryWeight.Heavy ? 2 : 1;
            }

            // 2. 获取怪物实时的总抗打断韧性 (MonsterTotalResilience)
            // 纯数值驱动：基础韧性 + Buff韧性加成(霸体Buff直接赋予极高韧性加成) + 怪物当前动作配表加成
            int monsterTotalResilience = ActionResilienceHelper.GetTotalResilience(monster);

            // 3. 纯数值统一打断裁决：打断力 > 0 且大于等于怪物总韧性
            bool willInterrupt = parryInterruptLevel > 0 && parryInterruptLevel >= monsterTotalResilience;
            data.WillInterrupt = willInterrupt;

            var role = player as RoleEntity;
            var parryWeight = contract.Marker != null ? contract.Marker.ParryWeight : ParryWeight.Heavy;
            var parryEntry = role?.Config?.AssistConfig?.GetParryEntry(parryWeight);
            data.HitStopDuration = parryEntry != null && parryEntry.HitStopDuration > 0f ? parryEntry.HitStopDuration : 0.2f;

            data.ReactionType = HitReactionType.Parried; // 严格对齐 Parried = 70 (被格挡)

            // 4. 若打断成立，预先根据相对受击方位解析受击动作
            if (willInterrupt && monster.Config?.hitReactionConfig != null)
            {
                Vector3 faceDir = (player.transform.position - monster.transform.position);
                faceDir.y = 0f;
                float signedAngle = faceDir.sqrMagnitude > 0.0001f 
                    ? Vector3.SignedAngle(monster.transform.forward, faceDir.normalized, Vector3.up) 
                    : 0f;

                monster.Config.hitReactionConfig.TryResolveHitAction(
                    data.ReactionType,
                    signedAngle,
                    0f,
                    out var resolvedAction,
                    out _);

                data.TargetHitAction = resolvedAction;
            }

            // 5. 空间拼刀锚点
            if (contract.Marker != null)
            {
                data.ClashPosition = monster.transform.position + monster.transform.rotation * contract.Marker.ClashPositionOffset;
            }

            data.IsValid = true;
        }
    }
}
