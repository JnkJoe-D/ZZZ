using UnityEngine;
using SkillEditor;
using Game.MAnimSystem;
using SU = SkillEditor.SerializationUtility;
using Game.Adapters;
public class Test_Switch : MonoBehaviour
{
    public TextAsset skillAsset1;
    public TextAsset skillAsset2;
    [Range(0f, 3.0f)]
    public float speedMultiplier = 1.0f; // 用于测试不同的播放速度
    private AnimComponent animComp;
    private SkillRunner runner;
    private ProcessContext context;
    private SkillTimeline timeline1;
    private SkillTimeline timeline2;
    private float timer = 0f;
    public void Start()
    {
        try
        {
            animComp = gameObject.GetComponent<AnimComponent>();
            // 1. 初始化
            if (animComp == null) animComp = gameObject.AddComponent<AnimComponent>();
            animComp.Initialize();

            // 2. 准备上下文
            // 2. 准备上下文
            // 2. 准备上下文
            context = new ProcessContext(gameObject, SkillEditor.PlayMode.Runtime,
                SkillServiceFactory.ProvideService);
            runner = new SkillRunner(SkillEditor.PlayMode.Runtime);

            timeline1 = SU.OpenFromJson(skillAsset1);
            timeline1.isLoop = true;
            timeline2 = SU.OpenFromJson(skillAsset2);
            timeline2.isLoop = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"测试初始化失败: {ex.Message}");
        }
    }
    void Update()
    {
        try
        {
            if(Input.GetKeyDown(KeyCode.Alpha1))
            {
                runner.Play(timeline1, context);
                Debug.Log($"播放开始: timeline1");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                runner.Play(timeline2, context);
                Debug.Log($"播放开始: timeline2");
            }
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
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"测试运行时异常: {ex.Message}");
        }
    }
}
