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
        
        public ActionConfigSO CurrentAction { get; private set; }
        public bool IsPlaying { get; private set; }

        public ActionPlayer(CharacterEntity entity)
        {
            _entity = entity;
        }

        public SkillRunner PlayAction(ActionConfigSO config)
        {
            if (config == null || config.TimelineAsset == null)
            {
                Debug.LogWarning("ActionPlayer: Tried to play a null config or Missing TimelineAsset.");
                IsPlaying = false;
                return null;
            }

            // 清理旧 Runner 监听与状态
            StopAction();

            if(config.TurnMode == ActionTurnMode.InputDirection)
            {
                // 校准面朝向 (技能施放前瞬间转向输入方向)
                var inputDir = _entity.InputProvider?.GetMovementDirection() ?? Vector2.zero;
                if (inputDir.sqrMagnitude > 0.01f && _entity.CameraController != null)
                {
                    Vector3 worldDir = _entity.CameraController.GetForward() * inputDir.y + _entity.CameraController.GetRight() * inputDir.x;
                    worldDir.y = 0;
                    if (worldDir != Vector3.zero)
                    {
                        _entity.transform.forward = worldDir.normalized;
                    }
                }
            }

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
                IsPlaying = false;
            }
            return _runner;
        }

        // private void HandleRunnerEnd()
        // {
        //     IsPlaying = false;
        //     // 不要在此处清理 CurrentAction，可能外部后续还需要判断刚刚播完的是什么动作
        // }

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
    }
}
