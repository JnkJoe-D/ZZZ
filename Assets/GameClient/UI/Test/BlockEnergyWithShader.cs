using UnityEngine;
using UnityEngine.UI;

public class BlockEnergyWithShader : MonoBehaviour
{
    private Image iconImage;
    private Material matInstance;

    [Header("能量设置")]
    public int maxEnergy = 6;
    [Range(0,6)]
    public int currentEnergy;

    void Start()
    {
        iconImage = GetComponent<Image>();

        // 关键：实例化材质球，防止修改影响到其他使用了同个材质的UI元素
        matInstance = new Material(iconImage.material);
        iconImage.material = matInstance;

        currentEnergy = maxEnergy;
        UpdateShader();
    }
    private void Update()
    {
        UpdateShader();
    }
    public void ConsumeEnergy(int amount = 1)
    {
        currentEnergy -= amount;
        if (currentEnergy < 0) currentEnergy = 0;
        UpdateShader();
    }

    public void RecoverEnergy(int amount = 1)
    {
        currentEnergy += amount;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
        UpdateShader();
    }

    private void UpdateShader()
    {
        if (matInstance != null)
        {
            // 计算需要"擦除"的比例。
            // 比如：满能量6点，擦除 (6-6)/6 = 0
            // 能量剩5点，擦除 (6-5)/6 = 1/6 (顺时针第一段消失)
            float eraseAmount = (float)(maxEnergy - currentEnergy) / maxEnergy;

            // 传递给Shader中的 _EraseAmount 变量
            matInstance.SetFloat("_EraseAmount", eraseAmount);
        }
    }
}