using System;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 框架层通用时钟管理器基类。
    /// 基于层次化时钟树（Hierarchical TimeClock），提供多层级时间流速缩放、UI/玩法时钟累加驱动、
    /// 固定逻辑步长 Tick 广播以及协程等待基础。
    /// </summary>
    public class FrameworkTimeManager : ITimeService
    {
        private static FrameworkTimeManager _instance;
        public static FrameworkTimeManager Instance
        {
            get => _instance ??= new FrameworkTimeManager();
            protected set => _instance = value;
        }

        // ─── 核心层次化时钟树拓扑 ───
        public TimeClock RootClock { get; private set; }
        public TimeClock GameplayClock { get; private set; }
        public TimeClock UIClock { get; private set; }

        // ─── ITimeService 接口契约实现（属性门面映射） ───
        public float GlobalTimeScale
        {
            get => RootClock != null ? RootClock.LocalScale : 1.0f;
            set { if (RootClock != null) RootClock.LocalScale = value; }
        }

        public float GameplayTimeScale
        {
            get => GameplayClock != null ? GameplayClock.LocalScale : 1.0f;
            set { if (GameplayClock != null) GameplayClock.LocalScale = value; }
        }

        public float UITimeScale
        {
            get => UIClock != null ? UIClock.LocalScale : 1.0f;
            set { if (UIClock != null) UIClock.LocalScale = value; }
        }

        public float FinalGameplayScale => GameplayClock != null ? GameplayClock.EffectiveScale : 1.0f;
        public float FinalUIScale => UIClock != null ? UIClock.EffectiveScale : 1.0f;

        // ─── 逻辑帧配置 ───
        public int TargetLogicFrameRate { get; set; } = 60;
        public float LogicDeltaTime => 1f / TargetLogicFrameRate;

        // ─── 时间状态 ───
        public float GameplayTime { get; protected set; }
        public float UITime { get; protected set; }

        protected float _gameplayAccumulator;

        // ─── 事件分发 ───
        /// <summary>
        /// 玩法逻辑固定频率 Tick，参数固定为 LogicDeltaTime。用于驱动 Entity, AI, ActionRunner 等
        /// </summary>
        public event Action<float> OnGameplayLogicTick;

        /// <summary>
        /// UI 等基于渲染帧的 Tick，参数为带时间缩放的 delta。用于驱动 UI, 渲染插值等
        /// </summary>
        public event Action<float> OnUIRenderTick;

        public FrameworkTimeManager()
        {
            Instance = this;
            InitializeClockTree();
        }

        private void InitializeClockTree()
        {
            RootClock = new TimeClock("Root");
            GameplayClock = new TimeClock("Gameplay", RootClock);
            UIClock = new TimeClock("UI", RootClock);
        }

        public virtual void Update()
        {
            float unscaledDelta = Time.unscaledDeltaTime;

            OnBeforeUpdate(unscaledDelta);

            // 1. UI 及渲染层帧更新（受 UIClock.EffectiveScale 影响）
            float uiDelta = unscaledDelta * FinalUIScale;
            UITime += uiDelta;
            OnUIRenderTick?.Invoke(uiDelta);

            // 2. 玩法逻辑层固定步长更新（受 GameplayClock.EffectiveScale 影响）
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

                OnBeforeLogicStep(logicDelta);

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

        /// <summary>
        /// 在每帧主时钟推进前调用，供派生类扩展（如顿帧/子弹时间计时）
        /// </summary>
        protected virtual void OnBeforeUpdate(float unscaledDelta) { }

        /// <summary>
        /// 在每个固定逻辑步长派发前调用，供派生类扩展
        /// </summary>
        protected virtual void OnBeforeLogicStep(float logicDelta) { }

        public virtual void PauseGameplay() => GameplayTimeScale = 0f;
        public virtual void ResumeGameplay() => GameplayTimeScale = 1.0f;

        /// <summary>
        /// 重置所有时钟会话与流速为正常状态 (1.0f)
        /// </summary>
        public virtual void ResetToNormal()
        {
            if (RootClock != null) RootClock.LocalScale = 1.0f;
            if (GameplayClock != null) GameplayClock.LocalScale = 1.0f;
            if (UIClock != null) UIClock.LocalScale = 1.0f;
            Time.timeScale = 1.0f;
        }
    }
}
