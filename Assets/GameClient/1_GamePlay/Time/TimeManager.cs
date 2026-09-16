using Game.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 业务层玩法时间管理器。
    /// 继承自框架层通用时钟管理器 FrameworkTimeManager，专注管理与战斗实体强绑定的
    /// 顿帧（Hit-Stop）与子弹时间（Bullet-Time）高阶玩法机制。
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

        public TimeManager()
        {
            _instance = this;
            FrameworkTimeManager.Instance = this;
        }

        // ─── 顿帧（Hit-Stop）集中管理 ───
        private class HitStopSession
        {
            public ActionPlayer Attacker;
            public ActionPlayer Victim;
            public float RemainingTime;
            public int SessionId;
        }

        private readonly List<HitStopSession> _activeHitStops = new List<HitStopSession>();
        private int _hitStopSessionCounter = 0;

        /// <summary>
        /// 全局注册一次受击顿帧（Hit-Stop），由全局逻辑时钟驱动恢复，彻底规避受击者失活/销毁导致的协程中断
        /// </summary>
        public int RegisterHitStop(ActionPlayer attacker, ActionPlayer victim, float duration, float scale)
        {
            if (duration <= 0f) return -1;

            _hitStopSessionCounter++;
            int sessionId = _hitStopSessionCounter;

            attacker?.SetPlaySpeed(scale);
            victim?.SetPlaySpeed(scale);

            _activeHitStops.Add(new HitStopSession
            {
                Attacker = attacker,
                Victim = victim,
                RemainingTime = duration,
                SessionId = sessionId
            });

            return sessionId;
        }

        private void TickHitStops(float deltaTime)
        {
            for (int i = _activeHitStops.Count - 1; i >= 0; i--)
            {
                var session = _activeHitStops[i];
                session.RemainingTime -= deltaTime;

                if (session.RemainingTime <= 0f)
                {
                    bool hasOtherAttackerSession = false;
                    bool hasOtherVictimSession = false;

                    for (int j = 0; j < _activeHitStops.Count; j++)
                    {
                        if (j == i) continue;
                        if (session.Attacker != null && (_activeHitStops[j].Attacker == session.Attacker || _activeHitStops[j].Victim == session.Attacker))
                            hasOtherAttackerSession = true;
                        if (session.Victim != null && (_activeHitStops[j].Attacker == session.Victim || _activeHitStops[j].Victim == session.Victim))
                            hasOtherVictimSession = true;
                    }

                    _activeHitStops.RemoveAt(i);

                    if (!hasOtherAttackerSession)
                    {
                        session.Attacker?.RestorePlaySpeed();
                    }

                    if (!hasOtherVictimSession)
                    {
                        session.Victim?.RestorePlaySpeed();
                    }
                }
            }
        }

        public void ClearAllHitStops()
        {
            for (int i = 0; i < _activeHitStops.Count; i++)
            {
                _activeHitStops[i].Attacker?.RestorePlaySpeed();
                _activeHitStops[i].Victim?.RestorePlaySpeed();
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

        /// <summary>当前全局是否正处于玩法子弹时间中（单一真理源）</summary>
        public bool IsBulletTimeActive => _activeBulletTime != null && _activeBulletTime.RemainingDuration > 0f;

        /// <summary>当前正在生效的怪物子弹时间流速</summary>
        public float CurrentBulletTimeScale => _activeBulletTime != null ? _activeBulletTime.TargetScale : 1.0f;

        /// <summary>触发本次子弹时间的发起者实体</summary>
        public CharacterEntity BulletTimeInstigator => _activeBulletTime?.Instigator;

        /// <summary>
        /// 触发定向怪物子弹时间（玩家角色 100% 保持全速，仅场上活跃怪物进入慢动作）
        /// </summary>
        /// <param name="scale">时间流速（如 0.1 表示 10% 速度）</param>
        /// <param name="duration">持续真实物理秒数</param>
        /// <param name="instigator">触发者角色（玩家），绝不减速</param>
        /// <param name="smoothRecover">是否在末段平滑缓出恢复</param>
        public void TriggerBulletTime(float scale, float duration, CharacterEntity instigator = null, bool smoothRecover = true)
        {
            if (duration <= 0f) return;

            float targetScale = Mathf.Clamp(scale, 0.01f, 1.0f);
            _activeBulletTime = new BulletTimeSession
            {
                Instigator = instigator,
                TargetScale = targetScale,
                RemainingDuration = duration,
                TotalDuration = duration,
                SmoothRecover = smoothRecover
            };

            // 定向通知场上所有活跃怪物进入慢动作（过滤触发者自身）
            var monsters = MonsterManager.Instance?.ActiveMonsters;
            if (monsters != null)
            {
                for (int i = 0; i < monsters.Count; i++)
                {
                    var monster = monsters[i];
                    if (monster != null && monster != instigator && monster.gameObject.activeInHierarchy)
                    {
                        monster.ApplyBulletTime(targetScale);
                    }
                }
            }
        }

        /// <summary>
        /// 强制清除当前子弹时间并将所有处于子弹时间的怪物立即恢复正常时速
        /// </summary>
        public void ClearBulletTime()
        {
            if (_activeBulletTime != null)
            {
                _activeBulletTime = null;
                RestoreAllMonstersFromBulletTime();
            }
        }

        private void RestoreAllMonstersFromBulletTime()
        {
            var monsters = MonsterManager.Instance?.ActiveMonsters;
            if (monsters != null)
            {
                for (int i = 0; i < monsters.Count; i++)
                {
                    var monster = monsters[i];
                    if (monster != null)
                    {
                        monster.ExitBulletTime();
                    }
                }
            }
        }

        private void TickBulletTime(float unscaledDelta)
        {
            if (_activeBulletTime == null) return;

            _activeBulletTime.RemainingDuration -= unscaledDelta;
            if (_activeBulletTime.RemainingDuration <= 0f)
            {
                _activeBulletTime = null;
                RestoreAllMonstersFromBulletTime();
            }
            else if (_activeBulletTime.SmoothRecover)
            {
                // 后 35% 时间平滑缓出插值恢复至 1.0
                float recoverThreshold = _activeBulletTime.TotalDuration * 0.35f;
                if (_activeBulletTime.RemainingDuration < recoverThreshold && recoverThreshold > 0.001f)
                {
                    float t = 1.0f - (_activeBulletTime.RemainingDuration / recoverThreshold);
                    float lerpedScale = Mathf.Lerp(_activeBulletTime.TargetScale, 1.0f, t);

                    var monsters = MonsterManager.Instance?.ActiveMonsters;
                    if (monsters != null)
                    {
                        for (int i = 0; i < monsters.Count; i++)
                        {
                            var monster = monsters[i];
                            if (monster != null && monster.gameObject.activeInHierarchy)
                            {
                                var timeData = monster.DataModule?.Get<TimeDilationRuntimeData>();
                                // 只对仍处于子弹时间内的怪物插值，已受击打醒的怪物保持 1.0x 绝不回退
                                if (timeData != null && timeData.IsInBulletTime)
                                {
                                    monster.ApplyBulletTime(lerpedScale);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void OnBeforeUpdate(float unscaledDelta)
        {
            // 驱动全局玩法子弹时间倒计时与缓动恢复
            TickBulletTime(unscaledDelta);
        }

        protected override void OnBeforeLogicStep(float logicDelta)
        {
            // 驱动受击顿帧倒计时
            TickHitStops(logicDelta);
        }

        /// <summary>
        /// 重置所有时钟会话与流速为正常状态 (1.0f)，用于场景切换、系统停机或测试重置
        /// </summary>
        public override void ResetToNormal()
        {
            ClearAllHitStops();
            ClearBulletTime();
            base.ResetToNormal();
        }
    }
}
