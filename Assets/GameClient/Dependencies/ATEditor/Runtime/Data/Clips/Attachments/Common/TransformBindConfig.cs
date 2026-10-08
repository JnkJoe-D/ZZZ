using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 空间挂载与变换跟随参数块 (通用)
    /// </summary>
    [Serializable]
    public class TransformBindConfig
    {
        [ActionProperty("挂载位置")]
        public BindPoint bindPoint = BindPoint.LogicRoot;

        [ActionProperty("自定义骨骼名")]
        public string customBoneName = "";

        [ActionProperty("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;

        [ActionProperty("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;

        [ActionProperty("缩放比例")]
        public Vector3 scale = Vector3.one;

        [ActionProperty("是否跟随挂点")]
        public bool followTarget = true;

        [ActionProperty("跟随模式")]
        [ATShowIf("followTarget", true)]
        public HitBoxFollowMode followMode = HitBoxFollowMode.Both;

        public TransformBindConfig Clone()
        {
            return new TransformBindConfig
            {
                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                positionOffset = this.positionOffset,
                rotationOffset = this.rotationOffset,
                scale = this.scale,
                followTarget = this.followTarget,
                followMode = this.followMode
            };
        }
    }
}
