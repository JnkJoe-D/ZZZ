using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
{
    public class MonsterHitReactionComponent : HitReactionComponent
    {
        public event System.Action OnHitTimestampChanged;

        private bool _isActionPlaying;
        private bool _isStartingHitAction;
        private float _remainingStunTimer;
        private float _maxSafetyTimer;

        public override void Init(CharacterEntity entity)
        {
            base.Init(entity);
            EnsureActionPlayerSubscribed();
        }

        private void EnsureActionPlayerSubscribed()
        {
            if (_entity?.ActionPlayer != null)
            {
                _entity.ActionPlayer.OnActionComplete -= HandleActionComplete;
                _entity.ActionPlayer.OnActionComplete += HandleActionComplete;

                _entity.ActionPlayer.OnActionInterrupt -= HandleActionInterrupt;
                _entity.ActionPlayer.OnActionInterrupt += HandleActionInterrupt;
            }
        }

        private void OnDestroy()
        {
            if (_entity?.ActionPlayer != null)
            {
                _entity.ActionPlayer.OnActionComplete -= HandleActionComplete;
                _entity.ActionPlayer.OnActionInterrupt -= HandleActionInterrupt;
            }
        }

        protected override void OnInterrupted(HitPipelineContext ctx)
        {
            // 若被打断裁决没通过或无受击表现类型，直接忽略，不阻断自控力也不产生硬直
            if (ctx.SelectedReactionType == cfg.ZZZ.HitReactionType.None) return;

            // 受击时确保 ActionPlayer 事件已正确订阅（解决 Init 时 ActionPlayer 尚未初始化的时序错位问题）
            EnsureActionPlayerSubscribed();

            if (_entity is MonsterEntity monster)
            {
                var hitAction = ctx.ResolvedHitAction;

                // 仅当动作配置有效时才向动作控制器提交指令并标记动作播放中；若动作未配置，仅依赖硬直时间倒计时
                if (hitAction != null && monster.ActionController != null)
                {
                    GLog.Info(LogTags.Combat, $"怪物播放受击动作: {monster.name} → {hitAction.name} (类型: {ctx.SelectedReactionType})");
                    var hitCommand = CharacterCommandFactory.CreateDirectAssetCommand(hitAction);
                    _isStartingHitAction = true;
                    try
                    {
                        monster.ActionController.OnInput(hitCommand);
                    }
                    finally
                    {
                        _isStartingHitAction = false;
                    }
                    // 仅当动作控制器确实成功切入受击动作时才标记播放中；若未切入则仅依赖硬直时间倒计时
                    _isActionPlaying = (monster.ActionController?.CurrentPlayingAction == hitAction);
                }
                else
                {
                    _isActionPlaying = false;
                }

                float targetStunDuration = ctx.HitStunDuration;
                if (targetStunDuration <= 0f && monster.Config?.hitReactionConfig != null)
                {
                    targetStunDuration = monster.Config.hitReactionConfig.GetHitStunDuration(ctx.SelectedReactionType);
                }

                _remainingStunTimer = targetStunDuration;

                // 查询受击动作有效时长，用于保底超时计算
                float actionDuration = 0f;
                if (hitAction != null)
                {
                    if (hitAction.actionTimelineSO != null)
                    {
                        actionDuration = hitAction.actionTimelineSO.Duration;
                    }
                    else if (hitAction.TimelineAsset != null)
                    {
                        var timeline = ActionManager.Instance.GetOrLoadTimeline(hitAction);
                        actionDuration = timeline?.Duration ?? 0f;
                    }
                }

                // 保底超时时间：动作时长 + 硬直时长（若受击动作资源有效则加上其时间，避免正常播放时误触发保底退出）
                _maxSafetyTimer = actionDuration + targetStunDuration;

                if (_hitData != null)
                {
                    _hitData.Set(nameof(_hitData.InHitReaction), true);
                    _hitData.Set(nameof(_hitData.HitSequenceId), _hitData.HitSequenceId + 1);
                    _hitData.Set(nameof(_hitData.HitTriggerTimestamp), Time.frameCount);
                    _hitData.Set(nameof(_hitData.ResolvedHitAction), hitAction);
                    _hitData.Set(nameof(_hitData.CurrentHitStunDuration), targetStunDuration);
                    _hitData.Set(nameof(_hitData.CurrentReactionType), ctx.SelectedReactionType);
                    OnHitTimestampChanged?.Invoke();
                }

            }
        }

        public void OnLogicTick(float logicDeltaTime)
        {
            if (_hitData != null && _hitData.InHitReaction)
            {
                EnsureActionPlayerSubscribed();

                if (_remainingStunTimer > 0f)
                {
                    _remainingStunTimer -= logicDeltaTime;
                }

                if (_maxSafetyTimer > 0f)
                {
                    _maxSafetyTimer -= logicDeltaTime;
                }

                // 权威状态比对：若 ActionController 当前播放的动作已不再是受击动作（已回待机或转入其他动作），
                // 或 ActionPlayer 已播放结束，则权威确认受击动作已经播放完毕（双保险防御）
                var currentPlaying = _entity?.ActionController?.CurrentPlayingAction;
                var resolvedHitAction = _hitData?.ResolvedHitAction;
                if (_isActionPlaying && resolvedHitAction != null)
                {
                    bool isStillPlayingHit = (currentPlaying == resolvedHitAction) && (_entity.ActionPlayer?.IsPlaying ?? false);
                    if (!isStillPlayingHit)
                    {
                        _isActionPlaying = false;
                    }
                }

                // 正常退出条件：动作自然播完 且 硬直倒计时归零
                bool normalExit = !_isActionPlaying && _remainingStunTimer <= 0f;
                // 容灾保底退出：超过最大安全时间强制退出，防止任何未知原因导致的永久死锁
                bool timeoutExit = _maxSafetyTimer <= 0f;

                if (normalExit || timeoutExit)
                {
                    if (timeoutExit && !normalExit)
                    {
                        GLog.Warning(LogTags.Combat, $"[MonsterHitReactionComponent] 怪物 {_entity.name} 受击触发保底超时强制退出！(isActionPlaying={_isActionPlaying}, remainingStun={_remainingStunTimer})");
                    }
                    EndHitReactionSafely();
                }
            }
        }

        /// <summary>
        /// 动作自然播完触发
        /// </summary>
        private void HandleActionComplete()
        {
            _isActionPlaying = false;
        }

        /// <summary>
        /// 动作被中途打断触发（重复受击、失衡、死亡）
        /// </summary>
        private void HandleActionInterrupt()
        {
            if (_isStartingHitAction) return;
            _isActionPlaying = false;
            _remainingStunTimer = 0f;
            _maxSafetyTimer = 0f;
            EndHitReactionSafely();
        }

        private void EndHitReactionSafely()
        {
            _isActionPlaying = false;
            _remainingStunTimer = 0f;
            _maxSafetyTimer = 0f;

            if (_hitData != null && _hitData.InHitReaction)
            {
                _hitData.Set(nameof(_hitData.InHitReaction), false);
                _hitData.Set(nameof(_hitData.ResolvedHitAction), (ActionConfigAsset)null);
                _hitData.Set(nameof(_hitData.CurrentHitStunDuration), 0f);
            }
        }
    }
}
