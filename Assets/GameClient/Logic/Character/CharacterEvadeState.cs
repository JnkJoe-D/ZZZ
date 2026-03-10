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
        
        private BufferedInputType _bufferedInput = BufferedInputType.None;
        private SkillEditor.SkillRunner _currentRunner;
        
        private SkillConfigSO currentSkill;
        private bool isBackswingStarted;

        public override void OnEnter()
        {
            _bufferedInput = BufferedInputType.None;
            Entity.IsComboInputOpen = false;
            isBackswingStarted = false;

            if (Entity.InputProvider != null)
            {
                // 闪避态只关心最基础的起手攻击（立即派生冲刺普攻）与再次闪避（连闪）
                Entity.InputProvider.OnBasicAttackStarted += OnBasicAttackRequest;
                Entity.InputProvider.OnEvadeStarted += OnEvadeRequest;
            }
            
            Entity.OnSkillTimelineEvent += OnReceiveTimelineEvent;

            PlayCurrentSkill();
        }

        private void PlayCurrentSkill()
        {
            Entity.RecordEvade();

            isBackswingStarted = false;
            _bufferedInput = BufferedInputType.None;
            Entity.IsComboInputOpen = false;

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
            // EditorApplication.isPaused = true;
            // 对于闪避技能，InputAvailable 事件即代表“进入后摇/可交接时刻”
            if (eventName == "InputAvailable")
            {
                isBackswingStarted = true;

                // 检查是否有闪避预输入
                if (_bufferedInput == BufferedInputType.Evade)
                {
                    _bufferedInput = BufferedInputType.None;
                    
                    if (Entity.CanEvade())
                    {
                        // 尝试匹配连段
                        if (TryAdvanceComboFromTransitions(BufferedInputType.Evade))
                        {
                            return; // 成功连段，不再执行后续自动退出
                        }
                    }
                }

                // 如果没有预输入，或者查表没配置下一段闪避，则直接结束闪避态切回 Ground
                FinishEvadeAndReturnToGround();
            }
        }

        private void OnBasicAttackRequest()
        {
            // 普攻直接强制立即派生（如冲刺攻击），只要查表命中
            TryAdvanceComboFromTransitions(BufferedInputType.BasicAttack);
        }

        private void OnEvadeRequest()
        {
            if (isBackswingStarted) return;
            if (!Entity.CanEvade()) return;
            // 还没到后摇时按下闪避，只记录预输入
            _bufferedInput = BufferedInputType.Evade;
        }

        /// <summary>
        /// 评估闪避出的树杈分支
        /// 返回 true 代表成功被接管，发生了状态机转移或重新播放
        /// </summary>
        private bool TryAdvanceComboFromTransitions(BufferedInputType inputCommand)
        {
            if (currentSkill == null || currentSkill.OutTransitions == null || currentSkill.OutTransitions.Count == 0) 
            {
                return false;
            }

            foreach (var transition in currentSkill.OutTransitions)
            {
                if (transition.Evaluate(inputCommand, Entity))
                {
                    Entity.NextActionToCast = transition.NextAction;
                    
                    if (inputCommand == BufferedInputType.Evade)
                    {
                        // 连闪：继续待在 Evade 态重播新段落
                        PlayCurrentSkill();
                    }
                    else
                    {
                        // 攻击：跳回主战斗技能态
                        Machine.ChangeState<CharacterSkillState>();
                    }
                    return true;
                }
            }
            return false;
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
                Entity.InputProvider.OnEvadeStarted -= OnEvadeRequest;
            }
            Entity.OnSkillTimelineEvent -= OnReceiveTimelineEvent;
            
            Entity.IsComboInputOpen = false;
            _bufferedInput = BufferedInputType.None;
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
