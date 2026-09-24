using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 时间轴增益状态驱动进程 (RuntimeBuffProcess)。
    /// 遵循依赖倒置 (DIP) 原则：通过 IBuffHandler 接口通知外部业务逻辑，不直接依赖 Game.Logic 命名空间中的具体类。
    /// </summary>
    [ProcessBinding(typeof(BuffClip), PlayMode.Runtime)]
    public class RuntimeBuffProcess : ProcessBase<BuffClip>
    {
        private IBuffHandler _buffHandler;

        public override void OnEnable()
        {
            _buffHandler = context.GetService<IBuffHandler>();
        }

        public override void OnEnter()
        {
            if (_buffHandler == null || clip == null) return;
            _buffHandler.OnBuffEnter(clip.buffId, clip.clipId, clip.lifetimeMode);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            if (_buffHandler == null || clip == null) return;
            if (clip.lifetimeMode == BuffClipLifetimeMode.ManageByClip)
            {
                _buffHandler.OnBuffExit(clip.buffId, clip.clipId);
            }
        }

        public override void OnStop()
        {
            // 动作打断/切招/退场强力防泄漏兜底：只要生命周期受片段管理，被禁用时必定触发移除
            if (_buffHandler == null || clip == null) return;
            if (clip.lifetimeMode == BuffClipLifetimeMode.ManageByClip)
            {
                _buffHandler.OnBuffExit(clip.buffId, clip.clipId);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _buffHandler = null;
        }
    }
}
