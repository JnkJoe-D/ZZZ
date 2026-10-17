using ATEditor;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    public class ActionPlayInfo
    {
        public ActionConfigAsset ActionConfig;
        public ActionTransitionInfo TransitionInfo;
        public ActionEventInfo EventInfo;
    }

    public class ActionTransitionInfo
    {
        public float CrossfadeDuration = -1f;
        public float StartTime = 0f;
    }

    public class ActionEventInfo
    {
        public System.Action OnActionStart;
        public System.Action OnActionComplete;
        public System.Action OnActionInterrupt;
    }

    /// <summary>
    /// 角色实体业务层专用的动作播放信息。
    /// 继承自 ActionPlayer 底层所需的 ActionPlayInfo，并实现 ICommandPayload，
    /// 打通从源头指令压入、路由仲裁、控制器到播放器的统一参数传递。
    /// </summary>
    public class CharacterActionPlayInfo : ActionPlayInfo, ICommandPayload
    {
        public long CommandId;
        public string RouteTag;
        public CommandRouteSource RouteSource = CommandRouteSource.None;
        public ICommandPayload SourcePayload;

        public CharacterActionPlayInfo() { }

        public CharacterActionPlayInfo(ActionConfigAsset action, float crossfade = -1f, float startTime = 0f)
        {
            ActionConfig = action;
            if (crossfade >= 0f || startTime > 0f)
            {
                TransitionInfo = new ActionTransitionInfo
                {
                    CrossfadeDuration = crossfade,
                    StartTime = startTime
                };
            }
        }
    }

    public class ActionPlayer : IActionRunnerProvider, IEntityModule
    {
        private CharacterEntity _entity;
        private ActionRunner _runner;
        private ProcessContext _context;

        public ActionConfigAsset CurrentAction { get; private set; }
        public bool IsPlaying { get; private set; }
        public float ActionStartTime { get; private set; }

        public float CurrentTime => _runner != null ? _runner.CurrentTime : 0f;
        public float Duration => _runner != null && _runner.Timeline != null ? _runner.Timeline.Duration : 0f;
        public float NormalizedTime => Duration > 0f ? Mathf.Clamp01(CurrentTime / Duration) : 0f;

        private event System.Action OnActionStart;
        private event System.Action OnActionComplete;
        private event System.Action OnActionInterrupt;

        private float _externalTimeScale = 1.0f;

        public void Initialize(CharacterEntity owner)
        {
            _entity = owner;
            EnsureRuntimeContext();
            SetExternalTimeScale(_entity.Clock != null ? _entity.Clock.EffectiveScale : 1.0f);
        }

        public void LogicTick(float logicDeltaTime)
        {
            Tick(logicDeltaTime);
        }

        private void EnsureRuntimeContext()
        {
            if (_context == null && _entity != null && _entity.gameObject != null)
            {
                _context = new ProcessContext(_entity.gameObject, ATEditor.PlayMode.Runtime, ATServiceFactory.ProvideService);
                _context.UserData = this;
                _runner = new ActionRunner(ATEditor.PlayMode.Runtime);
            }
        }

        /// <summary>
        /// 被动接收外部时钟树推送的最终有效流速（如子弹时间、顿帧等），同步表现层播放速度
        /// </summary>
        public void SetExternalTimeScale(float effectiveScale)
        {
            _externalTimeScale = effectiveScale;
            if (_context != null)
            {
                _context.PresentationPlaySpeed = effectiveScale;
            }
        }

        private void ClearActionEvents()
        {
            OnActionStart = null;
            OnActionComplete = null;
            OnActionInterrupt = null;
        }

        private void BindActionEvents(ActionEventInfo eventInfo)
        {
            ClearActionEvents();
            if (eventInfo != null)
            {
                if (eventInfo.OnActionStart != null)
                    OnActionStart += eventInfo.OnActionStart;
                if (eventInfo.OnActionComplete != null)
                    OnActionComplete += eventInfo.OnActionComplete;
                if (eventInfo.OnActionInterrupt != null)
                    OnActionInterrupt += eventInfo.OnActionInterrupt;
            }
        }

        public bool PlayAction(ActionPlayInfo playInfo)
        {
            var config = playInfo?.ActionConfig;
            if (config == null || (config.TimelineAsset == null && config.actionTimelineSO == null))
            {
                GLog.Warning(LogTags.Action, "ActionPlayer: Tried to play a null config or Missing TimelineAsset.");
                return false;
            }

            // 先验证 Timeline 可用性（纯只读静态资产查询），避免在确认前就清理旧动作
            var timeline = ActionManager.Instance.GetOrLoadTimeline(config);
            if (timeline == null)
            {
                GLog.Warning(LogTags.Action, $"Timeline cache miss for action '{config.name}'. Skipping — keeping current action alive.");
                return false;
            }

            // Timeline 验证通过，此时才安全地清理旧动作（若旧动作还在播放，StopAction 会触发旧动作的打断事件并清空旧回调）
            StopAction();

            // 仅在新的 PlayAction 方法中赋值以及进行订阅/反订阅操作
            BindActionEvents(playInfo.EventInfo);

            EnsureRuntimeContext();

            // 继承当前已经生效的外部有效时钟流速（子弹时间/顿帧从第 0 帧自动继承）
            if (_context != null)
            {
                _context.PresentationPlaySpeed = _externalTimeScale;
            }

            float crossfadeOverride = playInfo.TransitionInfo != null ? playInfo.TransitionInfo.CrossfadeDuration : -1f;
            float startTime = playInfo.TransitionInfo != null ? playInfo.TransitionInfo.StartTime : 0f;

            if (crossfadeOverride >= 0f && _context != null)
            {
                _context.TransitionCrossfadeOverride = crossfadeOverride;
            }

            ActionRunner runner = _runner;
            CurrentAction = config;
            IsPlaying = true;
            ActionStartTime = Time.time; // 输入相关的时间判定，使用 Time.time 作为参考

            if (runner != null)
            {
                runner.OnComplete -= HandleRunnerComplete;
                runner.OnComplete += HandleRunnerComplete;
                runner.OnInterrupt -= HandleRunnerInterrupt;
                runner.OnInterrupt += HandleRunnerInterrupt;

                runner.Play(timeline, _context, startTime);
            }

            // 防重入保护：如果在 runner.Play (如第 0 帧 Clip 的 OnEnter 自动过渡) 内部递归触发了新动作播放，
            // 此时 _runner 与 CurrentAction 已被内层的全新动作接管，外层帧栈决不能再将其覆盖！
            if (_runner != runner || CurrentAction != config)
            {
                return false;
            }

            OnActionStart?.Invoke();
            return true;
        }

        private void HandleRunnerComplete()
        {
            IsPlaying = false;
            var completeCallback = OnActionComplete;
            ClearActionEvents();
            completeCallback?.Invoke();
        }

        private void HandleRunnerInterrupt()
        {
            IsPlaying = false;
            var interruptCallback = OnActionInterrupt;
            ClearActionEvents();
            interruptCallback?.Invoke();
        }

        private void Tick(float deltaTime)
        {
            if (IsPlaying && _runner != null)
            {
                _runner.Tick(deltaTime);
                if (_runner.CurrentState == ActionRunner.State.None)
                {
                    IsPlaying = false;
                }
            }
        }

        public void StopAction()
        {
            if (_runner != null && IsPlaying)
            {
                _runner.OnComplete -= HandleRunnerComplete;
                _runner.OnInterrupt -= HandleRunnerInterrupt;
                _runner.Stop();

                // 如果之前处于播放状态，主动派发打断事件并清空回调
                IsPlaying = false;
                var interruptCallback = OnActionInterrupt;
                ClearActionEvents();
                interruptCallback?.Invoke();
            }
            ClearActionEvents();
            IsPlaying = false;
            CurrentAction = null;
            ActionStartTime = 0f;
        }

        public void Dispose()
        {
            _entity = null;
            StopAction();
            if (_runner != null)
            {
                _runner.OnComplete -= HandleRunnerComplete;
                _runner.OnInterrupt -= HandleRunnerInterrupt;
                _runner = null;
            }
            _context?.Clear();
            _context = null;
        }

        ActionRunner ATEditor.IActionRunnerProvider.GetRunner()
        {
            return _runner;
        }
        
        public void RewindTo(float targetTime)
        {
            if (_runner != null)
            {
                _runner.Seek(targetTime, 0f);
            }
        }

        public void SendTimelineMessage(string message)
        {
            _context?.SendTimelineMessage(message);
        }

        public void SetTimelineFlag(string flag)
        {
            if (_context != null && !string.IsNullOrEmpty(flag))
            {
                _context.Flags.Add(flag);
            }
        }
    }
}
