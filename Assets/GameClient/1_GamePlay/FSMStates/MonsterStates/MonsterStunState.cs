using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物失衡瘫痪状态 (MonsterStunState)。
    /// 遵循绝区零原版失衡逻辑：
    /// 1. 进入时打断一切战术意图，根据 StunConfig 播放 StunStart 动作；
    /// 2. 启动受怪物私有时钟 (Clock.EffectiveScale) 流速缩放影响的异步计时；
    /// 3. 倒计时结束后播放 StunEnd 起身动作，并在其 onComplete 回调中清空失衡槽并切回 Idle 待机。
    /// </summary>
    public class MonsterStunState : MonsterStateBase
    {
        private CancellationTokenSource _cts;

        public override void OnEnter()
        {
            // 1. 立即清除战术意图与待发攻击，进入绝对受控失衡
            Context?.Reset();

            var monsterConfig = Entity.Config as MonsterConfigAsset;
            var stunCfg = monsterConfig?.stunConfig;
            float duration = stunCfg != null && stunCfg.DefaultStunDuration > 0f ? stunCfg.DefaultStunDuration : 5.0f;

            GLog.Info(LogTags.Combat, $"[MonsterStunState] 怪物 {Entity.name} 进入失衡瘫痪状态，时长: {duration:F2}s");

            // 2. 播放失衡起手 StunStart (通过 OnInput 压指令)
            if (stunCfg?.StunStart != null)
            {
                SendCommand(stunCfg.StunStart);
            }

            // 3. 启动异步受控时钟计时器
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _ = RunStunCountdownAsync(duration, stunCfg?.StunEnd, _cts.Token);
        }

        private async Task RunStunCountdownAsync(float totalDuration, ActionConfigAsset stunEndAction, CancellationToken ct)
        {
            try
            {
                float elapsed = 0f;

                // 严格受怪物私有时钟流速缩放驱动（子弹时间等比变慢，顿帧瞬间定格）
                while (elapsed < totalDuration)
                {
                    await Task.Yield();

                    if (ct.IsCancellationRequested || Entity == null) return;

                    float effectiveScale = Entity.Clock != null ? Entity.Clock.EffectiveScale : 1.0f;
                    float dt = Time.deltaTime * effectiveScale;
                    elapsed += dt;
                }

                if (ct.IsCancellationRequested || Entity == null) return;

                GLog.Info(LogTags.Combat, $"[MonsterStunState] 怪物 {Entity.name} 失衡倒计时结束，播放 StunEnd");

                // 4. 时间到达，播放 StunEnd，并在其 OnComplete 回调中清空失衡槽并切回 Idle
                if (stunEndAction != null)
                {
                    bool commandSent = SendCommand(stunEndAction, onComplete: () =>
                    {
                        RecoverFromStun();
                    });

                    if (!commandSent)
                    {
                        RecoverFromStun();
                    }
                }
                else
                {
                    RecoverFromStun();
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消（如怪物死亡或状态被主动打断）
            }
            catch (Exception ex)
            {
                GLog.Exception(LogTags.Combat, ex);
                RecoverFromStun();
            }
        }

        private void RecoverFromStun()
        {
            if (Entity == null) return;

            GLog.Info(LogTags.Combat, $"[MonsterStunState] 怪物 {Entity.name} 失衡恢复，清空失衡槽并返回 Idle");

            // 清空失衡槽，自控力由 MonsterBrainCoordinator 自动自愈恢复
            var attrs = Entity.StatusModule?.Attributes;
            attrs?.SetValue(AttributeId.Daze, 0f);

            Machine?.ChangeState<MonsterIdleState>();
        }

        public override void OnExit()
        {
            // 安全退出：取消异步倒计时，杜绝内存泄漏与幽灵回调
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        public override void OnDestroy()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
