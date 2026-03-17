using SkillEditor;
using UnityEngine;
using Game.Logic.Character;
using System.Linq;

namespace Game.Adapters
{
    public class SkillHitHandler : ISkillHitHandler
    {
        public void OnHitDetect(HitData hitData)
        {
            if (hitData.targetsCollilders == null || hitData.targetsCollilders.Length == 0) return;
            if(hitData.hitEffects == null || hitData.hitEffects.Length == 0) return;
            // 获取攻击者实体
            CharacterEntity attacker = null;
            if (hitData.deployer != null)
            {
                attacker = hitData.deployer.GetComponent<CharacterEntity>();
            }

            foreach (var collider in hitData.targetsCollilders)
            {
                if (collider == null) continue;

                var victim = collider.GetComponentInParent<CharacterEntity>();
                if (victim == null) continue;

                // 计算碰撞点和攻击方向
                Vector3 attackerPos = hitData.deployer != null ? hitData.deployer.transform.position : Vector3.zero;
                Vector3 hitPoint = collider.ClosestPoint(attackerPos);
                Vector3 hitDirection = (victim.transform.position - attackerPos).normalized;

                // 构建 HitContext
                var ctx = new HitContext
                {
                    attacker = attacker,
                    victim = victim,
                    hitEffects = hitData.hitEffects,
                    enableHitStop = hitData.enableHitStop,
                    hitStopDuration = hitData.hitStopDuration,
                    hitVFXPrefab = hitData.hitVFXPrefab,
                    hitVFXHeight = hitData.hitVFXHeight,
                    hitVFXScale = hitData.hitVFXScale,
                    hitVFXFollowTarget = hitData.followTarget,
                    hitAudioClip = hitData.hitAudioClip,
                    hitStunDuration = hitData.hitStunDuration,
                    hitPoint = hitPoint,
                    hitDirection = hitDirection
                };

                // 遍历 hitEffects，通过注册表分发给对应的 Impact
                foreach (var entry in hitData.hitEffects)
                {
                    if (entry == null) continue;

                    // 根据 entry.targetTags 做目标过滤
                    if(entry.targetTags.Contains(victim.tag))
                    {
                        var impact = HitImpactRegistry.Resolve(entry.eventTag);
                        if (impact != null)
                        {
                            impact.Execute(ctx, entry);
                        }
                        else
                        {
                            Debug.LogWarning($"[HitHandler] 未找到 eventTag '{entry.eventTag}' 对应的 Impact 实现");
                        }
                    }
                }
                Debug.Log($"<color=orange>[Hit] {hitData.deployer?.name} → {collider.gameObject.name}</color>");
            }
        }
    }
}
