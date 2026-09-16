using System;

namespace Game.Framework
{
    /// <summary>
    /// 非泛型的可清理接口
    /// 用于 GlobalPoolManager 在不知道泛型参数的情况下调用 Clear()
    /// </summary>
    public interface IClearable
    {
        /// <summary>
        /// 清空池中所有缓存的空闲对象
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 通用对象池接口
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    public interface IPool<T> : IClearable, IDisposable
    {
        /// <summary>
        /// 从池中获取一个对象
        /// </summary>
        T Get();

        /// <summary>
        /// 归还对象到池中
        /// </summary>
        void Return(T item);

        /// <summary>
        /// 当前池中空闲对象数量
        /// </summary>
        int CountInactive { get; }

        /// <summary>
        /// 当前已借出的活跃对象数量
        /// </summary>
        int CountActive { get; }

        /// <summary>
        /// 池中对象总数（空闲 + 活跃）
        /// </summary>
        int CountAll { get; }
    }
}
