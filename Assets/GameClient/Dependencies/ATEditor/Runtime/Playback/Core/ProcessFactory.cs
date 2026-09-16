using System;
using System.Collections.Generic;
using System.Reflection;

namespace ATEditor
{
    /// <summary>
    /// Process 工厂：反射自动发现 [ProcessBinding] + 对象池
    /// 新增 ClipType/Process 时无需修改此类，只需标注特性即可
    /// </summary>
    public static class ProcessFactory
    {
        // 注册表：(ClipType, PlayMode) → ProcessType
        private static Dictionary<(Type, PlayMode), Type> registry;

        // 对象池：按 Process 类型分池
        private static Dictionary<Type, Queue<IProcess>> pools
            = new Dictionary<Type, Queue<IProcess>>();

        // 是否已初始化
        private static bool initialized = false;

        /// <summary>
        /// 初始化注册表（首次调用 Create 时自动触发，也可手动调用）
        /// </summary>
        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            registry = new Dictionary<(Type, PlayMode), Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 跳过系统程序集以提升扫描速度
                var asmName = asm.GetName().Name;
                if (asmName.StartsWith("System") || asmName.StartsWith("Unity") ||
                    asmName.StartsWith("mscorlib") || asmName.StartsWith("Mono"))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(IProcess).IsAssignableFrom(type)) continue;

                    foreach (var attr in type.GetCustomAttributes<ProcessBindingAttribute>())
                    {
                        var key = (attr.ClipType, attr.Mode);
                        registry[key] = type;
                    }
                }
            }
        }

        /// <summary>
        /// 获取 Process 实例（优先从池取）
        /// </summary>
        /// <param name="clip">片段数据</param>
        /// <param name="mode">播放模式</param>
        /// <returns>对应的 Process 实例，如无注册则返回 null</returns>
        public static IProcess Create(ClipBase clip, PlayMode mode)
        {
            if (!initialized) Initialize();

            var key = (clip.GetType(), mode);
            if (!registry.TryGetValue(key, out var processType))
            {
                return null;
            }

            // 尝试从池中取
            if (pools.TryGetValue(processType, out var pool) && pool.Count > 0)
            {
                var reused = pool.Dequeue();
                reused.Reset();
                return reused;
            }

            return (IProcess)Activator.CreateInstance(processType);
        }

        /// <summary>
        /// 归还 Process 到对象池
        /// </summary>
        public static void Return(IProcess process)
        {
            if (process == null) return;

            var type = process.GetType();
            if (!pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<IProcess>();
                pools[type] = pool;
            }
            pool.Enqueue(process);
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public static void ClearPools()
        {
            pools.Clear();
        }

        /// <summary>
        /// 重置工厂（清空注册表和对象池，下次 Create 时重新扫描）
        /// </summary>
        public static void Reset()
        {
            registry = null;
            pools.Clear();
            initialized = false;
        }
    }
}
