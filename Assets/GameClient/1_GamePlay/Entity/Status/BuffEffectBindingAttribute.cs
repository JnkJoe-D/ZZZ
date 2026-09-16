using System;
using cfg.ZZZ;

namespace Game.GamePlay
{
    /// <summary>
    /// Buff 效果执行器绑定特性。
    /// 标注于 IBuffEffect 实现类上，声明该类负责驱动哪个 Luban 的 BuffEffectType。
    /// 遵循开闭原则 (OCP)：新增 Buff 效果类时只需打上此特性，无需修改任何中央工厂或注册代码。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class BuffEffectBindingAttribute : Attribute
    {
        public BuffEffectType EffectType { get; }

        public BuffEffectBindingAttribute(BuffEffectType effectType)
        {
            EffectType = effectType;
        }
    }
}
