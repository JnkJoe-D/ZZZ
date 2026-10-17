using UnityEngine;
using Game.Framework;
using Game.GamePlay;
using Game.UI;

namespace Game.UI
{
    /// <summary>
    /// 怪物状态面板 Module（MVC 中的 Controller 角色）。
    /// 职责：
    ///   1. 建立 Model 与 View 的双向绑定；
    ///   2. 监听所绑定 MonsterEntity 的属性变化（HP, MaxHp, Daze, MaxDaze）；
    ///   3. 计算归一化比例并推送至 Model，单向驱动 View 呈现；
    ///   4. 协调 View 约束组件与怪物挂点、相机的装配与注销。
    /// </summary>
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/MonsterStatus/MonsterStatusPanel.prefab", Layer = UILayer.Background)]
    public class MonsterStatusPanelModule : UIModule<MonsterStatusPanelView, MonsterStatusPanelModel>
    {
        private MonsterEntity _targetMonster;
        public MonsterEntity TargetMonster => _targetMonster;

        protected override void OnCreate()
        {
            base.OnCreate();

            if (View != null)
            {
                View.Model = Model;
                Model.OnChanged += View.Refresh;
            }
        }

        protected override void OnRemove()
        {
            UnbindMonster();

            if (Model != null && View != null)
            {
                Model.OnChanged -= View.Refresh;
            }

            base.OnRemove();
        }

        /// <summary>
        /// 绑定目标怪物，建立属性监听与约束装配
        /// </summary>
        public void BindMonster(MonsterEntity monster)
        {
            if (monster == null) return;

            UnbindMonster();
            _targetMonster = monster;

            // 1. 订阅属性变更
            if (monster.StatusModule?.Attributes != null)
            {
                monster.StatusModule.Attributes.OnAttributeChanged -= OnMonsterAttributeChanged;
                monster.StatusModule.Attributes.OnAttributeChanged += OnMonsterAttributeChanged;
            }

            // 2. 装配约束
            Transform uiPoint = monster.transform.Find("UIPoint");
            if (uiPoint == null)
            {
                uiPoint = monster.transform;
            }

            Transform camTransform = GameCameraManager.Instance?.MainCameraTransform
                ?? (UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null);

            View?.BindConstraints(camTransform, uiPoint);

            // 3. 立即拉取并刷新一次当前数值
            RefreshFromMonster();
        }

        /// <summary>
        /// 解绑目标怪物，注销属性监听与约束
        /// </summary>
        public void UnbindMonster()
        {
            if (_targetMonster != null)
            {
                if (_targetMonster.StatusModule?.Attributes != null)
                {
                    _targetMonster.StatusModule.Attributes.OnAttributeChanged -= OnMonsterAttributeChanged;
                }
                _targetMonster = null;
            }

            View?.ClearConstraints();
            Model?.Reset();
        }

        private void OnMonsterAttributeChanged(AttributeId attrId, float oldValue, float newValue)
        {
            if (attrId == AttributeId.HP || attrId == AttributeId.MaxHp ||
                attrId == AttributeId.Daze || attrId == AttributeId.MaxDaze)
            {
                RefreshFromMonster();
            }
        }

        /// <summary>
        /// 从怪物状态属性集拉取实时数值，推导百分比并同步到 Model
        /// </summary>
        public void RefreshFromMonster()
        {
            if (_targetMonster == null) return;

            var attrs = _targetMonster.StatusModule?.Attributes;
            if (attrs == null) return;

            // 血量计算
            float maxHp = attrs.Has(AttributeId.MaxHp) ? attrs.GetCurrent(AttributeId.MaxHp) : 99999f;
            float hp = attrs.Has(AttributeId.HP) ? attrs.GetCurrent(AttributeId.HP) : maxHp;
            float hpPercent = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;

            // 失衡计算
            float maxDaze = attrs.Has(AttributeId.MaxDaze) ? attrs.GetCurrent(AttributeId.MaxDaze) : 100f;
            float daze = attrs.Has(AttributeId.Daze) ? attrs.GetCurrent(AttributeId.Daze) : 0f;
            float dazePercent = maxDaze > 0f ? Mathf.Clamp01(daze / maxDaze) : 0f;

            // 失衡数值直接显示纯数字整数
            string dazeText = $"{Mathf.RoundToInt(daze)}";
            Model.SetData(hpPercent, dazePercent, dazeText);
        }
    }
}
