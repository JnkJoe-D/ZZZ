using Game.FSM;
using Game.Logic.Action.Config;
using SkillEditor;
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
        private SkillConfigAsset currentSkill;
        private bool isBackswingStarted;

        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;

        public override bool CanEnter()
        {
            return Entity != null && Entity.RuntimeData != null && Entity.RuntimeData.CanEvade(Entity.Config);
        }

        public override void OnInit(FSMSystem<CharacterEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new ComboInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            isBackswingStarted = false;
            PlayCurrentSkill();
        }

        private void PlayCurrentSkill()
        {
            Entity.RuntimeData.RecordEvade(Entity.Config);
            isBackswingStarted = false;

            var skillConfig = Entity.RuntimeData.NextActionToCast;
            if (skillConfig == null) return;

            _currentRunner = Entity.ActionPlayer.PlayAction(skillConfig);
            if(_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner.OnComplete += OnSkillEnd;
            }

            // 识别是否是前闪避，用于后续衔接 Dash
            bool isFrontEvade = false;
            if (Entity.Config.evadeFront != null)
            {
                foreach (var ev in Entity.Config.evadeFront)
                {
                    if (ev == skillConfig) { isFrontEvade = true; break; }
                }
            }
            Entity.RuntimeData.ForceDashNextFrame = isFrontEvade;

            Entity.ActionPlayer.SetPlaySpeed(Entity.Config.DodgeMultipier);

            currentSkill = skillConfig as SkillConfigAsset;
        }

        // 自然/提前结束闪避状态的统一流转出口
        private void FinishEvadeAndReturnToGround()
        {
            if (Entity.InputProvider != null && Entity.InputProvider.HasMovementInput())
            {
                Machine.ChangeState<CharacterGroundState>();
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
            //兜底，播放完强转
            if (!Entity.ActionPlayer.IsPlaying)
            {
                Machine.ChangeState<CharacterGroundState>();
                return;
            }
            //旋转
            var provider = Entity.InputProvider;
            if(provider==null)return;
            Vector2 inputDir = provider.GetMovementDirection();
            Entity.MovementController?.FaceTo(inputDir,5f);
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
            currentSkill = null;
        }
        private void OnSkillEnd()
        {
            currentSkill = null;
            Entity.Machine.ChangeState<CharacterGroundState>();
        }
    }
}
