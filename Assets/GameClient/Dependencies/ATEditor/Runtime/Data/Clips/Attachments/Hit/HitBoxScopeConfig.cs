using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 空间检测盒范围配置块 (范围划定，解耦于具体判定逻辑)
    /// </summary>
    [Serializable]
    public class HitBoxScopeConfig
    {
        [ActionProperty("检测盒形状")]
        public HitBoxShape shape = new HitBoxShape();

        [ActionProperty("检测盒绑定点")]
        public BindPoint bindPoint = BindPoint.LogicRoot;

        [ActionProperty("自定义骨骼名称")]
        public string customBoneName = "";

        [ActionProperty("检测盒跟随模式")]
        public HitBoxFollowMode hitBoxFollowMode = HitBoxFollowMode.PositionOnly;

        [ActionProperty("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;

        [ActionProperty("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;

        [ActionProperty("显示检测盒Gizmos")]
        public bool showHitBoxGizmos = true;

        public HitBoxScopeConfig Clone()
        {
            return new HitBoxScopeConfig
            {
                shape = this.shape?.Clone() ?? new HitBoxShape(),
                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                hitBoxFollowMode = this.hitBoxFollowMode,
                positionOffset = this.positionOffset,
                rotationOffset = this.rotationOffset,
                showHitBoxGizmos = this.showHitBoxGizmos
            };
        }

        /// <summary>
        /// 计算指定宿主与骨骼上下文下的检测盒世界矩阵（位置与旋转）
        /// </summary>
        public void CalculateMatrix(Transform rootTrans, IBoneGetter boneGetter, Vector3 fixedPos, Quaternion fixedRot, out Vector3 outPos, out Quaternion outRot)
        {
            Vector3 currentPos;
            Quaternion currentRot;

            Quaternion rootRot = rootTrans != null ? rootTrans.rotation : Quaternion.identity;
            Vector3 rootPos = rootTrans != null ? rootTrans.position : Vector3.zero;

            Transform bindTrans = null;
            if (boneGetter != null)
            {
                bindTrans = boneGetter.GetBone(bindPoint, customBoneName);
            }
            if (bindTrans == null) bindTrans = rootTrans;

            switch (hitBoxFollowMode)
            {
                case HitBoxFollowMode.PositionOnly:
                    if (bindTrans != null)
                        currentPos = bindTrans.position + rootRot * positionOffset;
                    else
                        currentPos = rootPos + rootRot * positionOffset;
                    currentRot = rootRot * Quaternion.Euler(rotationOffset);
                    break;

                case HitBoxFollowMode.RotationOnly:
                    currentPos = rootPos + rootRot * positionOffset;
                    if (bindTrans != null)
                        currentRot = bindTrans.rotation * Quaternion.Euler(rotationOffset);
                    else
                        currentRot = rootRot * Quaternion.Euler(rotationOffset);
                    break;

                case HitBoxFollowMode.None:
                    if (bindTrans != null && bindPoint != BindPoint.LogicRoot)
                        currentPos = bindTrans.position + rootRot * positionOffset;
                    else
                        currentPos = rootPos + rootRot * positionOffset;
                    currentRot = rootRot * Quaternion.Euler(rotationOffset);
                    break;

                case HitBoxFollowMode.Both:
                default:
                    if (bindTrans != null)
                    {
                        currentPos = bindTrans.position + bindTrans.rotation * positionOffset;
                        currentRot = bindTrans.rotation * Quaternion.Euler(rotationOffset);
                    }
                    else
                    {
                        currentPos = rootPos + rootRot * positionOffset;
                        currentRot = rootRot * Quaternion.Euler(rotationOffset);
                    }
                    break;
            }

            switch (hitBoxFollowMode)
            {
                case HitBoxFollowMode.None:
                    outPos = fixedPos;
                    outRot = fixedRot;
                    break;
                case HitBoxFollowMode.PositionOnly:
                    outPos = currentPos;
                    outRot = fixedRot;
                    break;
                case HitBoxFollowMode.RotationOnly:
                    outPos = fixedPos;
                    outRot = currentRot;
                    break;
                case HitBoxFollowMode.Both:
                default:
                    outPos = currentPos;
                    outRot = currentRot;
                    break;
            }
        }
    }
}
