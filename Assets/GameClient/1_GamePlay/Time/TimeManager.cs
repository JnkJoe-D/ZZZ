using Game.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 业务层玩法时间管理器。
    /// 继承自 FrameworkTimeManager，装配玩法专属分块时钟（RoleClock, MonsterClock, EnvironmentClock），
    /// 提供纯粹的子弹时间与受击顿帧（Hit-Stop）时钟调度服务。
    /// </summary>
    public class TimeManager : FrameworkTimeManager
    {
        private static TimeManager _instance;
        public new static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TimeManager();
                    FrameworkTimeManager.Instance = _instance;
                }
                return _instance;
            }
        }

        // ─── 玩法阵营分块时钟（挂载在 GameplayClock 下） ───
        public TimeClock RoleClock { get; private set; }
        public TimeClock MonsterClock { get; private set; }
        public TimeClock EnvironmentClock { get; private set; }

        public float MonsterTimeScale
        {
            get => MonsterClock != null ? MonsterClock.LocalScale : 1.0f;
            set { if (MonsterClock != null) MonsterClock.LocalScale = value; }
        }

        public float RoleTimeScale
        {
            get => RoleClock != null ? RoleClock.LocalScale : 1.0f;
            set { if (RoleClock != null) RoleClock.LocalScale = value; }
        }

        public TimeManager()
        {
            _instance = this;
            FrameworkTimeManager.Instance = this;

            RoleClock = new TimeClock("Role", GameplayClock);
            MonsterClock = new TimeClock("Monster", GameplayClock);
            EnvironmentClock = new TimeClock("Environment", GameplayClock);

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            EventCenter.Subscribe<EntityHitInterruptedEvent>(OnEntityHitInterrupted);
            EventCenter.Subscribe<PerfectEvadeTriggeredEvent>(OnPerfectEvadeTriggered);
            EventCenter.Subscribe<HitStopRequestEvent>(OnHitStopRequested);
        }

        // ─── 管理器生命周期装配入口 (Assembler API) ───

        /// <summary>
        /// 供 TeamCharacterSpawner / TeamManager 在生成角色时装配时钟树
        /// </summary>
        public void RegisterRoleClock(TimeClock clock)
        {
            if (clock != null && RoleClock != null)
            {
                RoleClock.AddChild(clock);
            }
        }

        /// <summary>
        /// 供 MonsterManager 在怪物出池时装配时钟树
        /// </summary>
        public void RegisterMonsterClock(TimeClock clock)
        {
            if (clock != null && MonsterClock != null)
            {
                MonsterClock.AddChild(clock);
            }
        }

        // ─── 领域事件闭环响应（私有内聚裁决） ───

        private void OnEntityHitInterrupted(EntityHitInterruptedEvent evt)
        {
            if (_activeBulletTime == null || _activeBulletTime.RemainingDuration <= 0f) return;

            bool isTargetUnderBulletTime = (evt.Victim != null && evt.Victim.Clock != null && evt.Victim.Clock.Parent == MonsterClock)
                                           || evt.Victim is MonsterEntity;
            if (isTargetUnderBulletTime)
            {
                GLog.Info(LogTags.Combat, $"[TimeManager] 监听到怪物 {evt.Victim.name} 受击打断，自发触发打醒并解除子弹时间！");
                ClearBulletTime();
            }
        }

        private void OnPerfectEvadeTriggered(PerfectEvadeTriggeredEvent evt)
        {
            GLog.Info(LogTags.Combat, $"[TimeManager] 监听到极限闪避触发事件，实体 {evt.Evader?.name} 触发子弹时间 ({evt.Duration}s @ {evt.Scale}x)");
            TriggerBulletTime(evt.Scale, evt.Duration, evt.Evader, evt.SmoothRecover);
        }

        private void OnHitStopRequested(HitStopRequestEvent evt)
        {
            RegisterHitStop(evt.AttackerClock, evt.VictimClock, evt.Duration, evt.Scale);
        }

        // ─── 受击顿帧（Hit-Stop）调度 ───
        private class HitStopSession
        {
            public TimeClock Clock;
            public float RemainingTime;
            public int SessionId;
        }

        private readonly List<HitStopSession> _activeHitStops = new List<HitStopSession>();
        private int _hitStopSessionCounter = 0;

        /// <summary>
        /// 全局注册实体时钟的受击顿帧（Hit-Stop）。
        /// 顿帧期间将目标时钟的 LocalScale 设为 0 (完全定格)，倒计时结束后自动还原为 1.0f。
        /// 在父节点处于子弹时间 (如 0.1x) 时，顿帧结束会自动链式恢复为 0.1x，零互斥状态覆盖。
        /// </summary>
        public int RegisterHitStop(TimeClock attackerClock, TimeClock victimClock, float duration, float scale = 0f)
        {
            if (duration <= 0f) return -1;

            _hitStopSessionCounter++;
            int sessionId = _hitStopSessionCounter;

            if (attackerClock != null)
            {
                attackerClock.LocalScale = scale;
                _activeHitStops.Add(new HitStopSession
                {
                    Clock = attackerClock,
                    RemainingTime = duration,
                    SessionId = sessionId
                });
            }

            if (victimClock != null && victimClock != attackerClock)
            {
                victimClock.LocalScale = scale;
                _activeHitStops.Add(new HitStopSession
                {
                    Clock = victimClock,
                    RemainingTime = duration,
                    SessionId = sessionId
                });
            }

            return sessionId;
        }

        /// <summary>
        /// 单时钟简易顿帧接口
        /// </summary>
        public int RegisterHitStop(TimeClock targetClock, float duration, float scale = 0f)
        {
            return RegisterHitStop(targetClock, null, duration, scale);
        }

        private void TickHitStops(float realDeltaTime)
        {
            for (int i = _activeHitStops.Count - 1; i >= 0; i--)
            {
                var session = _activeHitStops[i];
                session.RemainingTime -= realDeltaTime;

                if (session.RemainingTime <= 0f)
                {
                    var targetClock = session.Clock;
                    _activeHitStops.RemoveAt(i);

                    // 检查该时钟是否还有其他未完成的顿帧会话
                    bool hasOtherSession = false;
                    for (int j = 0; j < _activeHitStops.Count; j++)
                    {
                        if (_activeHitStops[j].Clock == targetClock)
                        {
                            hasOtherSession = true;
                            break;
                        }
                    }

                    if (!hasOtherSession && targetClock != null)
                    {
                        targetClock.LocalScale = 1.0f; // 自然恢复！在子弹时间下，有效流速自动变回 0.1x
                    }
                }
            }
        }

        public void ClearAllHitStops()
        {
            for (int i = 0; i < _activeHitStops.Count; i++)
            {
                if (_activeHitStops[i].Clock != null)
                {
                    _activeHitStops[i].Clock.LocalScale = 1.0f;
                }
            }
            _activeHitStops.Clear();
        }

        // ─── 子弹时间（Bullet Time）集中管理 ───
        private class BulletTimeSession
        {
            public CharacterEntity Instigator;
            public float TargetScale;
            public float RemainingDuration;
            public float TotalDuration;
            public bool SmoothRecover;
        }

        private BulletTimeSession _activeBulletTime;

        public bool IsBulletTimeActive => _activeBulletTime != null && _activeBulletTime.RemainingDuration > 0f;
        public float CurrentBulletTimeScale => MonsterClock != null ? MonsterClock.EffectiveScale : 1.0f;
        public CharacterEntity BulletTimeInstigator => _activeBulletTime?.Instigator;

        /// <summary>
        /// 触发定向怪物子弹时间（仅修改 MonsterClock，玩家角色 100% 保持全速）
        /// 全场所有挂载在 MonsterClock 下的怪物时钟瞬间自动级联减速，动态生成的新怪天然继承！
        /// </summary>
        public void TriggerBulletTime(float scale, float duration, CharacterEntity instigator = null, bool smoothRecover = true)
        {
            if (duration <= 0f) return;

            float targetScale = Mathf.Clamp(scale, 0.001f, 1.0f);
            MonsterClock.LocalScale = targetScale; // 一行核心驱动全场怪物！

            _activeBulletTime = new BulletTimeSession
            {
                Instigator = instigator,
                TargetScale = targetScale,
                RemainingDuration = duration,
                TotalDuration = duration,
                SmoothRecover = smoothRecover
            };
        }

        public void ClearBulletTime()
        {
            if (_activeBulletTime != null)
            {
                _activeBulletTime = null;
                if (MonsterClock != null) MonsterClock.LocalScale = 1.0f;
            }
        }

        private void TickBulletTime(float unscaledDelta)
        {
            if (_activeBulletTime == null) return;

            _activeBulletTime.RemainingDuration -= unscaledDelta;
            if (_activeBulletTime.RemainingDuration <= 0f)
            {
                ClearBulletTime();
            }
            else if (_activeBulletTime.SmoothRecover)
            {
                // 后 35% 时间平滑缓出插值恢复至 1.0
                float recoverThreshold = _activeBulletTime.TotalDuration * 0.35f;
                if (_activeBulletTime.RemainingDuration < recoverThreshold && recoverThreshold > 0.001f)
                {
                    float t = 1.0f - (_activeBulletTime.RemainingDuration / recoverThreshold);
                    MonsterClock.LocalScale = Mathf.Lerp(_activeBulletTime.TargetScale, 1.0f, t);
                }
            }
        }

        protected override void OnBeforeUpdate(float unscaledDelta)
        {
            // 顿帧使用不受时间缩放影响的物理真实时间倒计时，确保视觉定格时长真实可靠
            TickHitStops(unscaledDelta);

            // 驱动全局怪物子弹时间倒计时与缓动恢复
            TickBulletTime(unscaledDelta);
        }

        public override void ResetToNormal()
        {
            ClearAllHitStops();
            ClearBulletTime();
            if (RoleClock != null) RoleClock.LocalScale = 1.0f;
            if (MonsterClock != null) MonsterClock.LocalScale = 1.0f;
            if (EnvironmentClock != null) EnvironmentClock.LocalScale = 1.0f;
            base.ResetToNormal();
        }
    }
}
