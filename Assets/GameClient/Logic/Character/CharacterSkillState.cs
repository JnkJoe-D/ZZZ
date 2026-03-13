using Game.FSM;
using Game.Logic.Action.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{


    /// <summary>
    /// 角色的顶层层级状态：技能释放状态，接管 SkillRunner 的运行并监听按键连接
    /// </summary>
    public class CharacterSkillState : CharacterStateBase
    {
        
        private float _skillStartTime;
        public bool IsBasicAttackHold { get; private set; }
        private SkillRunner _currentRunner;
        public override void OnEnter()
        {
            IsBasicAttackHold = false;
            // 监听普攻连接
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

            PlayCurrentSkill();
        }

        private void PlayCurrentSkill()
        {
            _skillStartTime = Time.time;

            var skillConfig = Entity.NextActionToCast;
            if (skillConfig == null) return;

            Debug.Log($"<color=#0FFFFF>[Combo] PlayCurrentSkill {skillConfig.Name}</color>");
            _currentRunner = Entity.ActionPlayer.PlayAction(skillConfig);
            if(_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner.OnComplete += OnSkillEnd;
            }
            
            // 在此修改播放倍率，由于 ActionPlayer 已经挂载好Context
            Entity.ActionPlayer.SetPlaySpeed(
                (skillConfig is SkillConfigSO s && (s.Category == SkillCategory.LightAttack || s.Category == SkillCategory.DashAttack || s.Category == SkillCategory.HeavyAttack)) 
                ? Entity.Config.AttackMultipier : Entity.Config.SkillMultipier
            );

            Debug.Log($"<color=#E2243C>PlaySkill: {skillConfig.Name}</color>");
        }


        private void OnBasicAttackRequest()
        {
            if (Time.time - _skillStartTime < 0.1f) return;
            Entity.ComboController.OnInput(BufferedInputType.BasicAttack);
        }

        private void OnBasicAttackRequestCancel()
        {
        }

        private void OnBasicAttackRequestHoldStart()
        {
            if (Time.time - _skillStartTime < 0.1f) return;
            IsBasicAttackHold = true;
        }

        private void OnBasicAttackRequestHold()
        {
            if (Time.time - _skillStartTime < 0.1f) return;
            Entity.ComboController.OnInput(BufferedInputType.BasicAttackHold);
        }

        private void OnBasicAttackRequestHoldCancel()
        {
            IsBasicAttackHold = false;
        }

        private void OnSpecialAttackRequest()
        {
            if (Time.time - _skillStartTime < 0.1f) return;
            Entity.ComboController.OnInput(BufferedInputType.SpecialAttack);
        }

        private void OnUltimateRequest()
        {
            if (Time.time - _skillStartTime < 0.1f) return;
            Entity.ComboController.OnInput(BufferedInputType.Ultimate);
        }

        private void OnEvadeRequest()
        {
            if (Entity.CanEvade())
            {
                Entity.ComboController.OnInput(BufferedInputType.Evade);
            }
        }
        

        public override void OnUpdate(float deltaTime)
        {
            // Fallback 打断由 ComboController 直接处理，这里不再需要
        }

        public override void OnExit()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner = null;
            }
            Entity.ActionPlayer.StopAction();

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
        }

        private void OnSkillEnd()
        {
            if(Entity.MovementController!=null&&Entity.MovementController.IsGrounded)
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
