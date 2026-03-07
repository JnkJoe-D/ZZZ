using Game.FSM;
using Game.Logic.Skill.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    public enum BufferedInputType
    {
        None,
        BasicAttack,
        SpecialAttack,
        Ultimate
    }

    /// <summary>
    /// 角色的顶层层级状态：技能释放状态，接管 SkillRunner 的运行并监听按键连接
    /// </summary>
    public class CharacterSkillState : CharacterStateBase
    {
        private SkillEditor.SkillRunner _runner;
        private ProcessContext _context;
        private bool _isSkillFinished;
        
        // 连招缓冲：当玩家在 InputWindow 开启前提前输入时记录
        private BufferedInputType _bufferedInput = BufferedInputType.None;
        private float _bufferedInputTime = -999f;
        
        // 防止玩家由于狂按导致双击判定或前一按键未能及时释放，引入防抖
        private float _skillStartTime;

        private float PRE_INPUT_INTERVAL = 0.5f;
        private SkillConfigSO currentSkill;

        public override void OnEnter()
        {
            _isSkillFinished = false;
            _bufferedInput = BufferedInputType.None;
            Entity.IsComboInputOpen = false;

            // 监听普攻连接和时间轴发出的逻辑事件
            if (Entity.InputProvider != null)
            {
                Entity.InputProvider.OnBasicAttackStarted += OnAttackRequest;
                Entity.InputProvider.OnSpecialAttack += OnSpecialAttackRequest;
                Entity.InputProvider.OnUltimate += OnUltimateRequest;
            }
                
            Entity.OnSkillTimelineEvent += OnReceiveTimelineEvent;

            PlayCurrentSkill();
        }

        private void PlayCurrentSkill()
        {
            _isSkillFinished = false;
            _skillStartTime = Time.time;

            var skillConfig = Entity.NextSkillToCast;
            if (skillConfig == null)
            {
                _isSkillFinished = true; return;
            }

            var timeline = Game.Logic.Skill.SkillManager.Instance.GetOrLoadTimeline(skillConfig);
            if (timeline == null)
            {
                _isSkillFinished = true; return;
            }

            _runner = Game.Logic.Skill.SkillManager.Instance.GetRunner(Entity);
            _context = Game.Logic.Skill.SkillManager.Instance.GetContext(Entity);

            _runner.OnEnd -= OnSkillEnd;
            _runner.OnEnd += OnSkillEnd;

            // 切面刷新面朝向，技能释放前将自己转向摇杆方向
            var inputDir = Entity.InputProvider?.GetMovementDirection() ?? Vector2.zero;
            if (inputDir.sqrMagnitude > 0.01f && Entity.CameraController != null)
            {
                Vector3 worldDir = Entity.CameraController.GetForward() * inputDir.y + Entity.CameraController.GetRight() * inputDir.x;
                worldDir.y = 0;
                if(worldDir != Vector3.zero)
                {
                    Entity.transform.forward = worldDir.normalized;
                }
            }

            _runner.Play(timeline, _context);
            currentSkill = skillConfig;
        }

        private void OnReceiveTimelineEvent(string eventName)
        {
            // 这是在时间轴里自定义的字符串，开启连段输入窗口
            if (eventName == "InputAvailable")
            {
                Entity.IsComboInputOpen = true;

                // 如果在这之前玩家已经提前输入过了，且距离此刻不超过预输入阀值，则允许成功缓冲发招
                if (_bufferedInput != BufferedInputType.None && (Time.time - _bufferedInputTime <= PRE_INPUT_INTERVAL))
                {
                    TryConsumeBufferedInput();
                }
            }
        }

        private void TryConsumeBufferedInput()
        {
            var input = _bufferedInput;
            _bufferedInput = BufferedInputType.None;
            
            switch (input)
            {
                case BufferedInputType.BasicAttack:
                    AdvanceCombo();
                    break;
                case BufferedInputType.SpecialAttack:
                    if (Entity.Config.specialSkill != null) SwitchToSkill(Entity.Config.specialSkill);
                    break;
                case BufferedInputType.Ultimate:
                    if (Entity.Config.Ultimate != null) SwitchToSkill(Entity.Config.Ultimate);
                    break;
            }
        }

        private void OnAttackRequest()
        {
            // 防抖：技能刚开始的 0.1 秒内忽略任何连续输入，防止因为按键过快/双击导致的粘连
            if (Time.time - _skillStartTime < 0.1f) return;

            // 只要连击窗口开了，随时按随时切
            if (Entity.IsComboInputOpen)
            {
                AdvanceCombo();
            }
            else
            {
                // 如果窗口还没开，作为容错机制进行输入缓冲，并记录缓冲时刻
                _bufferedInput = BufferedInputType.BasicAttack;
                _bufferedInputTime = Time.time;
            }
        }

        private void OnSpecialAttackRequest()
        {
            if (Time.time - _skillStartTime < 0.1f) return;
            if (Entity.Config.specialSkill == null) return;

            if (Entity.IsComboInputOpen)
            {
                SwitchToSkill(Entity.Config.specialSkill);
            }
            else
            {
                _bufferedInput = BufferedInputType.SpecialAttack;
                _bufferedInputTime = Time.time;
            }
        }

        private void OnUltimateRequest()
        {
            if (Time.time - _skillStartTime < 0.1f) return;
            if (Entity.Config.Ultimate == null) return;

            if (Entity.IsComboInputOpen)
            {
                SwitchToSkill(Entity.Config.Ultimate);
            }
            else
            {
                _bufferedInput = BufferedInputType.Ultimate;
                _bufferedInputTime = Time.time;
            }
        }

        private void SwitchToSkill(Game.Logic.Skill.Config.SkillConfigSO newSkill)
        {
            Entity.CurrentComboIndex = 0; // 强切技能重置连段
            Entity.NextSkillToCast = newSkill;
            
            _runner.Stop();
            _bufferedInput = BufferedInputType.None;
            Entity.IsComboInputOpen = false;
            PlayCurrentSkill();
        }

        private void AdvanceCombo()
        {
            var attacks = Entity.Config.lightAttacks;
            if (attacks != null && attacks.Length > 0)
            {
                Entity.CurrentComboIndex++;
                
                // 如果当前索引超出了配置的普攻套路，进行循环，回到第一招
                if (Entity.CurrentComboIndex >= attacks.Length)
                {
                    Entity.CurrentComboIndex = 0;
                }

                Entity.NextSkillToCast = attacks[Entity.CurrentComboIndex];
                
                // 停止当前正在播放（且已经开放打断输入）的老技能
                _runner.Stop();
                // 因为复用了状态，不需要走 FSM 重进，直接播即可
                _bufferedInput = BufferedInputType.None;
                Entity.IsComboInputOpen = false;
                PlayCurrentSkill();
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            if (_isSkillFinished)
            {
                Entity.CurrentComboIndex = 0; // 重置连段
                
                if (Entity.MovementController != null && Entity.MovementController.IsGrounded)
                    Machine.ChangeState<CharacterGroundState>();
                else
                    Machine.ChangeState<CharacterAirborneState>();
                return;
            }

            // 如果当前已经进入后摇允许连招的阶段，同时玩家输入了方向键，则允许通过移动打断当前技能的后摇
            if (Entity.IsComboInputOpen && Entity.InputProvider != null && Entity.InputProvider.HasMovementInput())
            {
                Entity.CurrentComboIndex = 0; // 移动打断将重置连段
                if (Entity.MovementController != null && Entity.MovementController.IsGrounded)
                    Machine.ChangeState<CharacterGroundState>();
                else
                    Machine.ChangeState<CharacterAirborneState>();
                return;
            }
            if(currentSkill!=null &&_context!=null)
            {
                if(currentSkill.Category==SkillCategory.LightAttack 
                || currentSkill.Category == SkillCategory.HeavyAttack
                || currentSkill.Category == SkillCategory.DashAttack)
                {
                    _context.GlobalPlaySpeed=Entity.Config.AttackMultipier;
                }
                else
                {
                    _context.GlobalPlaySpeed = Entity.Config.SkillMultipier;
                }
            }
            _runner?.Tick(deltaTime);
        }

        public override void OnExit()
        {
            if (_runner != null)
            {
                _runner.OnEnd -= OnSkillEnd;
                _runner.Stop();
                _runner = null;
            }
            _context = null;
            Entity.NextSkillToCast = null;
            currentSkill = null;
            // 清理监听
            if (Entity.InputProvider != null)
            {
                Entity.InputProvider.OnBasicAttackStarted -= OnAttackRequest;
                Entity.InputProvider.OnSpecialAttack -= OnSpecialAttackRequest;
                Entity.InputProvider.OnUltimate -= OnUltimateRequest;
            }
            Entity.OnSkillTimelineEvent -= OnReceiveTimelineEvent;
            
            Entity.IsComboInputOpen = false;
            _bufferedInput = BufferedInputType.None;
        }

        private void OnSkillEnd()
        {
            _isSkillFinished = true;
            currentSkill = null;
        }
    }
}
