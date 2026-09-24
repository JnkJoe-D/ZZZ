using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(Image))]
    public class SkillEvadeBtnFill : MonoBehaviour
    {
        private Image _iconImage;
        private Material _matInstance;

        [Header("闪避冷却")]
        public float evadeTimer = 1f;

        private void Awake()
        {
            EnsureMaterial();
        }

        private void Start()
        {
            EnsureMaterial();
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
        private void OnEvadeStart()
        {
            if (_matInstance != null)
            {
                // 开始闪避时，将擦除量设置为 1，表示完全擦除
                _matInstance.SetFloat("_EraseAmount", 1f);
            }
        }
        private void UpdateShader()
        {

        }
    }
}
