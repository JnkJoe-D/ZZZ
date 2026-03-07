using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 处理起跳瞬间与悬空抛物线下落时的基础状态
    /// （在此状态下不允许使用普通攻击，可能允许特定的 AirDash 冲刺）
    /// </summary>
    public class CharacterAirborneState : CharacterStateBase
    {
        // 假设的滞空时间，暂时代替真实的射线落地检测
        private float _dummyFallTimer;

        public override void OnEnter()
        {
            _dummyFallTimer = 0.8f; // 假装我在空中能待 0.8 秒

            // 优先播掉落循环，以后如果有 JumpStart / JumpUp 可以在这分段
            if (Entity.CurrentAnimSet != null && Entity.CurrentAnimSet.FallLoop.clip != null)
            {
                Entity.AnimController?.PlayAnim(Entity.CurrentAnimSet.FallLoop.clip);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            // 在空中依然可以监听一部分方向键进行微弱的空中偏移
            var provider = Entity.InputProvider;
            if (provider != null && provider.HasMovementInput())
            {
                Vector2 inputDir = provider.GetMovementDirection();
                
                Vector3 worldDir;
                if (Entity.CameraController != null)
                {
                    Vector3 camForward = Entity.CameraController.GetForward();
                    Vector3 camRight = Entity.CameraController.GetRight();
                    worldDir = (camForward * inputDir.y + camRight * inputDir.x).normalized;
                }
                else
                {
                    worldDir = new Vector3(inputDir.x, 0, inputDir.y).normalized;
                }
                
                // 空中的移动速度打折
                Entity.MovementController?.Move(worldDir * (2.0f * deltaTime));
            }

            // === TODO: 替换为正式的射线探地检测 OnGrounded ===
            _dummyFallTimer -= deltaTime;
            if (_dummyFallTimer <= 0)
            {
                // 落地表现：如果包里有特制的轻落地、重落地砸地动作可以在这播（虽然马上会被 GroundState 的 Idle 覆盖出去，但能配合跨步融合）
                if (Entity.CurrentAnimSet != null && Entity.CurrentAnimSet.Land.clip != null)
                {
                    Entity.AnimController?.PlayAnim(Entity.CurrentAnimSet.Land.clip);
                }
                
                // 砸到地板了
                Machine.ChangeState<CharacterGroundState>();
            }
        }
    }
}
