using Game.Framework;
using Game.Logic;
using Game.UI;
using UnityEngine;

namespace Game.UI.Modules.RoleStatus
{
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/RoleStatus/StatusPanel.prefab", Layer = UILayer.Window)]
    public class StatusPanelModule : UIModule<StatusPanelView, StatusPanelModel>
    {
        private Coroutine _hpDelayCoroutine;

        protected override void OnCreate()
        {
            base.OnCreate();

            // 1. 实例化独立的材质，防止修改 _FillAmount 污染整个资源库，也避免多角色同屏时干扰
            if (View.HpFill != null && View.HpFill.material != null)
                View.HpFill.material = new Material(View.HpFill.material);
                
            if (View.HpDelay != null && View.HpDelay.material != null)
                View.HpDelay.material = new Material(View.HpDelay.material);
                
            if (View.EnergyFill != null && View.EnergyFill.material != null)
                View.EnergyFill.material = new Material(View.EnergyFill.material);

            // 2. 订阅底层属性数值变化事件 (HP/MP)
            EventCenter.Subscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);

            // 3. 界面刚创建时主动拉取一次当前状态
            RefreshAllStatus();
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            EventCenter.Unsubscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);
            
            if (_hpDelayCoroutine != null && View != null)
            {
                View.StopCoroutine(_hpDelayCoroutine);
                _hpDelayCoroutine = null;
            }
        }

        private void RefreshAllStatus()
        {
            var localPlayer = CharcterManager.Instance?.LocalCharacter;
            if (localPlayer != null && localPlayer.StatusModule != null)
            {
                float hpPercent = localPlayer.StatusModule.Attributes.GetPercent(AttributeId.HP);
                float energyPercent = localPlayer.StatusModule.Attributes.GetPercent(AttributeId.Energy);

                SetHpFill(hpPercent, hpPercent, true);
                SetEnergyFill(energyPercent);
            }
        }

        private void OnPlayerStatChanged(PlayerStatChangedEvent evt)
        {
            var localPlayer = CharcterManager.Instance?.LocalCharacter;
            if (localPlayer == null || evt.PlayerId != localPlayer.GetInstanceID())
                return;

            // StatType.HP 对应 AttributeId.HP
            if (evt.StatType == StatType.HP)
            {
                float percent = evt.MaxValue > 0 ? evt.NewValue / evt.MaxValue : 0;
                float oldPercent = evt.MaxValue > 0 ? evt.OldValue / evt.MaxValue : percent;
                SetHpFill(percent, oldPercent, false);
            }
            // StatType.MP 对应 AttributeId.Energy (框架底层的默认映射)
            else if (evt.StatType == StatType.MP)
            {
                float percent = evt.MaxValue > 0 ? evt.NewValue / evt.MaxValue : 0;
                SetEnergyFill(percent);
            }
        }

        private void SetHpFill(float newPercent, float oldPercent, bool instant)
        {
            if (View.HpFill == null || View.HpFill.material == null) return;

            // 1. 主血条永远是瞬间到位（绝区零风格，主绿条瞬间掉下去）
            View.HpFill.material.SetFloat("_FillAmount", newPercent);

            // 2. 缓冲条效果处理（红条）
            if (View.HpDelay == null || View.HpDelay.material == null) return;

            if (instant || newPercent >= oldPercent) 
            {
                // 回血或瞬间刷新时，缓冲条直接跟上，不表现延迟
                if (_hpDelayCoroutine != null && View != null)
                {
                    View.StopCoroutine(_hpDelayCoroutine);
                    _hpDelayCoroutine = null;
                }
                View.HpDelay.material.SetFloat("_FillAmount", newPercent);
            }
            else 
            {
                // 扣血时，缓冲条延迟跟上
                if (_hpDelayCoroutine != null && View != null)
                {
                    View.StopCoroutine(_hpDelayCoroutine);
                }
                
                // 让缓冲条先停留在扣血前的位置
                View.HpDelay.material.SetFloat("_FillAmount", oldPercent);
                
                // 启动协程做延迟回缩
                if (View != null)
                {
                    _hpDelayCoroutine = View.StartCoroutine(HpDelayRoutine(oldPercent, newPercent));
                }
            }
        }

        private System.Collections.IEnumerator HpDelayRoutine(float startPercent, float targetPercent)
        {
            // 停顿 0.3 秒
            yield return new WaitForSeconds(0.3f);
            
            // 花 0.5 秒平滑缩回
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 简单的平滑过渡 (类似 Ease.OutQuad，先快后慢)
                float smoothT = 1f - (1f - t) * (1f - t); 
                
                float current = Mathf.Lerp(startPercent, targetPercent, smoothT);
                
                if (View != null && View.HpDelay != null && View.HpDelay.material != null)
                {
                    View.HpDelay.material.SetFloat("_FillAmount", current);
                }
                yield return null;
            }
            
            if (View != null && View.HpDelay != null && View.HpDelay.material != null)
            {
                View.HpDelay.material.SetFloat("_FillAmount", targetPercent);
            }
            
            _hpDelayCoroutine = null;
        }

        private void SetEnergyFill(float percent)
        {
            if (View.EnergyFill == null || View.EnergyFill.material == null) return;

            View.EnergyFill.material.SetFloat("_FillAmount", percent);
            
            // 根据能量是否满，控制 Shader 的闪烁开关
            if (percent >= 1f)
            {
                View.EnergyFill.material.SetFloat("_UseFlash", 1f);
            }
            else
            {
                View.EnergyFill.material.SetFloat("_UseFlash", 0f);
            }
        }
    }
}
