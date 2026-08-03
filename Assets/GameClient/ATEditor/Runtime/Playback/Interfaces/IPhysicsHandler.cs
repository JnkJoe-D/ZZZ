using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 技能物理与碰撞控制接口（解耦 ATEditor 核心与底层物理组件）
    /// </summary>
    public interface IPhysicsHandler
    {
        /// <summary>
        /// 设置 CharacterController / 物理系统忽略的碰撞层级
        /// </summary>
        void SetExcludeLayers(LayerMask mask);

        /// <summary>
        /// 获取当前忽略的碰撞层级
        /// </summary>
        LayerMask GetExcludeLayers();

        /// <summary>
        /// 设置角色碰撞体开关（例如瞬步穿梭时完全禁用碰撞体）
        /// </summary>
        void SetCollisionEnabled(bool enabled);

        /// <summary>
        /// 获取碰撞体是否启用
        /// </summary>
        bool GetCollisionEnabled();

        /// <summary>
        /// 设置重力倍率（0 = 完全滞空无重力，0.5 = 缓落，1 = 正常重力，>1 = 快速下坠）
        /// </summary>
        void SetGravityScale(float scale);

        /// <summary>
        /// 获取当前重力倍率
        /// </summary>
        float GetGravityScale();

        /// <summary>
        /// 清空垂直方向的下落速度（在空中施放技能瞬间定在空中）
        /// </summary>
        void ResetVerticalVelocity();

        /// <summary>
        /// 设置推挤抗性 (0~1, 1 = 免疫一切外部推挤)
        /// </summary>
        void SetPushResistance(float resistance);

        /// <summary>
        /// 获取当前推挤抗性
        /// </summary>
        float GetPushResistance();
    }
}
