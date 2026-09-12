using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 实体时间膨胀与子弹时间运行时数据模型。
    /// 承载实体的局部流速、子弹时间标记与打醒恢复逻辑，严格遵守数据与 MonoBehaviour 边界分离规范。
    /// </summary>
    public class TimeDilationRuntimeData : IEntityRuntimeData
    {
        /// <summary>当前是否处于子弹时间/时空裂隙中</summary>
        public bool IsInBulletTime { get; private set; }

        /// <summary>当前时间流速缩放倍率 (1.0f = 正常, 0.1f = 10% 慢动作)</summary>
        public float TimeScale { get; private set; } = 1.0f;

        /// <summary>
        /// 应用子弹时间流速缩放
        /// </summary>
        public void ApplyBulletTime(float scale)
        {
            IsInBulletTime = true;
            TimeScale = Mathf.Clamp(scale, 0.001f, 1.0f);
        }

        /// <summary>
        /// 解除子弹时间，恢复正常时速 (1.0f)
        /// </summary>
        public void ExitBulletTime()
        {
            IsInBulletTime = false;
            TimeScale = 1.0f;
        }

        /// <summary>
        /// 重置状态（用于对象池回收复用）
        /// </summary>
        public void Reset()
        {
            IsInBulletTime = false;
            TimeScale = 1.0f;
        }
    }
}
