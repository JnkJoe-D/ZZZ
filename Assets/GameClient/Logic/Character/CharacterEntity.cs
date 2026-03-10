using Game.Audio;
using Game.FSM;
using Game.Input;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 主角或玩家控制体的核心枢纽
    /// 管理状态机、持有各项解耦接口（Input、移动、动画），并对这些零件起粘合协调作用
    /// </summary>
    public class CharacterEntity : MonoBehaviour, SkillEditor.ISkillEventHandler
    {
        // === 对底层组件的松散引用 ===
        public IInputProvider InputProvider { get; private set; }
        public IMovementController MovementController { get; private set; }
        public IAnimController AnimController { get; private set; }
        // 专门处理该实体视角的组件（不依赖全局管理，哪怕是没相机的服务器克隆体也可以模拟前向）
        public ICameraController CameraController { get; private set; }
        
        // === 状态机引用 ===
        public FSMSystem<CharacterEntity> StateMachine { get; private set; }
        public FSM.FSMSystem<CharacterEntity> Machine => StateMachine;

        // --- 动作执行器 ---
        public ActionPlayer ActionPlayer { get; private set; }

        // --- 供 State 拿取配置动作 ---
        public Game.Logic.Character.Config.CharacterConfigSO Config { get; private set; }

        // --- 技能连招与事件通讯黑板 ---
        public event System.Action<string> OnSkillTimelineEvent;
        public Game.Logic.Action.Config.ActionConfigSO NextActionToCast { get; set; }
        public bool IsComboInputOpen { get; set; } = false;
        
        // --- 闪避到移动状态的越级信号黑板 ---
        public bool ForceDashNextFrame { get; set; } = false;

        // === 闪避充能与冷却 ===
        public float EvadeTimer { get; set; } = 0f;
        public int EvadeCount { get; set; } = 0;

        public bool CanEvade()
        {
            if (Config == null) return false;
            // 达到限制次数且还在冷却中，则无法闪避
            if (EvadeCount >= Config.evadeLimitedTimes && EvadeTimer > 0)
                return false;
            return true;
        }

        public void RecordEvade()
        {
            if (Config == null) return;
            EvadeCount++;
            EvadeTimer = Config.evadeCoolDown;
        }

        private void Awake()
        {
            // 在实际工业架构中，它们通过依赖注入容器或 Awake GetComponent 汇聚到实体上
            InputProvider = GetComponent<IInputProvider>();
            MovementController = GetComponent<IMovementController>();
            AnimController = GetComponent<IAnimController>();
            // 实体视听组件
            CameraController = GetComponent<ICameraController>();

            if (InputProvider == null || MovementController == null || AnimController == null)
            {
                Debug.LogWarning($"[CharacterEntity] {gameObject.name} 缺少部分控制组件！");
            }
        }
        public void Init(Game.Logic.Character.Config.CharacterConfigSO config)
        {
            Config = config;
            Debug.Log($"[CharacterEntity] Config Injected: Role={config.RoleName}");
            
            // 如果还需要额外拿武器或者其他的，可以直接在这边拿
        }

        private void Start()
        {
            // ===== 不再使用全局动画管理器，现在由配置传入 =====
            if (Config == null)
            {
                Debug.LogWarning("[CharacterEntity] 配置未注入，可能是独立测试场景，跳过动画强行检测。");
            }

            if (StateMachine == null)
            {
                var fsmMgr = Game.FSM.FSMManager.Instance;
                if (fsmMgr != null)
                {
                    StateMachine = fsmMgr.CreateFSM<CharacterEntity>(this);
                    StateMachine.AddState(new CharacterGroundState());
                    StateMachine.AddState(new CharacterAirborneState());
                    StateMachine.AddState(new CharacterSkillState());
                    StateMachine.AddState(new CharacterEvadeState());

                    // 动作播放器
                    ActionPlayer = new ActionPlayer(this);

                    // 初始状态
                    StateMachine.ChangeState<CharacterGroundState>();
                }
                else
                {
                    Debug.LogError("[CharacterEntity] 无法创建状态机，找不到 FSMManager 单例！");
                }
            }

            // 实体入场时，认领当前全局相机的跟随聚焦
            Game.Camera.GameCameraManager.Instance?.SetTarget(this.transform);

            // 注册输入监听
            if (InputProvider != null)
            {
                InputProvider.OnBasicAttackStarted += OnBasicAttack;
                InputProvider.OnSpecialAttack += OnSpecialAttack;
                InputProvider.OnUltimate += OnUltimateAttack;
                InputProvider.OnEvadeStarted += OnEvade;
            }
        }
        private void OnEvade()
        {
            if (!(StateMachine.CurrentState is CharacterEvadeState))
            {
                if (!CanEvade()) return;

                if (InputProvider.HasMovementInput())
                    NextActionToCast = Config.evadeFront[0];
                else
                    NextActionToCast = Config.evadeBack[0];

                StateMachine.ChangeState<CharacterEvadeState>();
            }
        }

        // =====================================
        // 战斗按键响应输入
        // =====================================
        private void OnBasicAttack()
        {
            // 如果不在技能态中，则是起手第一刀
            if (StateMachine.CurrentState is CharacterGroundState)
            {
                if (Config == null) return;
                if (Config.lightAttacks != null && Config.lightAttacks.Length > 0)
                {
                    NextActionToCast = Config.lightAttacks[0];
                    StateMachine.ChangeState<CharacterSkillState>();
                }
            }
        }

        private void OnSpecialAttack()
        {
            if (!(StateMachine.CurrentState is CharacterSkillState))
            {
                if (Config != null && Config.specialSkill != null)
                {
                    NextActionToCast = Config.specialSkill;
                    StateMachine.ChangeState<CharacterSkillState>();
                }
            }
        }

        private void OnUltimateAttack()
        {
            if (!(StateMachine.CurrentState is CharacterSkillState))
            {
                if (Config != null && Config.Ultimate != null)
                {
                    NextActionToCast = Config.Ultimate;
                    StateMachine.ChangeState<CharacterSkillState>();
                }
            }
        }

        // =====================================
        // ISkillEventHandler 接口实现 (来自 Timeline 的事件下发)
        // =====================================
        public void OnSkillEvent(string eventName, System.Collections.Generic.List<SkillEditor.SkillEventParam> parameters)
        {
            OnSkillTimelineEvent?.Invoke(eventName);
        }

        private void Update()
        {
            ActionPlayer?.Tick(Time.deltaTime);

            if (EvadeTimer > 0)
            {
                EvadeTimer -= Time.deltaTime;
                if (EvadeTimer <= 0)
                {
                    EvadeCount = 0;
                    EvadeTimer = 0f;
                }
            }
        }

        private void OnDestroy()
        {
            if (FSMManager.Instance != null && StateMachine != null)
            {
                // 回收该角色的所有计算轮组
                FSMManager.Instance.DestroyFSM(StateMachine);
                StateMachine = null;
            }
        }
    }
}
