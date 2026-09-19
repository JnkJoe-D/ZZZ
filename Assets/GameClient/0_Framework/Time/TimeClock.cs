using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 层次化时钟树通用节点模型（Composite Pattern）。
    /// 支持局部流速 (LocalScale) 与整条祖先链路级联相乘的最终有效流速 (EffectiveScale)。
    /// 变动时自顶向下链式渗透，仅在 EffectiveScale 真正产生浮点变化时派发单次通知。
    /// </summary>
    public class TimeClock
    {
        public string Name { get; }
        public TimeClock Parent { get; private set; }

        private readonly List<TimeClock> _children = new List<TimeClock>(4);
        public IReadOnlyList<TimeClock> Children => _children;

        private float _localScale = 1.0f;
        private float _effectiveScale = 1.0f;

        /// <summary>当前时钟节点私有的局部流速倍率 (如顿帧设为 0，子弹时间设为 0.1)</summary>
        public float LocalScale
        {
            get => _localScale;
            set
            {
                float clamped = Mathf.Max(0f, value);
                if (!Mathf.Approximately(_localScale, clamped))
                {
                    _localScale = clamped;
                    RecalculateEffectiveScale();
                }
            }
        }

        /// <summary>自顶向下整条祖先链路级联相乘后的全局最终生效流速（单一真理源）</summary>
        public float EffectiveScale => _effectiveScale;

        /// <summary>当前时钟累计流逝的有效时间（秒）</summary>
        public float Time { get; private set; }

        /// <summary>当前时钟在当前步长的有效增量时间（秒）</summary>
        public float DeltaTime { get; private set; }

        /// <summary>最终生效流速变更事件（仅当 EffectiveScale 实际改变时派发）</summary>
        public event Action<float> OnEffectiveScaleChanged;

        public TimeClock(string name, TimeClock parent = null)
        {
            Name = name ?? "Clock";
            if (parent != null)
            {
                parent.AddChild(this);
            }
            else
            {
                _effectiveScale = _localScale;
            }
        }

        /// <summary>
        /// 添加子时钟节点。若子节点已有父节点，会自动脱离原父节点。
        /// </summary>
        public void AddChild(TimeClock child)
        {
            if (child == null || child == this || _children.Contains(child)) return;

            if (child.Parent != null && child.Parent != this)
            {
                child.Parent.RemoveChild(child);
            }

            child.Parent = this;
            _children.Add(child);
            child.RecalculateEffectiveScale();
        }

        /// <summary>
        /// 移除子时钟节点。
        /// </summary>
        public void RemoveChild(TimeClock child)
        {
            if (child == null || !_children.Remove(child)) return;

            child.Parent = null;
            child.RecalculateEffectiveScale();
        }

        /// <summary>
        /// 安全脱离当前父时钟节点，并清理自身的子节点引用
        /// </summary>
        public void Detach()
        {
            Parent?.RemoveChild(this);
            _children.Clear();
            OnEffectiveScaleChanged = null;
        }

        /// <summary>
        /// 级联重新计算本节点的 EffectiveScale，并在发生数值变化时向下递归通知所有子节点
        /// </summary>
        internal void RecalculateEffectiveScale()
        {
            float parentEffective = Parent != null ? Parent.EffectiveScale : 1.0f;
            float newEffective = _localScale * parentEffective;

            if (!Mathf.Approximately(_effectiveScale, newEffective))
            {
                _effectiveScale = newEffective;
                OnEffectiveScaleChanged?.Invoke(_effectiveScale);

                // 递归向下级联刷新子孙节点
                for (int i = 0; i < _children.Count; i++)
                {
                    _children[i].RecalculateEffectiveScale();
                }
            }
        }

        /// <summary>
        /// 推进该时钟的时间累加（由主时钟驱动递归更新）
        /// </summary>
        public void Advance(float parentDeltaTime)
        {
            DeltaTime = parentDeltaTime * _localScale;
            Time += DeltaTime;

            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Advance(DeltaTime);
            }
        }
    }
}
