using Game.FSM;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 动作后摇等待态 (Action Backswing State / Recovery State)
    /// 职责：扮演“逻辑上的待机，表现上的延续”。
    /// 1. 允许通过位移输入瞬间打断（回 GroundState）。
    /// 2. 允许接收普攻/闪避/技能等全量指令并进行连段裁决（回 SkillState/EvadeState）。
    /// 3. 若无任何操作且当前动作播放完毕，则自然回归地面。
    /// </summary>
    public class CharacterActionBackswingState : CharacterStateBase
    {
        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;

        public override void OnInit(FSMSystem<CharacterEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new DefaultInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
        }

        public override void OnUpdate(float deltaTime)
        {
            // 1. 打断逻辑：只要有摇杆推入，立即视为进入移动请求
            if (Entity.InputProvider != null && Entity.InputProvider.HasMovementInput())
            {
                ReturnToGround();
                return;
            }

            // 2. 自然结束逻辑：
            // 现在由 ComboController.OnWindowExit(Fallback) 确定性地驱动，无需轮询探测。
        }

        private void ReturnToGround()
        {
            Machine.ChangeState<CharacterGroundState>();
        }

        public override void OnExit()
        {
        }
    }
}
