using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 编辑器模式下的伤害检测预览。
    /// 由于编辑器预览环境不具备真实的怪物实体和数值组件，这里仅提供核心的时间轴触发日志提示，
    /// 帮助开发者确认伤害判定逻辑是否按期执行。
    /// </summary>
    [ProcessBinding(typeof(HitClip), PlayMode.EditorPreview)]
    public class EditorHitProcess : ProcessBase<HitClip>
    {
        private float lastCheckTime;

        public override void OnEnter()
        {
            if (clip.hitFrequency == HitFrequency.Once)
            {
                Debug.Log($"[SkillEditor Preview] <color=orange>Damage Triggered!</color> EventTag: {clip.eventTag}, Time: OnEner");
            }
            lastCheckTime = clip.StartTime;
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (clip.hitFrequency == HitFrequency.Interval)
            {
                if (currentTime - lastCheckTime >= clip.checkInterval)
                {
                    Debug.Log($"[SkillEditor Preview] <color=orange>Damage Triggered (Interval)!</color> EventTag: {clip.eventTag}, Time: {currentTime:F2}");
                    lastCheckTime = currentTime;
                }
            }
            else if (clip.hitFrequency == HitFrequency.Always)
            {
                // 可选打印，但频率太高容易刷屏，通常不提示或采用聚合提示
            }
        }
    }
}
