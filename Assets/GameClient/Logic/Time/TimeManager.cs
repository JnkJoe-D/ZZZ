using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Framework;

namespace Game.Logic
{
    public class TimeManager : Singleton<TimeManager>
    {
        // ─── 时间缩放层级（乘法关系） ───
        public float GlobalTimeScale { get; set; } = 1.0f;     // 全局时停 / 慢动作
        public float GameplayTimeScale { get; set; } = 1.0f;   // 游戏玩法时停
        public float UITimeScale { get; set; } = 1.0f;         // UI面板独立缩放
        
        public float FinalGameplayScale => GlobalTimeScale * GameplayTimeScale;
        public float FinalUIScale => GlobalTimeScale * UITimeScale;
        
        // ─── 逻辑帧配置 ───
        public int TargetLogicFrameRate = 60;
        public float LogicDeltaTime => 1f / TargetLogicFrameRate;

        // ─── 时间状态 ───
        public float GameplayTime { get; private set; }
        public float UITime { get; private set; }
        
        private float _gameplayAccumulator;

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
        
        // ─── 事件分发 ───
        /// <summary>
        /// 玩法逻辑固定频率 Tick，参数固定为 LogicDeltaTime。用于驱动 Entity, AI, ActionRunner 等
        /// </summary>
        public event Action<float> OnGameplayLogicTick;
        
        /// <summary>
        /// UI 等基于渲染帧的 Tick，参数为带时间缩放的 delta。用于驱动 UI, 渲染插值等
        /// </summary>
        public event Action<float> OnUIRenderTick;

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

        public void Update()
        {
            float unscaledDelta = Time.unscaledDeltaTime;

            // 驱动全局玩法子弹时间倒计时与缓动恢复
            TickBulletTime(unscaledDelta);

            // 1. UI及渲染层帧更新（受 UI 缩放影响）
            float uiDelta = unscaledDelta * FinalUIScale;
            UITime += uiDelta;
            OnUIRenderTick?.Invoke(uiDelta);
            
            // 2. 玩法逻辑层固定步长更新（受 Gameplay 缩放影响）
            float gameplayUnscaledDelta = unscaledDelta * FinalGameplayScale;
            _gameplayAccumulator += gameplayUnscaledDelta;
            
            // 防止卡顿导致的“死亡螺旋”（单帧最多追赶 5 次逻辑帧）
            int maxCatchUp = 5;
            int catchUpCount = 0;
            
            float logicDelta = LogicDeltaTime;
            while (_gameplayAccumulator >= logicDelta && catchUpCount < maxCatchUp)
            {
                _gameplayAccumulator -= logicDelta;
                GameplayTime += logicDelta;
                
                // 驱动受击顿帧倒计时
                TickHitStops(logicDelta);

                // 派发一次固定帧距的逻辑更新
                OnGameplayLogicTick?.Invoke(logicDelta);
                
                catchUpCount++;
            }
            
            // 若落后太多，舍弃丢弃的时间片段防止永久性延迟
            if (catchUpCount >= maxCatchUp)
            {
                _gameplayAccumulator = _gameplayAccumulator % logicDelta;
            }
        }
        
        public void PauseGameplay() => GameplayTimeScale = 0f;
        public void ResumeGameplay() => GameplayTimeScale = 1.0f;

        /// <summary>
        /// 重置所有时钟会话与流速为正常状态 (1.0f)，用于场景切换、系统停机或测试重置
        /// </summary>
        public void ResetToNormal()
        {
            ClearAllHitStops();
            ClearBulletTime();
            GlobalTimeScale = 1.0f;
            GameplayTimeScale = 1.0f;
            UITimeScale = 1.0f;
            Time.timeScale = 1.0f;
        }
    }
}
