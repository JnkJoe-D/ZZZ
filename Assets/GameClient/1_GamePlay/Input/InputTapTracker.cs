using System;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 定长无 GC 环形时间戳队列 (容量固定为 8)
    /// 专用于记录单按键最近 8 次被按下的真实时间戳 (Time.unscaledTime)
    /// </summary>
    public struct TapTimestampRingBuffer
    {
        private const int Capacity = 8;
        private float _t0, _t1, _t2, _t3, _t4, _t5, _t6, _t7;
        private int _headIndex; // 下一次写入的槽位 [0, Capacity - 1]
        private int _count;     // 当前累计有效记录数 [0, Capacity]

        /// <summary>
        /// 记录一次按键敲击时间戳
        /// </summary>
        public void Record(float timestamp)
        {
            SetTimestamp(_headIndex, timestamp);
            _headIndex = (_headIndex + 1) % Capacity;
            if (_count < Capacity)
            {
                _count++;
            }
        }

        /// <summary>
        /// 清空所有历史记录
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _headIndex = 0;
            _t0 = _t1 = _t2 = _t3 = _t4 = _t5 = _t6 = _t7 = 0f;
        }

        /// <summary>
        /// 统计自 minTimestamp 以来被记录的点击次数
        /// </summary>
        public int CountSince(float minTimestamp)
        {
            int taps = 0;
            for (int i = 0; i < _count; i++)
            {
                // 从最近写入的槽位往前倒推查找
                int idx = (_headIndex - 1 - i + Capacity) % Capacity;
                float time = GetTimestamp(idx);
                if (time >= minTimestamp)
                {
                    taps++;
                }
                else
                {
                    // 时间单调递减，一旦小于阈值可直接提前退出
                    break;
                }
            }
            return taps;
        }

        private float GetTimestamp(int index)
        {
            switch (index)
            {
                case 0: return _t0;
                case 1: return _t1;
                case 2: return _t2;
                case 3: return _t3;
                case 4: return _t4;
                case 5: return _t5;
                case 6: return _t6;
                case 7: return _t7;
                default: return 0f;
            }
        }

        private void SetTimestamp(int index, float value)
        {
            switch (index)
            {
                case 0: _t0 = value; break;
                case 1: _t1 = value; break;
                case 2: _t2 = value; break;
                case 3: _t3 = value; break;
                case 4: _t4 = value; break;
                case 5: _t5 = value; break;
                case 6: _t6 = value; break;
                case 7: _t7 = value; break;
            }
        }
    }

    /// <summary>
    /// 纯 C# 时域按键敲击跟踪器
    /// 宿主于 InputProvider，专职记录硬件输入的离散脉冲时序，提供连续点击/双击/频次查询
    /// 严格使用物理真实时间 (Time.unscaledTime)，确保卡肉顿帧与子弹时间下手感不失真
    /// </summary>
    public sealed class InputTapTracker
    {
        private const int SlotCount = 8;
        private readonly TapTimestampRingBuffer[] _buffers = new TapTimestampRingBuffer[SlotCount];

        /// <summary>
        /// 映射 HardwareInputType 到紧凑槽位下标
        /// </summary>
        private static int GetKeySlot(HardwareInputType key)
        {
            switch (key)
            {
                case HardwareInputType.None: return 0;
                case HardwareInputType.Move: return 1;
                case HardwareInputType.BasicAttack: return 2;
                case HardwareInputType.SpecialAttack: return 3;
                case HardwareInputType.Ultimate: return 4;
                case HardwareInputType.Evade: return 5;
                case HardwareInputType.Switch: return 6;
                case HardwareInputType.Interact: return 7;
                default: return -1;
            }
        }

        /// <summary>
        /// 当硬件按键按下瞬间触发记录
        /// </summary>
        public void RecordTap(HardwareInputType key, float currentTime)
        {
            int slot = GetKeySlot(key);
            if (slot >= 0 && slot < SlotCount)
            {
                _buffers[slot].Record(currentTime);
            }
        }

        /// <summary>
        /// 获取指定按键在最近 windowSeconds 秒内的敲击次数 (滑动时间窗口)
        /// </summary>
        public int GetTapCountInWindow(HardwareInputType key, float windowSeconds, float currentTime)
        {
            int slot = GetKeySlot(key);
            if (slot >= 0 && slot < SlotCount)
            {
                float minTime = currentTime - windowSeconds;
                return _buffers[slot].CountSince(minTime);
            }
            return 0;
        }

        /// <summary>
        /// 获取自某一绝对时间戳以来（如当前动作开始、或当前窗口开始）的敲击次数
        /// </summary>
        public int GetTapCountSince(HardwareInputType key, float sinceTime)
        {
            int slot = GetKeySlot(key);
            if (slot >= 0 && slot < SlotCount)
            {
                return _buffers[slot].CountSince(sinceTime);
            }
            return 0;
        }

        /// <summary>
        /// 清理指定按键历史 (通常用于切人、受击打断或强制重置)
        /// </summary>
        public void Clear(HardwareInputType key)
        {
            int slot = GetKeySlot(key);
            if (slot >= 0 && slot < SlotCount)
            {
                _buffers[slot].Clear();
            }
        }

        /// <summary>
        /// 清理所有按键历史
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _buffers[i].Clear();
            }
        }
    }
}
