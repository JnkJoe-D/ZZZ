using System.Collections;
using UnityEngine;
namespace  Game.MAnimSystem
{
/// <summary>
/// SimpleAnimancer 系统的功能测试脚本。
/// 展示了如何播放 Clip、如何监听事件以及如何使用 1D/2D 混合器。
/// 包含频繁切换测试和缓存验证。
/// 包含多层混合测试：AvatarMask、Additive、层淡入淡出。
/// </summary>
public class Test1 : MonoBehaviour
{
    [Tooltip("animComponent 组件引用")]
    public AnimComponent animComponent;
    
    [Header("基础动画片段 (Clip)")]
    public AnimationClip idleClip;
    public AnimationClip attackClip;
    public AnimationClip walkClip;
    public AnimationClip runClip;

    [Header("2D 混合资源 (Movement)")]
    public AnimationClip idle2D;
    public AnimationClip walkFwd;
    public AnimationClip walkRight;

    [Header("多层混合测试资源")]
    [Tooltip("上半身动画（攻击、挥手等）")]
    public AnimationClip upperBodyClip;
    [Tooltip("叠加动画（呼吸等）")]
    public AnimationClip breatheClip;
    [Tooltip("上半身遮罩")]
    public AvatarMask upperBodyMask;

    // 运行时创建的混合器状态引用

    // 测试状态
    private bool _isRapidSwitchTesting;

    void Start()
    {
        // 实际开发中，建议在 Start 中预先构建好需要的 State，或者使用对象池管理
    }

    void Update()
    {
        // 1. 基础播放与事件演示
        // 按 Q 播放待机动画 (0.2s 过渡)
        if (UnityEngine.Input.GetKeyDown(KeyCode.Q))
        {
            animComponent.Play(idleClip, 0.2f);
        }

        // 按 E 播放攻击动画，并演示事件监听
        if (UnityEngine.Input.GetKeyDown(KeyCode.E))
        {
            // 播放攻击，0.1s 快切
            var state = animComponent.Play(attackClip, 0.1f);
            
            // 订阅过渡完成事件 (淡入完毕)
            state.OnFadeComplete = (_) => Debug.Log("[事件] 攻击动作：过渡淡入完成!");
            
            // 订阅播放结束事件 (动画播完)
            state.OnEnd = (_) => 
            {
                Debug.Log("[事件] 攻击动作：播放结束! 自动切回 Idle。");
                animComponent.Play(idleClip, 0.2f);
            };
        }

        
        // 4. 频繁切换测试 - 验证中断列表法
        // 按 F 开始频繁切换测试
        if (UnityEngine.Input.GetKeyDown(KeyCode.F) && !_isRapidSwitchTesting)
        {
            StartCoroutine(RapidSwitchTest());
        }

        // 5. 缓存验证测试
        // 按 G 验证状态缓存
        if (UnityEngine.Input.GetKeyDown(KeyCode.G))
        {
            TestStateCache();
        }

        // 6. 时间归一化 API 测试
        // 按 H 测试归一化时间
        if (UnityEngine.Input.GetKeyDown(KeyCode.H))
        {
            TestNormalizedTime();
        }

        // 7. 暂停/恢复测试
        // 按 P 暂停/恢复当前动画
        if (UnityEngine.Input.GetKeyDown(KeyCode.P))
        {
            TestPauseResume();
        }

        // ========== 多层混合测试 ==========

        // 9. 上半身层测试（带 AvatarMask）
        // 按 U 在上半身层播放动画
        if (UnityEngine.Input.GetKeyDown(KeyCode.U))
        {
            TestUpperBodyLayer();
        }

        // 10. 叠加层测试（Additive）
        // 按 I 播放叠加动画
        if (UnityEngine.Input.GetKeyDown(KeyCode.I))
        {
            TestAdditiveLayer();
        }

        // 11. 层淡入淡出测试
        // 按 O 测试层淡入淡出
        if (UnityEngine.Input.GetKeyDown(KeyCode.O))
        {
            TestLayerFade();
        }

        // 12. 动态创建层测试
        // 按 L 测试动态创建层
        if (UnityEngine.Input.GetKeyDown(KeyCode.L))
        {
            TestDynamicLayerCreation();
        }

        // 13. 多层同时播放测试
        // 按 M 测试多层同时播放
        if (UnityEngine.Input.GetKeyDown(KeyCode.M))
        {
            TestMultipleLayers();
        }
    }

    /// <summary>
    /// 频繁切换测试。
    /// 验证中断列表法是否正确处理多状态过渡。
    /// </summary>
    IEnumerator RapidSwitchTest()
    {
        _isRapidSwitchTesting = true;
        Debug.Log("[频繁切换测试] 开始 - 验证中断列表法");
        
        var clips = new[] { idleClip, attackClip, walkClip, runClip };
        int switchCount = 20;
        
        for (int i = 0; i < switchCount; i++)
        {
            var clip = clips[i % clips.Length];
            animComponent.Play(clip, 0.25f);
            Debug.Log($"[频繁切换] 第 {i + 1} 次切换: {clip.name}");
            
            // 50ms 切换一次，远小于过渡时间 250ms
            yield return new WaitForSeconds(0.05f);
        }
        
        Debug.Log("[频繁切换测试] 完成 - 所有状态应正确过渡，无权重卡住");
        _isRapidSwitchTesting = false;
    }

    /// <summary>
    /// 状态缓存验证测试。
    /// 验证同一 AnimationClip 返回相同的 ClipState 实例。
    /// </summary>
    void TestStateCache()
    {
        Debug.Log("[缓存验证] 开始测试");
        
        var state1 = animComponent.Play(idleClip);
        var state2 = animComponent.Play(attackClip);
        var state3 = animComponent.Play(idleClip);  // 应该返回缓存的 state1
        
        bool cacheWorking = (state1 == state3);
        Debug.Log($"[缓存验证] state1 == state3 ? {cacheWorking}");
        
        if (cacheWorking)
        {
            Debug.Log("[缓存验证] ✅ 成功 - 同一 Clip 返回相同实例");
        }
        else
        {
            Debug.Log("[缓存验证] ❌ 失败 - 缓存机制未生效");
        }
    }

    /// <summary>
    /// 归一化时间 API 测试。
    /// </summary>
    void TestNormalizedTime()
    {
        var state = animComponent.Play(attackClip);
        
        Debug.Log("[归一化时间] 测试开始");
        Debug.Log($"  动画长度: {state.Length:F2}s");
        
        // 设置归一化时间到 50%
        state.NormalizedTime = 0.5f;
        Debug.Log($"  设置 NormalizedTime = 0.5");
        Debug.Log($"  实际 Time: {state.Time:F2}s");
        Debug.Log($"  实际 NormalizedTime: {state.NormalizedTime:F2}");
        
        Debug.Log("[归一化时间] ✅ API 正常工作");
    }

    /// <summary>
    /// 暂停/恢复测试。
    /// </summary>
    void TestPauseResume()
    {
        var state = animComponent.Play(idleClip);
        
        if (state.IsPaused)
        {
            state.Resume();
            Debug.Log("[暂停控制] 已恢复播放");
        }
        else
        {
            state.Pause();
            Debug.Log("[暂停控制] 已暂停播放 - 再按 P 恢复");
        }
    }


    // ========== 多层混合测试方法 ==========

    /// <summary>
    /// 上半身层测试（带 AvatarMask）。
    /// 演示如何在 Layer 1 播放上本身动画，同时保持下半身动画。
    /// </summary>
    void TestUpperBodyLayer()
    {
        Debug.Log("[上半身层] 开始测试");

        // 在基础层播放行走动画
        animComponent.Play(walkClip, 0);

        // 获取或创建 Layer 1
        var upperLayer = animComponent[1];

        // 设置上半身遮罩
        if (upperBodyMask != null)
        {
            upperLayer.Mask = upperBodyMask;
            Debug.Log("[上半身层] 已设置 AvatarMask");
        }
        else
        {
            Debug.LogWarning("[上半身层] 未配置 upperBodyMask，请在 Inspector 中设置");
        }

        // 在上半身层播放动画
        if (upperBodyClip != null)
        {
            upperLayer.Play(upperBodyClip, 0.25f);
            Debug.Log("[上半身层] 正在播放上半身动画，下半身保持行走");
        }
        else
        {
            Debug.LogWarning("[上半身层] 未配置 upperBodyClip，请在 Inspector 中设置");
        }
    }

    /// <summary>
    /// 叠加层测试（Additive）。
    /// 演示如何使用 Additive 模式叠加动画。
    /// </summary>
    void TestAdditiveLayer()
    {
        Debug.Log("[叠加层] 开始测试");

        // 在基础层播放动画
        animComponent.Play(idleClip, 0);

        // 获取或创建 Layer 2
        var additiveLayer = animComponent[2];

        // 设置为叠加模式
        additiveLayer.IsAdditive = true;
        Debug.Log("[叠加层] 已设置为 Additive 模式");

        // 播放叠加动画
        if (breatheClip != null)
        {
            additiveLayer.Play(breatheClip, 0.25f);
            Debug.Log("[叠加层] 正在播放叠加动画（呼吸）");
        }
        else
        {
            Debug.LogWarning("[叠加层] 未配置 breatheClip，请在 Inspector 中设置");
        }
    }

    /// <summary>
    /// 层淡入淡出测试。
    /// 演示如何使用 StartFade 淡入淡出整个层。
    /// </summary>
    void TestLayerFade()
    {
        Debug.Log("[层淡入淡出] 开始测试");
        StartCoroutine(LayerFadeCoroutine());
    }

    /// <summary>
    /// 层淡入淡出协程。
    /// </summary>
    IEnumerator LayerFadeCoroutine()
    {
        // 确保层存在并播放动画
        var layer1 = animComponent[1];
        layer1.Mask = upperBodyMask;
        layer1.Play(upperBodyClip);

        // 等待一帧确保动画开始
        yield return null;

        // 淡出层
        Debug.Log("[层淡入淡出] 开始淡出 Layer 1");
        layer1.StartLayerFade(0f, 1f);

        yield return new WaitForSeconds(1.5f);
        Debug.Log($"[层淡入淡出] Layer 1 权重: {layer1.Weight:F2}");

        // 淡入层
        Debug.Log("[层淡入淡出] 开始淡入 Layer 1");
        layer1.StartLayerFade(1f, 1f);

        yield return new WaitForSeconds(1.5f);
        Debug.Log($"[层淡入淡出] Layer 1 权重: {layer1.Weight:F2}");
        Debug.Log("[层淡入淡出] ✅ 测试完成");
    }

    /// <summary>
    /// 动态创建层测试。
    /// 演示如何动态创建多个层。
    /// </summary>
    void TestDynamicLayerCreation()
    {
        Debug.Log("[动态创建层] 开始测试");

        // 当前层数
        Debug.Log($"[动态创建层] 当前层数: {animComponent.LayerCount}");

        // 访问 Layer 5，会自动创建 Layer 0~5
        var layer5 = animComponent[5];
        Debug.Log($"[动态创建层] 访问 Layer 5 后，层数: {animComponent.LayerCount}");

        // 验证所有层都已创建
        for (int i = 0; i < animComponent.LayerCount; i++)
        {
            var layer = animComponent[i];
            Debug.Log($"[动态创建层] Layer {i}: {(layer != null ? "已创建" : "null")}");
        }

        Debug.Log("[动态创建层] ✅ 测试完成");
    }

    /// <summary>
    /// 多层同时播放测试。
    /// 演示基础层 + 上半身层 + 叠加层同时播放。
    /// </summary>
    void TestMultipleLayers()
    {
        Debug.Log("[多层同时播放] 开始测试");

        // Layer 0: 基础层 - 行走
        animComponent.Play(walkClip, 0);
        Debug.Log("[多层同时播放] Layer 0: 行走动画");

        // Layer 1: 上半身层 - 攻击
        if (upperBodyClip != null && upperBodyMask != null)
        {
            var layer1 = animComponent[1];
            layer1.Mask = upperBodyMask;
            layer1.Play(upperBodyClip, 0.25f);
            Debug.Log("[多层同时播放] Layer 1: 上半身动画（带 Mask）");
        }

        // Layer 2: 叠加层 - 呼吸
        if (breatheClip != null)
        {
            var layer2 = animComponent[2];
            layer2.IsAdditive = true;
            layer2.Play(breatheClip, 0.25f);
            Debug.Log("[多层同时播放] Layer 2: 叠加动画（Additive）");
        }

        Debug.Log($"[多层同时播放] 总层数: {animComponent.LayerCount}");
        Debug.Log("[多层同时播放] ✅ 测试完成 - 角色应该同时播放三个动画");
    }
}
}