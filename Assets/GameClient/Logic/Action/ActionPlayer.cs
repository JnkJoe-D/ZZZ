using Game.Logic;
using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 全局行为播放器，剥离状态机对 Timeline API 的直接依赖
    /// 统一管理 ActionConfigSO 的解析、方向校准、Runner 播放与停止
    /// </summary>
    public class ActionPlayer : IActionRunnerProvider
    {
        private CharacterEntity _entity;
        private ActionRunner _runner;
        private ProcessContext _context;
        
        public ActionConfigAsset CurrentAction { get; private set; }
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// 当前动作开始播放的 Time.time 时间戳。
        /// 用于 TimeSinceActionStartCondition 等计算动作已播放时长。
        /// </summary>
        public float ActionStartTime { get; private set; }
        public float CurrentTime => _runner != null ? _runner.CurrentTime : 0f;
        public float OvershootTime => _runner != null ? _runner.OvershootTime : 0f;

        public event System.Action OnActionComplete;
        public event System.Action OnActionInterrupt;

        public ActionPlayer(CharacterEntity entity)
        {
            _entity = entity;
        }

        public bool PlayAction(ActionConfigAsset config, float crossfadeOverride = -1f, float startTime = 0f)
        {
            if (config == null || config.TimelineAsset == null)
            {
                Debug.LogWarning("ActionPlayer: Tried to play a null config or Missing TimelineAsset.");
                return false;
            }

            // 先验证 Timeline 可用性，避免在确认前就清理旧动作
            var timeline = Game.Logic.ActionManager.Instance.GetOrLoadTimeline(config);
            if (timeline == null)
            {
                Debug.LogWarning($"[ActionPlayer] Timeline cache miss for action '{config.name}'. Skipping — keeping current action alive.");
                return false;
            }

            // Timeline 验证通过，此时才安全地清理旧 Runner
            StopAction();



            // 从管理器索要新 Runner, Context
            _runner = Game.Logic.ActionManager.Instance.GetRunner(_entity);
            _context = Game.Logic.ActionManager.Instance.GetContext(_entity);
            
            // Register self as ISkillRunnerProvider
            _context.UserData = this;

            // 默认恢复全局速度（基础速度 * 当前动作配置速度）
            float baseSpeed = 1.0f; // 如果有角色全局攻速Buff，可以从 _entity 获取并相乘
            _context.GlobalPlaySpeed = baseSpeed * config.PlaybackSpeed;
            if (crossfadeOverride >= 0f)
            {
                _context.TransitionCrossfadeOverride = crossfadeOverride;
            }

            _runner.Play(timeline, _context, startTime);
            CurrentAction = config;
            IsPlaying = true;
            ActionStartTime = Time.time;

            _runner.OnComplete -= HandleRunnerComplete;
            _runner.OnComplete += HandleRunnerComplete;
            _runner.OnInterrupt -= HandleRunnerInterrupt;
            _runner.OnInterrupt += HandleRunnerInterrupt;

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
            if (_runner != null)
            {
                _runner.OnComplete -= HandleRunnerComplete;
                _runner.OnInterrupt -= HandleRunnerInterrupt;
                _runner.Stop();
                _runner = null;
            }
            IsPlaying = false;
            CurrentAction = null;
            ActionStartTime = 0f;
        }

        public void SetPlaySpeed(float speed)
        {
            if (_context != null)
            {
                _context.GlobalPlaySpeed = speed;
            }
        }

        public void RestorePlaySpeed()
        {
            float baseSpeed = 1.0f; // 同上，如果有全局攻速Buff加成
            float actionSpeed = CurrentAction != null ? CurrentAction.PlaybackSpeed : 1.0f;
            SetPlaySpeed(baseSpeed * actionSpeed);
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
