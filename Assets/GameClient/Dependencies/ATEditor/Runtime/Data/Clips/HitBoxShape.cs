using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    public class HitBoxShape
    {
        [SkillProperty("形状类型")]
        public HitBoxType shapeType = HitBoxType.Sphere;

        [SkillProperty("尺寸")]
        public Vector3 size = Vector3.one;

        [SkillProperty("半径")]
        public float radius = 2f;

        [SkillProperty("高度")]
        public float height = 2f;

        [SkillProperty("角度")]
        [Range(0f, 360f)]
        public float angle = 90f;

        [SkillProperty("内半径")]
        public float innerRadius = 1f;

        public HitBoxShape Clone()
        {
            return new HitBoxShape
            {
                shapeType = this.shapeType,
                size = this.size,
                radius = this.radius,
                height = this.height,
                angle = this.angle,
                innerRadius = this.innerRadius
            };
        }
    }
}
