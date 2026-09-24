using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    public class HitBoxShape
    {
        [ActionProperty("形状类型")]
        public HitBoxType shapeType = HitBoxType.Sphere;

        [ActionProperty("尺寸")]
        public Vector3 size = Vector3.one;

        [ActionProperty("半径")]
        public float radius = 2f;

        [ActionProperty("高度")]
        public float height = 2f;

        [ActionProperty("角度")]
        [Range(0f, 360f)]
        public float angle = 90f;

        [ActionProperty("内半径")]
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
