using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 片段基类 (Non-Generic Wrapper for serialization)
    /// </summary>
    [Serializable]
    public abstract class ClipBase : IClipData
    {
        [HideInInspector]
        public string clipId = Guid.NewGuid().ToString();
        [ActionProperty("片段名称")]
        public string clipName = "Clip";
        [ActionProperty("启用")]
        public bool isEnabled = true;

        [SerializeField] protected float startTime;
        [SerializeField] protected float duration = 1.0f;
        [SerializeField] protected float blendInDuration;
        [SerializeField] protected float blendOutDuration;

        [ActionProperty("开始时间")]
        public virtual float StartTime
        {
            get { return startTime; }
            set
            {
                if (!Application.isPlaying)
                {
                    startTime = Mathf.Max(0,value);
                }
            }
        }
        [ActionProperty("持续时间")]
        public virtual float Duration
        {
            get{return duration;}
            set
            {
                if(!Application.isPlaying)
                {
                    duration=Mathf.Max(value,0.01f);
                }
            }
        }
        [ActionProperty("渐入时间")]
        public virtual float BlendInDuration
        {
            get{return blendInDuration;}
            set
            {
                if (!Application.isPlaying)
                {
                    blendInDuration = Mathf.Clamp(value, 0,duration-blendOutDuration);
                }
            }
        }
        [ActionProperty("渐出时间")]
        public virtual float BlendOutDuration
        {
            get { return blendOutDuration; }
            set
            {
                if (!Application.isPlaying)
                {
                    blendOutDuration = Mathf.Clamp(value, 0,duration - blendInDuration);
                }
            }
        }
        public float EndTime => startTime + duration;

        public virtual bool SupportsBlending => false;

        public abstract ClipBase Clone();
    }
}
