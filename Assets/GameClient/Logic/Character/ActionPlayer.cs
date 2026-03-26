using Game.Logic.Action.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 全局行为播放器，剥离状态机对 Timeline API 的直接依赖
    /// 统一管理 ActionConfigSO 的解析、方向校准、Runner 播放与停止
    /// </summary>
    public class ActionPlayer
    {
        private CharacterEntity _entity;
        private SkillRunner _runner;
        private ProcessContext _context;
        
        public ActionConfigAsset CurrentAction { get; private set; }
        public bool IsPlaying { get; private set; }
        public float CurrentTime=>_runner.CurrentTime;

        public ActionPlayer(CharacterEntity entity)
        {
            _entity = entity;
        }

        public SkillRunner PlayAction(ActionConfigAsset config)
        {
            if (config == null || config.TimelineAsset == null)
            {
                Debug.LogWarning("ActionPlayer: Tried to play a null config or Missing TimelineAsset.");
                IsPlaying = false;
                return null;
            }

            // 清理旧 Runner 监听与状态
            StopAction();

            FaceTo(config);

            // 从管理器索要新 Runner, Context, Timeline
            var timeline = Game.Logic.Action.ActionManager.Instance.GetOrLoadTimeline(config);
            if (timeline != null)
            {
                _runner = Game.Logic.Action.ActionManager.Instance.GetRunner(_entity);
                _context = Game.Logic.Action.ActionManager.Instance.GetContext(_entity);
                
                // _runner.OnEnd -= HandleRunnerEnd;
                // _runner.OnEnd += HandleRunnerEnd;

                // 默认恢复全局速度
                _context.GlobalPlaySpeed = 1.0f;
                
                _runner.Play(timeline, _context);
                CurrentAction = config;
                IsPlaying = true;
            }
            else
            {
                Debug.LogWarning($"[ActionPlayer] Timeline cache miss for action '{config.name}'. Ensure preload completed before playback.");
                IsPlaying = false;
            }
            return _runner;
        }

        public void Tick(float deltaTime)
        {
            if (IsPlaying && _runner != null)
            {
                _runner.Tick(deltaTime);
            }
        }

        public void StopAction()
        {
            if (_runner != null)
            {
                // _runner.OnEnd -= HandleRunnerEnd;
                _runner.Stop();
                _runner = null;
            }
            IsPlaying = false;
            CurrentAction = null;
        }

        public void SetPlaySpeed(float speed)
        {
            if (_context != null)
            {
                _context.GlobalPlaySpeed = speed;
            }
        }

        protected virtual void FaceTo(ActionConfigAsset config)
        {
            if (config == null || _entity.MovementController == null) return;

            if (config.TurnMode == ActionTurnMode.InputDirection)
            {
                // 校准面朝向 (技能施放前瞬间转向输入方向)
                var inputDir = _entity.InputProvider?.GetMovementDirection() ?? Vector2.zero;
                if (inputDir.sqrMagnitude > 0.01f)
                {
                    _entity.MovementController.FaceToImmediately(inputDir);
                }
            }
            else if (config.TurnMode == ActionTurnMode.EnemyPriorityThenInput)
            {
                bool targetFound = false;
                
                if (_entity.TargetFinder != null)
                {
                    var enemy = _entity.TargetFinder.GetEnemy();
                    if (enemy != null)
                    {
                        // 发现敌人，立刻朝向敌人
                        _entity.MovementController.FaceToTargetImmediately(enemy);
                        targetFound = true;
                    }
                }

                // 没找到敌人时退回输入方向策略
                if (!targetFound)
                {
                    var inputDir = _entity.InputProvider?.GetMovementDirection() ?? Vector2.zero;
                    if (inputDir.sqrMagnitude > 0.01f)
                    {
                        _entity.MovementController.FaceToImmediately(inputDir);
                    }
                }
            }
        }
    }
}
