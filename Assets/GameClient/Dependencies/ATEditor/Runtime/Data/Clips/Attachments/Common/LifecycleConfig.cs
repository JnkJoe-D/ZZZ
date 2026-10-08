using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 生命周期与打断控制参数块 (通用)
    /// </summary>
    [Serializable]
    public class LifecycleConfig
    {
        [ActionProperty("跟随片段结束销毁")]
        public bool destroyOnEnd = true;

        [ActionProperty("最大存活时长(秒)")]
        [Tooltip("<= 0 表示完全由片段结束或外部逻辑控制")]
        public float maxLifeTime = 0f;

        [ActionProperty("被动打断时销毁")]
        public bool destroyOnInterrupt = true;

        [ActionProperty("结束时停止发射粒子")]
        public bool stopEmissionOnEnd = false;

        public LifecycleConfig Clone()
        {
            return new LifecycleConfig
            {
                destroyOnEnd = this.destroyOnEnd,
                maxLifeTime = this.maxLifeTime,
                destroyOnInterrupt = this.destroyOnInterrupt,
                stopEmissionOnEnd = this.stopEmissionOnEnd
            };
        }
    }
}
