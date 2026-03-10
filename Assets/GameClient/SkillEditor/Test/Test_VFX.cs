using System.Collections;
using System.Collections.Generic;
using Game.MAnimSystem;
using SkillEditor;
using SU = SkillEditor.SerializationUtility;
using UnityEngine;
using Game.Adapters;
public class Test_VFX : MonoBehaviour
{
    public TextAsset skillAsset; // 直接拖入 TextAsset 资源（编辑器专用）
    [Range(0f, 3.0f)]
    public float speedMultiplier = 1.0f; // 用于测试不同的播放速度
    private AnimComponent animComp;
    private SkillRunner runner;
    private ProcessContext context;
    private SkillTimeline timeline;
    private float timer = 0f;
    public void Start()
    {
        if(skillAsset == null)
        {
            Debug.LogError("请在 Inspector 中拖入一个 Skill JSON 资源");
            return;
        }
        // try
        // {
            animComp = gameObject.GetComponent<AnimComponent>();
            // 1. 初始化
            if (animComp == null) animComp = gameObject.AddComponent<AnimComponent>();
            animComp.Initialize();

            // 2. 准备上下文
            context = new ProcessContext(gameObject, SkillEditor.PlayMode.Runtime);
        // 3. 添加服务
        context.AddService<ISkillAnimationHandler>(new SkillAnimationHandler(animComp));
        context.AddService<ISkillBoneGetter>(new SkillBoneGetter(gameObject)); // 注入测试用 ISkillActor 实现
            context.AddService<MonoBehaviour>(this);
            runner = new SkillRunner(SkillEditor.PlayMode.Runtime);

            timeline = SU.OpenFromJson(skillAsset);
            timeline.isLoop = true;
            // 5. 开始播放
            runner.Play(timeline, context);
            Debug.Log($"播放开始: State={runner.CurrentState}");
        // }
        // catch (System.Exception ex)
        // {
        //     Debug.LogError($"测试初始化失败: {ex.Message}");
        // }
    }
    void Update()
    {
        // try
        // {
            if (runner != null)
            {
                context.GlobalPlaySpeed = speedMultiplier; // 动态调整全局播放速度
                timer += Time.deltaTime;
                float step = 1f / 30f;

                // 使用 while 处理单帧时间过长的情况（追帧）
                while (timer >= step)
                {
                    timer -= step; // <--- 关键：减去步长，保留余数 (0.04 - 0.0333 = 0.0067)
                    runner.Tick(step);
                }
            }
        // }
        // catch (System.Exception ex)
        // {
        //     Debug.LogError($"测试运行时异常: {ex.Message}");
        // }
    }
}
