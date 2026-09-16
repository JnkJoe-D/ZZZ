using System;

namespace ATEditor
{
    /// <summary>
    /// 标注 Process 类与 ClipBase 子类型 + PlayMode 的绑定关系
    /// ProcessFactory 通过反射扫描此特性自动注册
    /// 支持 AllowMultiple：同一个 Process 类可绑定多个 (ClipType, PlayMode) 组合
    /// </summary>
    /// <example>
    /// [ProcessBinding(typeof(SkillAnimationClip), PlayMode.EditorPreview)]
    /// public class EditorAnimationProcess : ProcessBase&lt;SkillAnimationClip&gt; { }
    ///
    /// [ProcessBinding(typeof(DamageClip), PlayMode.EditorPreview)]
    /// [ProcessBinding(typeof(DamageClip), PlayMode.Runtime)]
    /// public class DamageProcess : ProcessBase&lt;DamageClip&gt; { }
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ProcessBindingAttribute : Attribute
    {
        /// <summary>
        /// 绑定的 ClipBase 子类型
        /// </summary>
        public Type ClipType { get; }

        /// <summary>
        /// 绑定的播放模式
        /// </summary>
        public PlayMode Mode { get; }

        public ProcessBindingAttribute(Type clipType, PlayMode mode)
        {
            ClipType = clipType;
            Mode = mode;
        }
    }
}
