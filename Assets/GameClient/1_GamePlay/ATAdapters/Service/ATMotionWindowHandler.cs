using ATEditor;
using UnityEngine;

namespace Game.GamePlay
{
    public sealed class ATMotionWindowHandler : IMotionWindowHandler
    {
        private readonly CharacterEntity _entity;

        public ATMotionWindowHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode filterMode)
        {
            _entity.MovementComponent?.SetFilterMode(filterMode);
        }
        public void DisableLocalDeltaFilter()
        {
            _entity.MovementComponent?.SetFilterMode(MotionWindowLocalDeltaFilterMode.None);
        }

        public void EnableCollisionMode(RootMotionCollisionMode mode, LayerMask obstacleMask)
        {
            _entity.MovementComponent?.SetCollisionMode(mode);
            _entity.MovementComponent?.SetObstacleMask(obstacleMask);
        }

        public void DisableCollisionMode()
        {
            _entity.MovementComponent?.SetCollisionMode(RootMotionCollisionMode.DefaultSlide);
        }

        public void EnableVisualOffset(MotionWindowVisualOffsetMode offsetMode)
        {
            _entity.MovementComponent?.SetVisualOffsetMode(offsetMode);
        }

        public void DisableVisualOffset()
        {
            _entity.MovementComponent?.SetVisualOffsetMode(MotionWindowVisualOffsetMode.None);
        }

        public void EnableVisualOffsetRecover(float speed)
        {
            _entity.MovementComponent?.SetVisualRecover(true, speed);
        }

        public void DisableVisualOffsetRecover()
        {
            _entity.MovementComponent?.SetVisualRecover(false);
            _entity.MovementComponent?.ResetVisualOffset();
        }

        public void EnableRotationFilter(RotationFilterClip clipData)
        {
            _entity.MovementComponent?.SetRotationFilter(clipData);
        }

        public void DisableRotationFilter()
        {
            _entity.MovementComponent?.ClearRotationFilter();
        }

        private VisualRotationOffsetClip _activeVisualRotClip;

        public void EnableVisualRotationOffset(VisualRotationOffsetClip clipData)
        {
            _activeVisualRotClip = clipData;
            if (clipData == null) return;

            // 1. 若配置进入时根节点对齐输入：立即旋转 GameObject 根节点（物理胶囊体立即就位）
            if (clipData.snapRootOnEnter && _entity != null && _entity.MovementComponent != null)
            {
                if (_entity is RoleEntity role && role.InputProvider != null && role.InputProvider.HasRawMoveInput())
                {
                    Vector2 rawInput = role.InputProvider.GetRawMovementDirection();
                    _entity.MovementComponent.FaceToImmediately(rawInput);
                }
            }

            // 2. 注入模型的初始反向偏航补偿角（与父节点旋转相互抵消，第 0 帧世界朝向纹丝不动）
            _entity?.MovementComponent?.SetVisualRotationOffset(clipData.initialCounterYaw);
        }

        public void UpdateVisualRotationOffset(float normalizedTime)
        {
            if (_activeVisualRotClip == null) return;

            if (_activeVisualRotClip.autoRecoverInWindow && _activeVisualRotClip.fadeCurve != null)
            {
                float weight = _activeVisualRotClip.fadeCurve.Evaluate(Mathf.Clamp01(normalizedTime));
                float currentYaw = _activeVisualRotClip.initialCounterYaw * weight;
                _entity?.MovementComponent?.SetVisualRotationOffset(currentYaw);
            }
        }

        public void DisableVisualRotationOffset()
        {
            _activeVisualRotClip = null;
            _entity?.MovementComponent?.ResetVisualRotationOffset();
        }
    }
}
