using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 攻击检测策略配置块 (判定规则与受击参数策略，与几何范围解耦)
    /// </summary>
    [Serializable]
    public class AttackDetectionPolicy : ISerializationCallbackReceiver
    {
        [ActionProperty("检测频率")]
        public Frequency detectFrequency = Frequency.Once;

        [ActionProperty("检测次数")]
        [ATShowIf("detectFrequency", Frequency.Times)]
        public int times = 1;

        [ActionProperty("最大命中数")]
        public int maxHitTargets = 0;

        [ActionProperty("选择策略")]
        public TargetSortMode targetSortMode = TargetSortMode.Closest;

        [ActionProperty("受击方向模式")]
        public HitDirectionMode hitDirectionMode = HitDirectionMode.AttackerToTarget;

        [ActionProperty("相对受击方向")]
        [ATShowIf("hitDirectionMode", HitDirectionMode.OnEnterCustomRelative)]
        public Vector2 customHitDirection = new Vector2(0, 1);

        [ActionProperty("碰撞检测层级")]
        public LayerMask hitLayerMask = -1;

        [SerializeField, HideInInspector]
        public int serializedHitLayerMask = -1;

        [ActionProperty("是否影响自身")]
        public bool isSelfImpacted = false;

        [ActionProperty("检测配置")]
        public DetectConfig[] detects = new DetectConfig[] { new DetectConfig() };

        public AttackDetectionPolicy Clone()
        {
            return new AttackDetectionPolicy
            {
                detectFrequency = this.detectFrequency,
                times = this.times,
                maxHitTargets = this.maxHitTargets,
                targetSortMode = this.targetSortMode,
                hitDirectionMode = this.hitDirectionMode,
                customHitDirection = this.customHitDirection,
                hitLayerMask = this.hitLayerMask,
                serializedHitLayerMask = this.serializedHitLayerMask,
                isSelfImpacted = this.isSelfImpacted,
                detects = CloneDetects(this.detects)
            };
        }

        public static DetectConfig[] CloneDetects(DetectConfig[] source)
        {
            if (source == null || source.Length == 0) return new DetectConfig[] { new DetectConfig() };
            var result = new DetectConfig[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i]?.Clone() ?? new DetectConfig();
            }
            return result;
        }

        public void OnBeforeSerialize()
        {
            serializedHitLayerMask = hitLayerMask.value;
        }

        public void OnAfterDeserialize()
        {
            hitLayerMask.value = serializedHitLayerMask;
        }
    }
}
