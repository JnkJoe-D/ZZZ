namespace Game.GamePlay
{
using Game.Framework;
using Game.GamePlay;
using ATEditor;
using UnityEngine;
using SU = ATEditor.SerializationUtility;
public class Test_Audio : MonoBehaviour
{
    public TextAsset skillAsset; // 直接拖入 TextAsset 资源（编辑器专用）
    [Range(0f, 3.0f)]
    public float speedMultiplier = 1.0f; // 用于测试不同的播放速度
    private ActionRunner runner;
    private ProcessContext context;
    private ActionTimeline timeline;
    private float timer = 0f;
    public void Start()
    {
        try
        {
            // 2. 准备上下文
            context = new ProcessContext(gameObject, ATEditor.PlayMode.Runtime,
                ATServiceFactory.ProvideService);
            runner = new ActionRunner(ATEditor.PlayMode.Runtime);

            // 4.反序列化
            timeline = SU.OpenFromJson(skillAsset);
            timeline.isLoop = true;

            // 5. 开始播放
            runner.Play(timeline, context);
            GLog.Info(LogTags.Audio, $"播放开始: State={runner.CurrentState}");
        }
        catch (System.Exception ex)
        {
            GLog.Error(LogTags.Audio, $"测试初始化失败: {ex.Message}");
        }
    }
    void Update()
    {
        try
        {
            if (runner != null)
            {
                context.PresentationPlaySpeed = speedMultiplier; // 动态调整表现播放速度
                timer += Time.deltaTime;
                float step = 1f / 30f;

                // 使用 while 处理单帧时间过长的情况（追帧）
                while (timer >= step)
                {
                    timer -= step; // <--- 关键：减去步长，保留余数 (0.04 - 0.0333 = 0.0067)
                    runner.Tick(step * speedMultiplier);
                }
            }
        }
        catch (System.Exception ex)
        {
            GLog.Error(LogTags.Audio, $"测试运行时异常: {ex.Message}");
        }
    }
}

}
