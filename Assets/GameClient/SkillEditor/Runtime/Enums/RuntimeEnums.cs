namespace SkillEditor
{
    /// <summary>
    /// 播放状态
    /// </summary>
    public enum SkillRunnerState
    {
        Idle,
        Playing,
        Paused
    }

    /// <summary>
    /// 播放模式
    /// </summary>
    public enum PlayMode
    {
        /// <summary>
        /// 编辑器预览
        /// </summary>
        EditorPreview,

        /// <summary>
        /// 运行时（Mono Update 或帧同步共用）
        /// </summary>
        Runtime,
    }
    public enum EAnimLayer
    {
        Locomotion = 0,
        Action = 1,
        Expression = 2
    }

    /// <summary>
    /// 动画混合模式
    /// </summary>
    public enum AnimBlendMode
    {
        Linear,     // 线性混合 (原版)
        SmoothStep  // 平滑混合 (Mathf.SmoothStep) - 默认推荐
    }
    /// <summary>
    /// 挂载点枚举
    /// </summary>
    public enum BindPoint
    {
        LogicRoot,           // 根节点
        Body,           // 身体中心 (胸口/骨盆)
        Head,           // 头部
        LeftHand,       // 左手
        RightHand,      // 右手
        WeaponLeft,     // 左手武器
        WeaponRight,    // 右手武器
        CustomBone      // 自定义骨骼 (需配合 string boneName)
    }
}
