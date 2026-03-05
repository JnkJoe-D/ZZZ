namespace SkillEditor
{
    /// <summary>
    /// Process 泛型基类，提供类型安全的片段数据访问和默认空实现
    /// </summary>
    /// <typeparam name="TClip">关联的 ClipBase 子类型</typeparam>
    public abstract class ProcessBase<TClip> : IProcess where TClip : ClipBase
    {
        /// <summary>
        /// 当前片段数据（强类型）
        /// </summary>
        protected TClip clip;

        /// <summary>
        /// 播放上下文（组件访问、清理注册等）
        /// </summary>
        protected ProcessContext context;

        public void Initialize(ClipBase clipData, ProcessContext context)
        {
            this.clip = (TClip)clipData;
            this.context = context;
        }

        /// <summary>
        /// 重置为初始状态（对象池复用前调用）
        /// 子类如有额外字段需 override 并调用 base.Reset()
        /// </summary>
        public virtual void Reset()
        {
            clip = default;
            context = null;
        }  

        public virtual void OnEnable() { }

        public virtual void OnEnter() { }

        /// <summary>
        /// 每帧更新，子类必须实现
        /// </summary>
        public abstract void OnUpdate(float currentTime, float deltaTime);
        public virtual void OnPause(){}
        public virtual void OnResume(){}

        public virtual void OnExit() { }

        public virtual void OnDisable() { }
    }
}
