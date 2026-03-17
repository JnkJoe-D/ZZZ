using Game.Logic.Character;
using SkillEditor;
using UnityEngine;

namespace Game.Adapters
{
    public class SkillMovementHandler : ISkillMovementHandler
    {
        private CharacterEntity _entity;

        public SkillMovementHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void Move(Vector3 targetPosition, float speed, float deltaTime)
        {
            if (_entity == null || _entity.MovementController == null) return;
            
            // 计算由于技能要求产生的每帧平移量，方向是 target - current
            // 注意这里只是一个简单的模拟：如果这是一个设定了相对偏移量的位移，需要在 Context 中经过转换
            // 原 `MovementClip.targetPosition` 通常是相对于释放点的相对值或者目标点

            // 为简化，目前先按绝对世界坐标走。真实逻辑可能还要判断是相对本地坐标还是世界等等
            // 假设 targetPosition 是被传入前已经计算好的世界坐标
            Vector3 diff = targetPosition - _entity.transform.position;
            if (diff.sqrMagnitude > 0.001f)
            {
                // 用 Move 向着偏移去
                Vector3 moveVelocity = diff.normalized * speed;
                _entity.MovementController.Move(moveVelocity * deltaTime); 
                // 由于 IMovementController.Move 是依赖底层控制器的(需自行根据实际项目定义)，如果有直接设位置的 API 可以用那个。
            }
        }

        public void Rotate(RotationTargetMode targetMode, float turnSpeed, float deltaTime)
        {
            if (_entity == null || _entity.MovementController == null) return;

            if (targetMode == RotationTargetMode.InputDirection)
            {
                var inputDir = _entity.InputProvider?.GetMovementDirection() ?? Vector2.zero;
                if (inputDir.sqrMagnitude > 0.01f)
                {
                    _entity.MovementController.FaceTo(inputDir, turnSpeed);
                }
            }
            else if (targetMode == RotationTargetMode.EnemyPriority)
            {
                bool targetFound = false;
                if (_entity.TargetFinder != null)
                {
                    var enemy = _entity.TargetFinder.GetEnemy();
                    if (enemy != null)
                    {
                        _entity.MovementController.FaceToTarget(enemy, turnSpeed);
                        targetFound = true;
                    }
                }

                if (!targetFound)
                {
                    var inputDir = _entity.InputProvider?.GetMovementDirection() ?? Vector2.zero;
                    if (inputDir.sqrMagnitude > 0.01f)
                    {
                        _entity.MovementController.FaceTo(inputDir, turnSpeed);
                    }
                }
            }
        }
    }
}
