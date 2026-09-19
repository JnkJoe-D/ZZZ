using ATEditor;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    public class ActionPlayer : IActionRunnerProvider
    {
        private readonly CharacterEntity _entity;
        private ActionRunner _runner;
        private ProcessContext _context;

        public ActionConfigAsset CurrentAction { get; private set; }
        public bool IsPlaying { get; private set; }
        public float ActionStartTime { get; private set; }

        public float CurrentTime => _runner != null ? _runner.CurrentTime : 0f;
        public float Duration => _runner != null && _runner.Timeline != null ? _runner.Timeline.Duration : 0f;
        public float NormalizedTime => Duration > 0f ? Mathf.Clamp01(CurrentTime / Duration) : 0f;

        public event System.Action OnActionStart;
        public event System.Action OnActionComplete;
        public event System.Action OnActionInterrupt;

        private float _externalTimeScale = 1.0f;

        public ActionPlayer(CharacterEntity entity)
        {
            _entity = entity;
            EnsureRuntimeContext();
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
        /// 被动接收外部时钟树推送的最终有效流速（如子弹时间、顿帧等），纯粹应用，零业务计算
        /// </summary>
        public void SetExternalTimeScale(float effectiveScale)
        {
            _externalTimeScale = effectiveScale;
            ApplyEffectivePlaySpeed();
        }

        private void ApplyEffectivePlaySpeed()
        {
            if (_context != null)
            {
                float actionSpeed = CurrentAction != null ? CurrentAction.PlaybackSpeed : 1.0f;
                _context.GlobalPlaySpeed = actionSpeed * _externalTimeScale;
            }
        }

        public bool PlayAction(ActionConfigAsset config, float crossfadeOverride = -1f, float startTime = 0f)
        {
            if (config == null || config.TimelineAsset == null)
            {
                GLog.Warning(LogTags.Action, "ActionPlayer: Tried to play a null config or Missing TimelineAsset.");
                return false;
            }

            // 先验证 Timeline 可用性（纯只读静态资产查询），避免在确认前就清理旧动作
            var timeline = Game.GamePlay.ActionManager.Instance.GetOrLoadTimeline(config);
            if (timeline == null)
            {
                GLog.Warning(LogTags.Action, $"Timeline cache miss for action '{config.name}'. Skipping — keeping current action alive.");
                return false;
            }

            // Timeline 验证通过，此时才安全地清理旧动作
            StopAction();

            EnsureRuntimeContext();

            // 继承当前已经生效的外部有效时钟流速（子弹时间从第 0 帧自动继承）
            ApplyEffectivePlaySpeed();

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

            return true;
        }

        private void HandleRunnerComplete()
        {
            IsPlaying = false;
            OnActionComplete?.Invoke();
        }

        private void HandleRunnerInterrupt()
        {
            IsPlaying = false;
            OnActionInterrupt?.Invoke();
        }

        public void Tick(float deltaTime)
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

                //如果之前处于播放状态，主动派发打断事件
                IsPlaying = false;
                OnActionInterrupt?.Invoke();
            }
            IsPlaying = false;
            CurrentAction = null;
            ActionStartTime = 0f;
        }

        public void Dispose()
        {
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

        public void SetPlaySpeed(float speed)
        {
            if (_context != null)
            {
                _context.GlobalPlaySpeed = speed;
            }
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
