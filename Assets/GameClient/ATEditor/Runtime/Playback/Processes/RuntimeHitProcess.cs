using System.Collections.Generic;
using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(HitClip), PlayMode.Runtime)]
    public class RuntimeHitProcess : ProcessBase<HitClip>
    {
        private ISkillHitHandler damageHandler;
        private Dictionary<Collider, float> hitRecords = new Dictionary<Collider, float>();
        private float lastCheckTime = -1f;
        private int currentHitCount = 0;
        private int timesChecked = 0;
        private Vector3 fixedHitBoxPosition;
        private Quaternion fixedHitBoxRotation;

        public override void OnEnable()
        {
            damageHandler = context.GetService<ISkillHitHandler>();
        }

        public override void OnEnter()
        {
            hitRecords.Clear();
            lastCheckTime = -1f;
            currentHitCount = 0;
            timesChecked = 0;

            if (!clip.isHitBoxFollowBindPoint)
            {
                GetMatrix(out fixedHitBoxPosition, out fixedHitBoxRotation);
            }

            if (clip.detectFrequency == Frequency.Once)
            {
                DoHitCheck();
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (clip.detectFrequency == Frequency.Times)
            {
                if (clip.times <= 0 || timesChecked >= clip.times) return;
                
                float dynamicInterval = clip.times > 1 ? clip.Duration / clip.times : clip.Duration; 

                if (lastCheckTime < 0 || currentTime - lastCheckTime >= dynamicInterval)
                {
                    DoHitCheck();
                    lastCheckTime = currentTime;
                    timesChecked++;
                }
            }
        }

        private void DoHitCheck()
        {
            if (damageHandler == null) return;
            if (clip.maxHitTargets > 0 && currentHitCount >= clip.maxHitTargets) return;

            Vector3 center;
            Quaternion rotation;
            if (clip.isHitBoxFollowBindPoint)
            {
                GetMatrix(out center, out rotation);
            }
            else
            {
                center = fixedHitBoxPosition;
                rotation = fixedHitBoxRotation;
            }

            var shape = clip.shape;
            
            // 使用用户配置的 LayerMask
            int layerMask = clip.hitLayerMask.value;

            Collider[] hits = null;

            switch (shape.shapeType)
            {
                case HitBoxType.Sphere:
                    hits = Physics.OverlapSphere(center, shape.radius, layerMask);
                    break;
                case HitBoxType.Sector:
                case HitBoxType.Ring:
                    hits = Physics.OverlapBox(center, new Vector3(shape.radius, shape.height / 2f, shape.radius), rotation, layerMask);
                    break;
                case HitBoxType.Box:
                    hits = Physics.OverlapBox(center, shape.size / 2f, rotation, layerMask);
                    break;
                case HitBoxType.Capsule:
                    Vector3 up = rotation * Vector3.up;
                    float h = Mathf.Max(0, shape.height - shape.radius * 2);
                    Vector3 p1 = center - up * (h / 2);
                    Vector3 p2 = center + up * (h / 2);
                    hits = Physics.OverlapCapsule(p1, p2, shape.radius, layerMask);
                    break;
            }

            if (hits == null || hits.Length == 0) return;

            List<Collider> validHits = new List<Collider>();
            foreach (var hit in hits)
            {
                // 自己免疫伤害
                if (context.Owner != null && hit.gameObject == context.Owner &&!clip.isSelfImpacted) continue;

                // 冷却过滤
                if (hitRecords.TryGetValue(hit, out float lastHitTime))
                {
                    if (clip.detectFrequency == Frequency.Once) continue;
                }

                // 圆柱体相关的过滤逻辑 (高度剔除、平面剔除)
                if (shape.shapeType == HitBoxType.Sector || shape.shapeType == HitBoxType.Ring)
                {
                    // 使用碰撞体的包围盒作为基准
                    Bounds bounds = hit.bounds;
                    
                    // 转到检测框自身的局部坐标系来判断（以 center 为原点，rotation 为方向）
                    Vector3 localCenter = Quaternion.Inverse(rotation) * (bounds.center - center);
                    
                    // 估算碰撞体在 XZ 平面上的最大投影半径（粗略但安全的包围圆）
                    float targetRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                    
                    // 1. 高度过滤 (Y轴方向)
                    float boundMinY = localCenter.y - bounds.extents.y;
                    float boundMaxY = localCenter.y + bounds.extents.y;
                    float shapeHalfHeight = shape.height / 2f;
                    
                    if (boundMinY > shapeHalfHeight || boundMaxY < -shapeHalfHeight)
                        continue;

                    // 2. 局部 2D 平面 (XZ平面) 的距离判断
                    Vector2 localPos2D = new Vector2(localCenter.x, localCenter.z);
                    float dist2D = localPos2D.magnitude;

                    // 外圈剔除
                    if (dist2D - targetRadius > shape.radius)
                        continue;

                    if (shape.shapeType == HitBoxType.Sector)
                    {
                        if (dist2D > targetRadius)
                        {
                            float angle2D = Vector2.Angle(Vector2.up, localPos2D);
                            float angleTolerance = Mathf.Asin(Mathf.Clamp01(targetRadius / dist2D)) * Mathf.Rad2Deg;
                            
                            if (angle2D - angleTolerance > shape.angle / 2f)
                                continue;
                        }
                    }
                    else if (shape.shapeType == HitBoxType.Ring)
                    {
                        if (dist2D + targetRadius < shape.innerRadius)
                            continue;
                    }
                }

                validHits.Add(hit);
            }

            // 容量截断 / 排序
            if (clip.maxHitTargets > 0)
            {
                if (clip.targetSortMode == TargetSortMode.Closest)
                {
                    validHits.Sort((a, b) => 
                        Vector3.Distance(a.transform.position, center).CompareTo(Vector3.Distance(b.transform.position, center)));
                }
                else if (clip.targetSortMode == TargetSortMode.Random)
                {
                    for (int i = 0; i < validHits.Count; i++)
                    {
                        var temp = validHits[i];
                        int randomIndex = Random.Range(i, validHits.Count);
                        validHits[i] = validHits[randomIndex];
                        validHits[randomIndex] = temp;
                    }
                }

                int takeCount = Mathf.Min(clip.maxHitTargets - currentHitCount, validHits.Count);
                if (takeCount < validHits.Count)
                {
                    validHits = validHits.GetRange(0, takeCount);
                }
            }

            if (validHits.Count > 0)
            {
                foreach (var h in validHits)
                {
                    hitRecords[h] = Time.time;
                }
                currentHitCount += validHits.Count;

                // ★ 根据当前检测轮次取对应的 DetectConfig
                // 若 detects 数组长度不足，Clamp 到最后一条复用
                int configIndex = (clip.detects != null && clip.detects.Length > 0)
                    ? Mathf.Clamp(timesChecked, 0, clip.detects.Length - 1)
                    : 0;
                var detectConfig = (clip.detects != null && clip.detects.Length > 0)
                    ? clip.detects[configIndex]
                    : new DetectConfig();

                HitData hitData = new HitData()
                {
                    deployer = context.Owner,
                    hitBoxCenter = center,
                    targetsCollilders = validHits.ToArray(),
                    hitEffectId = detectConfig.hitEffectId,
                    hitMode = detectConfig.hitMode,
                    multiHitCount = detectConfig.multiHitCount,
                    multiHitDuration = detectConfig.multiHitDuration,
                    enableHitStop = detectConfig.enableHitStop,
                    hitStopDuration = detectConfig.hitStopDuration,
                    hitVFXPrefab = detectConfig.hitVFXPrefab,
                    hitVFXHeight = detectConfig.hitVFXHeight,
                    hitVFXScale = detectConfig.hitVFXScale,
                    hitAudioClip = detectConfig.hitAudioClip,
                    hitStunDuration = detectConfig.hitStunDuration,
                    followTarget = detectConfig.followTarget
                };
                damageHandler.OnHitDetect(hitData);
            }
        }

        private void GetMatrix(out Vector3 pos, out Quaternion rot)
        {
            Transform bindTrans = null;
            if (context != null)
            {
                var actor = context.GetService<ISkillBoneGetter>();
                if (actor != null)
                {
                    bindTrans = actor.GetBone(clip.bindPoint, clip.customBoneName);
                }
            }

            if (bindTrans != null)
            {
                pos = bindTrans.position + bindTrans.rotation * clip.positionOffset;
                rot = bindTrans.rotation * Quaternion.Euler(clip.rotationOffset);
            }
            else
            {
                pos = clip.positionOffset;
                rot = Quaternion.Euler(clip.rotationOffset);
            }
        }

        public override void OnExit()
        {
            hitRecords.Clear();
        }
        public override void OnDisable()
        {
            hitRecords.Clear();
        }

        public override void Reset()
        {
            base.Reset();
            damageHandler = null;
            hitRecords.Clear();
        }
    }
}
