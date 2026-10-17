using UnityEngine;
using Game.UI;

namespace Game.UI
{
    /// <summary>
    /// 怪物状态面板 UI 数据模型（纯数值状态快照）。
    /// 承载 HP、失衡百分比与失衡值文本，作为单向数据源供 View 订阅渲染。
    /// </summary>
    public class MonsterStatusPanelModel : UIModel
    {
        private float _hpPercent = 1f;
        public float HpPercent
        {
            get => _hpPercent;
            set
            {
                if (!Mathf.Approximately(_hpPercent, value))
                {
                    _hpPercent = Mathf.Clamp01(value);
                    NotifyChanged();
                }
            }
        }

        private float _dazePercent = 0f;
        public float DazePercent
        {
            get => _dazePercent;
            set
            {
                if (!Mathf.Approximately(_dazePercent, value))
                {
                    _dazePercent = Mathf.Clamp01(value);
                    NotifyChanged();
                }
            }
        }

        private string _dazeText = "0";
        public string DazeText
        {
            get => _dazeText;
            set
            {
                if (_dazeText != value)
                {
                    _dazeText = value;
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 批量更新状态数据，合并单次 NotifyChanged 触发
        /// </summary>
        public void SetData(float hpPercent, float dazePercent, string dazeText)
        {
            _hpPercent = Mathf.Clamp01(hpPercent);
            _dazePercent = Mathf.Clamp01(dazePercent);
            _dazeText = dazeText ?? "0";
            NotifyChanged();
        }

        public override void Reset()
        {
            _hpPercent = 1f;
            _dazePercent = 0f;
            _dazeText = "0";
            base.Reset();
        }
    }
}
