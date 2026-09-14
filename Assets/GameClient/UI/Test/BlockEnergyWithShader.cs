using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 切人按键外环支援点数 Shader 擦除圆弧控制微件 (挂载于 View/Keys/KeySwitch/Icons/IconKey)
    /// 受控表现微件：彻底移除 Update 轮询，由 SkillKeyView 显式驱动
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class BlockEnergyWithShader : MonoBehaviour
    {
        private Image _iconImage;
        private Material _matInstance;

        [Header("能量设置")]
        public int maxEnergy = 6;
        [Range(0, 6)]
        public int currentEnergy = 6;

        private void Awake()
        {
            EnsureMaterial();
        }

        private void Start()
        {
            EnsureMaterial();
            UpdateShader();
        }

        private void OnDestroy()
        {
            // 严格清理动态实例化的材质，杜绝内存泄漏
            if (_matInstance != null)
            {
                Destroy(_matInstance);
                _matInstance = null;
            }
        }

        /// <summary>
        /// 确保材质实例安全准备就绪（惰性初始化，防止外部调用先于 Start/Awake）
        /// </summary>
        private void EnsureMaterial()
        {
            if (_matInstance != null) return;

            if (_iconImage == null)
            {
                _iconImage = GetComponent<Image>();
            }

            if (_iconImage != null && _iconImage.material != null)
            {
                // 实例化材质球，防止修改影响到其他使用了同个材质的UI元素
                _matInstance = new Material(_iconImage.material);
                _iconImage.material = _matInstance;
            }
        }

        /// <summary>
        /// 外部受控入口：由 View 调用设置当前支援点数与最大支援点数
        /// </summary>
        public void SetEnergy(int current, int max = 6)
        {
            EnsureMaterial();

            maxEnergy = max > 0 ? max : 6;
            currentEnergy = Mathf.Clamp(current, 0, maxEnergy);

            UpdateShader();
        }

        public void ConsumeEnergy(int amount = 1)
        {
            SetEnergy(currentEnergy - amount, maxEnergy);
        }

        public void RecoverEnergy(int amount = 1)
        {
            SetEnergy(currentEnergy + amount, maxEnergy);
        }

        private void UpdateShader()
        {
            if (_matInstance != null)
            {
                // 计算需要"擦除"的比例。
                // 比如：满能量6点，擦除 (6-6)/6 = 0
                // 能量剩5点，擦除 (6-5)/6 = 1/6 (顺时针第一段消失)
                // 能量为0点，擦除 (6-0)/6 = 1 (全部消失)
                float eraseAmount = (float)(maxEnergy - currentEnergy) / maxEnergy;

                // 传递给Shader中的 _EraseAmount 变量
                _matInstance.SetFloat("_EraseAmount", eraseAmount);
            }
        }
    }
}