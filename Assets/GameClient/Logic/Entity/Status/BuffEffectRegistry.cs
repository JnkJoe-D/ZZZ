using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using cfg.ZZZ;

namespace Game.Logic
{
    /// <summary>
    /// Buff 效果执行器构造委托
    /// </summary>
    public delegate IBuffEffect BuffEffectCreator(BuffEffectData data, BuffApplyContext ctx);

    /// <summary>
    /// 高性能去中心化 Buff 效果注册表。
    /// 彻底消灭 switch-case 巨石工厂，实现 100% 开闭原则 (OCP)。
    /// 启动期一次性完成特性扫描并缓存高效委托，运行时极速 O(1) 查找，零反射，零堆内存分配。
    /// </summary>
    public static class BuffEffectRegistry
    {
        private static readonly Dictionary<BuffEffectType, BuffEffectCreator> _creators = new(64);
        private static bool _initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Initialize()
        {
            if (_initialized) return;
            _creators.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                // 仅扫描游戏逻辑相关程序集
                var asmName = assembly.GetName().Name;
                if (!asmName.StartsWith("Assembly-CSharp") && !asmName.StartsWith("Game."))
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null) continue;

                foreach (var type in types)
                {
                    if (type == null || !typeof(IBuffEffect).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                        continue;

                    var attr = type.GetCustomAttribute<BuffEffectBindingAttribute>();
                    if (attr == null) continue;

                    // 优先匹配 (BuffEffectData, BuffApplyContext)，次选 (BuffEffectData)
                    var ctor = type.GetConstructor(new[] { typeof(BuffEffectData), typeof(BuffApplyContext) })
                            ?? type.GetConstructor(new[] { typeof(BuffEffectData) });

                    if (ctor != null)
                    {
                        bool hasCtx = ctor.GetParameters().Length == 2;
                        _creators[attr.EffectType] = (data, ctx) =>
                        {
                            return hasCtx
                                ? (IBuffEffect)ctor.Invoke(new object[] { data, ctx })
                                : (IBuffEffect)ctor.Invoke(new object[] { data });
                        };
                    }
                    else
                    {
                        Debug.LogError($"[Buff] 执行器 {type.Name} 缺少标准构造函数 (BuffEffectData[, BuffApplyContext])！");
                    }
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// 手动注册（供单元测试、AOT 显式预热或特种扩展使用）
        /// </summary>
        public static void Register(BuffEffectType type, BuffEffectCreator creator)
        {
            if (creator != null)
            {
                _creators[type] = creator;
            }
        }

        /// <summary>
        /// O(1) 极速根据 Luban 配置数据创建对应执行器
        /// </summary>
        public static IBuffEffect Create(BuffEffectData data, BuffApplyContext ctx)
        {
            if (!_initialized) Initialize();

            if (data == null) return null;

            if (_creators.TryGetValue(data.EffectType, out var creator))
            {
                return creator(data, ctx);
            }

            Debug.LogWarning($"[Buff] 未找到效果类型为 {data.EffectType} 的 BuffEffect 执行器绑定。请确认对应执行器已添加 [BuffEffectBinding] 特性。");
            return null;
        }

        /// <summary>
        /// 批量创建效果执行器列表
        /// </summary>
        public static List<IBuffEffect> CreateEffects(List<BuffEffectData> effectDatas, BuffApplyContext ctx)
        {
            if (effectDatas == null || effectDatas.Count == 0) return new List<IBuffEffect>(0);

            var list = new List<IBuffEffect>(effectDatas.Count);
            for (int i = 0; i < effectDatas.Count; i++)
            {
                var effect = Create(effectDatas[i], ctx);
                if (effect != null) list.Add(effect);
            }
            return list;
        }
    }
}
