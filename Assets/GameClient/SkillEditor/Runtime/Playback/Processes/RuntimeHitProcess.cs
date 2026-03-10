using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    [ProcessBinding(typeof(HitClip), PlayMode.Runtime)]
    public class RuntimeHitProcess : ProcessBase<HitClip>
    {
        private ISkillHitHandler damageHandler;
        private Dictionary<Collider, float> hitRecords = new Dictionary<Collider, float>();
        private float lastCheckTime = -1f;
        private int currentHitCount = 0;

        public override void OnEnable()
        {
            damageHandler = context.GetService<ISkillHitHandler>();
        }

        public override void OnEnter()
        {
            hitRecords.Clear();
            lastCheckTime = -1f;
            currentHitCount = 0;

            if (clip.hitFrequency == HitFrequency.Once)
            {
                DoHitCheck();
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (clip.hitFrequency == HitFrequency.Always)
            {
                DoHitCheck();
            }
            else if (clip.hitFrequency == HitFrequency.Interval)
            {
                if (lastCheckTime < 0 || currentTime - lastCheckTime >= clip.checkInterval)
                {
                    DoHitCheck();
                    lastCheckTime = currentTime;
                }
            }
        }

        private void DoHitCheck()
        {
            if (damageHandler == null) return;
            if (clip.maxHitTargets > 0 && currentHitCount >= clip.maxHitTargets) return;

            Vector3 center;
            Quaternion rotation;
            GetMatrix(out center, out rotation);

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
                    if (clip.hitFrequency == HitFrequency.Once) continue;
                    // 如果是 Interval，依靠自身的计时器，这里允许命中，但是如果一个对象还在上一轮的冷却中？
                    // 由于 DoDamageCheck 本身是按频率调用的，只要被调到了就可以生效。此处可做更精细的每目标 CD，这里从简。
                }

                // 圆柱体相关的过滤逻辑 (高度剔除、平面剔除)
                if (shape.shapeType == HitBoxType.Sector || shape.shapeType == HitBoxType.Ring)
                {
                    // 转到检测框自身的局部坐标系进行计算
                    Vector3 localPos = Quaternion.Inverse(rotation) * (hit.transform.position - center);

                    // 1. 高度过滤 (带一点误差容忍)
                    if (Mathf.Abs(localPos.y) > (shape.height / 2f) + 0.01f)
                        continue;

                    // 2. 局部 2D 平面 (XZ平面) 的距离判断
                    Vector2 localPos2D = new Vector2(localPos.x, localPos.z);
                    float dist2D = localPos2D.magnitude;

                    // broad-phase 的 Box 会包含圆柱外的四个角，需二次过滤半径
                    if (dist2D > shape.radius)
                        continue;

                    if (shape.shapeType == HitBoxType.Sector)
                    {
                        // 局部 Z 轴是正前方，计算点在平面上与 Z 轴的夹角
                        // Vector2.up 是 (0, 1)，即对应局部的 Z 轴
                        float angle2D = Vector2.Angle(Vector2.up, localPos2D); 
                        if (angle2D > shape.angle / 2f)
                            continue;
                    }
                    else if (shape.shapeType == HitBoxType.Ring)
                    {
                        if (dist2D < shape.innerRadius)
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
                HitData hitData = new HitData()
                {
                    deployer = context.Owner,
                    targets = validHits.ToArray(),
                    eventTag = clip.eventTag,
                    actionTags = clip.targetTags

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
                bindTrans = actor.GetBone(clip.bindPoint);
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

        public override void Reset()
        {
            base.Reset();
            damageHandler = null;
            hitRecords.Clear();
        }
    }
}
