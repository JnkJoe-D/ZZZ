using System;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// UI 导航栈
    /// 
    /// 职责：
    ///   记录面板跳转历史，支持 Back() 返回上一个面板
    ///   只有标记为导航节点的面板才会入栈
    /// </summary>
    public class UIStack
    {
        /// <summary>导航记录</summary>
        public struct NavigationRecord
        {
            public Type   ModuleType;
            public object Data;
        }

        private readonly Stack<NavigationRecord> _stack = new();

        /// <summary>栈中元素数量</summary>
        public int Count => _stack.Count;

        /// <summary>
        /// 将当前面板压入导航栈
        /// </summary>
        public void Push(Type moduleType, object data = null)
        {
            _stack.Push(new NavigationRecord
            {
                ModuleType = moduleType,
                Data       = data
            });
        }

        /// <summary>
        /// 弹出栈顶记录（用于 Back 返回）
        /// </summary>
        public NavigationRecord? Pop()
        {
            if (_stack.Count > 0)
                return _stack.Pop();
            return null;
        }

        /// <summary>
        /// 查看栈顶但不弹出
        /// </summary>
        public NavigationRecord? Peek()
        {
            if (_stack.Count > 0)
                return _stack.Peek();
            return null;
        }

        /// <summary>
        /// 清空导航栈
        /// </summary>
        public void Clear()
        {
            _stack.Clear();
        }
    }
}
