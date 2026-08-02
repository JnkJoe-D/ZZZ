namespace ATEditor
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
    /// 挂载点枚举（标准 Bip001 骨骼预设）
    /// </summary>
    public enum BindPoint
    {
        LogicRoot,              // 逻辑根节点 (GameObject Root)

        // 躯干
        Bip001,                 // Bip001 (根骨骼)
        Bip001_Pelvis,          // Bip001 Pelvis (骨盆)
        Bip001_Spine,           // Bip001 Spine (脊椎下)
        Bip001_Spine1,          // Bip001 Spine1 (脊椎中)
        Bip001_Spine2,          // Bip001 Spine2 (胸部/脊椎上)
        Bip001_Neck,            // Bip001 Neck (颈部)
        Bip001_Head,            // Bip001 Head (头部)

        // 上肢 - 左
        Bip001_L_Clavicle,      // Bip001 L Clavicle (左锁骨)
        Bip001_L_UpperArm,      // Bip001 L UpperArm (左大臂)
        Bip001_L_Forearm,       // Bip001 L Forearm (左小臂)
        Bip001_L_Hand,          // Bip001 L Hand (左手)

        // 上肢 - 右
        Bip001_R_Clavicle,      // Bip001 R Clavicle (右锁骨)
        Bip001_R_UpperArm,      // Bip001 R UpperArm (右大臂)
        Bip001_R_Forearm,       // Bip001 R Forearm (右小臂)
        Bip001_R_Hand,          // Bip001 R Hand (右手)

        // 下肢 - 左
        Bip001_L_Thigh,         // Bip001 L Thigh (左大腿)
        Bip001_L_Calf,          // Bip001 L Calf (左小腿)
        Bip001_L_Foot,          // Bip001 L Foot (左脚)
        Bip001_L_Toe0,          // Bip001 L Toe0 (左脚趾)

        // 下肢 - 右
        Bip001_R_Thigh,         // Bip001 R Thigh (右大腿)
        Bip001_R_Calf,          // Bip001 R Calf (右小腿)
        Bip001_R_Foot,          // Bip001 R Foot (右脚)
        Bip001_R_Toe0,          // Bip001 R Toe0 (右脚趾)

        // 道具 / 武器挂点
        Bip001_Prop1,           // Bip001 Prop1 (道具/武器挂点)

        // 自定义骨骼
        CustomBone              // 自定义骨骼 (需配合 customBoneName)
    }

    /// <summary>
    /// 检测盒跟随绑定点模式
    /// </summary>
    public enum HitBoxFollowMode
    {
        None,                   // 不跟随 (固定在片段触发时的初始位置和旋转)
        PositionOnly,           // 仅同步世界坐标 (位置跟随绑定点，旋转保持固定)
        RotationOnly,           // 仅旋转 (旋转跟随绑定点，位置保持固定)
        Both                    // 全部同步 (位置和旋转均实时跟随绑定点)
    }
}
