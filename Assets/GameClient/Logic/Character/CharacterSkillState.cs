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

        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;

        public override void OnInit(FSMSystem<CharacterEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new ComboInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            IsBasicAttackHold = false;
            PlayCurrentSkill();
        }

        private void PlayCurrentSkill()
        {
            _skillStartTime = Time.time;

            var skillConfig = Entity.RuntimeData.NextActionToCast;
            if (skillConfig == null) return;

            Debug.Log($"<color=#0FFFFF>[Combo] PlayCurrentSkill {skillConfig.Name}</color>");
            _currentRunner = Entity.ActionPlayer.PlayAction(skillConfig);
            // 技能态启动，清除之前的闪避冲刺信号
            Entity.RuntimeData.ForceDashNextFrame = false;

            if(_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner.OnComplete += OnSkillEnd;
            }
            
            // 在此修改播放倍率，由于 ActionPlayer 已经挂载好Context
            Entity.ActionPlayer.SetPlaySpeed(
                (skillConfig is SkillConfigAsset s && (s.Category == SkillCategory.LightAttack || s.Category == SkillCategory.DashAttack || s.Category == SkillCategory.HeavyAttack)) 
                ? Entity.Config.AttackMultipier : Entity.Config.SkillMultipier
            );

            Debug.Log($"<color=#E2243C>PlaySkill: {skillConfig.Name}</color>");
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
            if (Machine.NextState is CharacterActionBackswingState)
            {
                // 不要停止播放，让后摇自然流逝并交给新状态接力
            }
            else
            {
                Entity.ActionPlayer.StopAction();
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
