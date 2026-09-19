using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 换人空间坐标解算与防卡死避障服务。
    /// 负责计算切入角色的安全坐标、几何候选偏移及物理阻挡检测。
    /// </summary>
    public class TeamPlacementService
    {
        private static readonly Collider[] _blockCheckBuffer = new Collider[1];

        public void ResolveSwitchInPlacement(
            Transform originTransform, 
            RoleEntity switchInEntity, 
            TeamConfigAsset teamConfig, 
            out Vector3 targetPos, 
            out Quaternion targetRot)
        {
            if (originTransform == null)
            {
                targetPos = Vector3.zero;
                targetRot = Quaternion.identity;
                return;
            }

            GetInvalidPosSwitchIn(originTransform, switchInEntity, teamConfig, out targetPos, out targetRot);
        }

        public void ResolvePlacementAtPosition(
            Vector3 position, 
            Quaternion rotation, 
            RoleEntity switchInEntity, 
            TeamConfigAsset teamConfig, 
            out Vector3 targetPos, 
            out Quaternion targetRot)
        {
            GetInvalidPosItself(position, rotation, switchInEntity, teamConfig, out targetPos, out targetRot);
        }

        private void GetInvalidPosSwitchIn(
            Transform originTransform, 
            RoleEntity switchInEntity, 
            TeamConfigAsset teamConfig, 
            out Vector3 targetPos, 
            out Quaternion targetRot)
        {
            Vector3 originPos = originTransform.position;
            Quaternion originRot = originTransform.rotation;

            var offsets = teamConfig != null ? teamConfig.SwitchInOffset : null;
            if (offsets != null)
            {
                for (int i = 0; i < offsets.Count; ++i)
                {
                    Vector3 testPos = originTransform.TransformPoint(offsets[i]);
                    if (!IsPositionBlocked(testPos, switchInEntity, teamConfig))
                    {
                        targetPos = testPos;
                        targetRot = originRot;
                        return;
                    }
                }
            }

            if (!IsPositionBlocked(originPos, switchInEntity, teamConfig))
            {
                targetPos = originPos;
                targetRot = originRot;
                return;
            }

            targetPos = originPos;
            targetRot = originRot;
        }

        private void GetInvalidPosItself(
            Vector3 position, 
            Quaternion rotation, 
            RoleEntity switchInEntity, 
            TeamConfigAsset teamConfig, 
            out Vector3 targetPos, 
            out Quaternion targetRot)
        {
            targetPos = position;
            targetRot = rotation;

            if (IsPositionBlocked(position, switchInEntity, teamConfig))
            {
                var offsets = teamConfig != null ? teamConfig.SwitchInOffset : null;
                if (offsets != null)
                {
                    for (int i = 0; i < offsets.Count; ++i)
                    {
                        Vector3 testPos = position + rotation * offsets[i];
                        if (!IsPositionBlocked(testPos, switchInEntity, teamConfig))
                        {
                            targetPos = testPos;
                            break;
                        }
                    }
                }
            }
        }

        public bool IsPositionBlocked(Vector3 pos, RoleEntity entity, TeamConfigAsset teamConfig)
        {
            float radius = entity.MovementComponent != null ? entity.MovementComponent.CharacterRadius : 0.5f;
            float checkRadius = radius * (teamConfig != null ? teamConfig.blockRadiusMultipier : 1.0f);

            float height = 2.0f;
            var cc = entity.GetComponent<CharacterController>();
            if (cc != null)
            {
                height = cc.height;
            }
            else
            {
                var capsule = entity.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    height = capsule.height;
                }
            }

            Vector3 point1 = pos + Vector3.up * checkRadius;
            Vector3 point2 = pos + Vector3.up * Mathf.Max(checkRadius, height - checkRadius);

            int obstacleLayerMask = LayerMask.GetMask("Default", "Ground", "Obstacle", "Wall");
            if (obstacleLayerMask == 0)
            {
                obstacleLayerMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
            }

            int hitCount = Physics.OverlapCapsuleNonAlloc(
                point1,
                point2,
                checkRadius,
                _blockCheckBuffer,
                obstacleLayerMask,
                QueryTriggerInteraction.Ignore);

            return hitCount > 0;
        }
    }
}
