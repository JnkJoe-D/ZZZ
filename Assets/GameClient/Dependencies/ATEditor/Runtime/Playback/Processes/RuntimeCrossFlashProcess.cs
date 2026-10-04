using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 全屏十字闪光时间轴运行时驱动进程。
    /// 遵循：OnEnter 触发，以独立固定时长结束，OnExit / 打断时不提前终止。
    /// 流速完全由表现层直接与实体的私有时钟 (CharacterEntity.Clock) 联动，时间轴内部无需进行任何速度中继或事件监听。
    /// </summary>
    [ProcessBinding(typeof(CrossFlashClip), PlayMode.Runtime)]
    public class RuntimeCrossFlashProcess : ProcessBase<CrossFlashClip>
    {
        private ICrossFlashHandler _handler;
        private IBoneGetter _boneGetter;

        public override void OnEnable()
        {
            _handler = context.GetService<ICrossFlashHandler>();
            _boneGetter = context.GetService<IBoneGetter>();
        }

        public override void OnEnter()
        {
            if (_handler == null || clip == null) return;

            Transform bindTrans = null;
            if (_boneGetter != null)
            {
                bindTrans = _boneGetter.GetBone(clip.bindPoint, clip.customBoneName);
            }
            if (bindTrans == null)
            {
                bindTrans = context.OwnerTransform;
            }

            // 核心约束：十字闪光仅同步绑定点的世界坐标，不同步旋转
            Vector3 worldPos = bindTrans != null 
                ? bindTrans.position + clip.positionOffset 
                : clip.positionOffset;

            // 触发闪光，移交给 Handler 处理（闪光实例将由表现层直接绑定实体时钟）
            _handler.TriggerCrossFlash(worldPos, clip.parameters.FollowTarget ? bindTrans : null, clip.positionOffset, clip.parameters);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 闪光生命周期由表现层基于自身时长与实体时钟独立推进，此流程保持空
        }

        public override void OnExit()
        {
            // 核心约束：绝不在此处销毁或停止闪光。
            // 动作自然播完或中途被打断，闪光在表现层脱离时间轴继续按实体时钟有效流速播完，杜绝闪退穿帮。
        }

        public override void OnStop()
        {
            // 动作打断时同样不影响脱离时间轴的独立视觉闪光
        }
    }
}
