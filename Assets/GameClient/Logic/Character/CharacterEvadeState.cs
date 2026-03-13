using Game.FSM;
using Game.Logic.Action.Config;
using SkillEditor;
using UnityEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 角色的顶层层级状态：闪避状态
    /// 接管 SkillRunner 的运行，严格遵守闪避窗口机制：
    /// 1. 到后摇（InputAvailable）前，仅限接收 BasicAttack (立即派生冲刺普攻) 和 Evade (预输入闪避连段)
    /// 2. 进入后摇后，若有预输入直接播后续连段，否则直接退出到 GroundState 恢复移动。
    /// </summary>
    public class CharacterEvadeState : CharacterStateBase
    {
        
        private SkillEditor.SkillRunner _currentRunner;
        private SkillConfigSO currentSkill;
        private bool isBackswingStarted;

        public override void OnEnter()
        {
            isBackswingStarted = false;

            if (Entity.InputProvider != null)
            {
                Entity.InputProvider.OnBasicAttackStarted += OnBasicAttackRequest;
                Entity.InputProvider.OnBasicAttackCanceled += OnBasicAttackRequestCancel;
                Entity.InputProvider.OnBasicAttackHoldStart += OnBasicAttackRequestHoldStart;
                Entity.InputProvider.OnBasicAttackHold += OnBasicAttackRequestHold;
                Entity.InputProvider.OnBasicAttackHoldCancel += OnBasicAttackRequestHoldCancel;
                Entity.InputProvider.OnSpecialAttack += OnSpecialAttackRequest;
                Entity.InputProvider.OnUltimate += OnUltimateRequest;
                Entity.InputProvider.OnEvadeStarted += OnEvadeRequest;
            }

            // 即便采用新设计，Evade 的自然结（跑/跑）可能仍需 Timeline Event 退场
            Entity.OnSkillTimelineEvent += OnReceiveTimelineEvent;

            PlayCurrentSkill();
        }

        private void PlayCurrentSkill()
        {
            Entity.RecordEvade();
            isBackswingStarted = false;

            var skillConfig = Entity.NextActionToCast;
            if (skillConfig == null) return;

            _currentRunner = Entity.ActionPlayer.PlayAction(skillConfig);
            if(_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner.OnComplete += OnSkillEnd;
            }
            Entity.ActionPlayer.SetPlaySpeed(Entity.Config.DodgeMultipier);

            currentSkill = skillConfig as SkillConfigSO;
        }

        private void OnReceiveTimelineEvent(string eventName)
        {
            // 对于闪避技能，InputAvailable 事件现在仅用作“如果没有派生，则自然切回Ground的标记点”
            // 连段逻辑已经完全被 ComboWindow 及 ComboController 接管
            if (eventName == "InputAvailable")
            {
                isBackswingStarted = true;
            }
        }

        private float _skillStartTime;
        private void OnBasicAttackRequest() { Entity.ComboController.OnInput(BufferedInputType.BasicAttack); }
        private void OnBasicAttackRequestCancel() {}
        private void OnBasicAttackRequestHoldStart() {}
        private void OnBasicAttackRequestHold() { Entity.ComboController.OnInput(BufferedInputType.BasicAttackHold); }
        private void OnBasicAttackRequestHoldCancel() {}
        private void OnSpecialAttackRequest() { Entity.ComboController.OnInput(BufferedInputType.SpecialAttack); }
        private void OnUltimateRequest() { Entity.ComboController.OnInput(BufferedInputType.Ultimate); }

        private void OnEvadeRequest()
        {
            if (Entity.CanEvade())
            {
                Entity.ComboController.OnInput(BufferedInputType.Evade);
            }
        }

        // 自然/提前结束闪避状态的统一流转出口
        private void FinishEvadeAndReturnToGround()
        {
            if (Entity.InputProvider != null && Entity.InputProvider.HasMovementInput())
            {
                Entity.ForceDashNextFrame = true; // 告诉地面状态直接切跑
                if (Entity.MovementController != null && Entity.MovementController.IsGrounded)
                    Machine.ChangeState<CharacterGroundState>();
                else
                    Machine.ChangeState<CharacterAirborneState>();
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            // 闪避完无移动输入，自然切回Idle
            if(isBackswingStarted)
            {
                FinishEvadeAndReturnToGround();
                return;
            }
            if (!Entity.ActionPlayer.IsPlaying)
            {
                if (Entity.MovementController != null && Entity.MovementController.IsGrounded)
                    Machine.ChangeState<CharacterGroundState>();
                else
                    Machine.ChangeState<CharacterAirborneState>();
                return;
            }
        }

        public override void OnExit()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner = null;
            }
            Entity.ActionPlayer.StopAction();
            currentSkill = null;
            
            if (Entity.InputProvider != null)
            {
                Entity.InputProvider.OnBasicAttackStarted -= OnBasicAttackRequest;
                Entity.InputProvider.OnBasicAttackCanceled -= OnBasicAttackRequestCancel;
                Entity.InputProvider.OnBasicAttackHoldStart -= OnBasicAttackRequestHoldStart;
                Entity.InputProvider.OnBasicAttackHold -= OnBasicAttackRequestHold;
                Entity.InputProvider.OnBasicAttackHoldCancel -= OnBasicAttackRequestHoldCancel;
                Entity.InputProvider.OnSpecialAttack -= OnSpecialAttackRequest;
                Entity.InputProvider.OnUltimate -= OnUltimateRequest;
                Entity.InputProvider.OnEvadeStarted -= OnEvadeRequest;
            }
            Entity.OnSkillTimelineEvent -= OnReceiveTimelineEvent;
        }

        private void OnSkillEnd()
        {
            currentSkill = null;
            if (Entity.MovementController != null && Entity.MovementController.IsGrounded)
            {
                Entity.Machine.ChangeState<CharacterGroundState>();
            }
            else
            {
                Entity.Machine.ChangeState<CharacterAirborneState>();
            }
        }
    }
}
