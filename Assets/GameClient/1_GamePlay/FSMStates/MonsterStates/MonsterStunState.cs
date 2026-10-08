using System;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物失衡瘫痪状态 (MonsterStunState)。
    /// 遵循绝区零原版失衡逻辑：
    /// 1. 进入时打断一切战术意图，根据 StunConfig 播放 StunStart 动作；
    /// 2. 状态机自顶向下由 OnUpdate(deltaTime) 推进确定性计时（天然继承怪物局部时钟流速与顿帧）；
    /// 3. 倒计时结束后播放 StunEnd 起身动作，并在其 onComplete 回调中清空失衡槽并切回 Idle 待机。
    /// </summary>
    public class MonsterStunState : MonsterStateBase
    {
        private float _stunDuration = 5.0f;
        private float _elapsedTime = 0f;
        private bool _isRecovering = false;
        private ActionConfigAsset _stunEndAction;

        public override void OnEnter()
        {
            // 1. 立即清除战术意图与待发攻击，进入绝对受控失衡
            Context?.Reset();

            var monsterConfig = Entity.Config as MonsterConfigAsset;
            var stunCfg = monsterConfig?.stunConfig;
            _stunDuration = stunCfg != null && stunCfg.DefaultStunDuration > 0f ? stunCfg.DefaultStunDuration : 5.0f;
            _elapsedTime = 0f;
            _isRecovering = false;
            _stunEndAction = stunCfg?.StunEnd;

            GLog.Info(LogTags.Combat, $"[MonsterStunState] 怪物 {Entity.name} 进入失衡瘫痪状态，时长: {_stunDuration:F2}s");

            // 2. 播放失衡起手 StunStart (通过 OnInput 压指令)
            if (stunCfg?.StunStart != null)
            {
                SendCommand(stunCfg.StunStart);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            if (_isRecovering)
            {
                // 若处于起身阶段但动作已不在播放（防御兜底防止极端异常卡死），恢复并切回 Idle
                if (Entity.ActionController != null && Entity.ActionController.CurrentPlayingAction != _stunEndAction)
                {
                    RecoverFromStun();
                }
                return;
            }

            _elapsedTime += deltaTime;
            if (_elapsedTime >= _stunDuration)
            {
                _isRecovering = true;
                GLog.Info(LogTags.Combat, $"[MonsterStunState] 怪物 {Entity.name} 失衡倒计时结束，播放 StunEnd");

                if (_stunEndAction != null)
                {
                    bool commandSent = SendCommand(_stunEndAction, onComplete: () =>
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
            _stunEndAction = null;
            _isRecovering = false;
            _elapsedTime = 0f;
        }

        public override void OnDestroy()
        {
            _stunEndAction = null;
            _isRecovering = false;
            _elapsedTime = 0f;
        }
    }
}
