using System.Collections;
using UnityEngine;
using Game.Framework;
using Game.Resource;
using Game.Logic;
namespace Game.UI.Test
{
    /// <summary>
    /// 热更新 UI 与通用弹窗流程测试脚本
    /// 使用方法：
    /// 1. 在新建的 Init 场景中，确保已有挂载 GameRoot 脚本的空物体 [System]。
    /// 2. 确保 GameRoot 面板上的 Resource Config 中的 Play Mode 设置为 Editor Simulate 或 Offline。
    /// 3. 将此脚本挂载到任意对象上（比如 Main Camera）。
    /// 4. 运行游戏观察。
    /// </summary>
    public class TestHotUpdateUI : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // 1. 等待 GameRoot 核心框架初始化完成
            // (现在 GameRoot 内部也会尝试弹出此面板，我们只需等待即可)
            yield return new WaitUntil(() => GameRoot.IsInitialized);
            yield return new WaitForSeconds(0.5f); // 确保所有的 UI 真正渲染和绑定完成

            Debug.Log("<color=green>[Test] 游戏框架启动完毕，开始测试热更 UI！</color>");
            
            // 稍微等待一下表现效果
            yield return new WaitForSeconds(1.5f);

            // 3. 模拟底层 ResourceUpdater 发现更新并触发拦截弹窗
            Debug.Log("<color=yellow>[Test] 模拟：发现新版本，弹出确认框...</color>");
            bool isConfirmed = false;
            
            EventCenter.Publish(new HotUpdateRequireConfirmEvent
            {
                FileCount = 12,
                TotalDownloadBytes = 104857600, // 100MB
                ConfirmAction = () => 
                {
                    Debug.Log("<color=green>[Test] 用户点击了确认，开始模拟下载！</color>");
                    isConfirmed = true;
                }
            });

            // 4. 挂起等待用户点击弹窗上的"确认"按钮
            yield return new WaitUntil(() => isConfirmed);

            // 5. 模拟资源下载进度条变化
            float duration = 4f; // 模拟下载持续 4 秒
            float elapsed = 0f;
            long totalBytes = 104857600;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                long currentBytes = (long)(totalBytes * progress);

                EventCenter.Publish(new HotUpdateProgressEvent
                {
                    TotalDownloadCount = 12,
                    CurrentDownloadCount = (int)(progress * 12),
                    TotalDownloadBytes = totalBytes,
                    CurrentDownloadBytes = currentBytes
                });

                yield return null; // 等待下一帧刷新
            }

            // 6. 模拟下载完成并满血
            EventCenter.Publish(new HotUpdateProgressEvent
            {
                TotalDownloadCount = 12,
                CurrentDownloadCount = 12,
                TotalDownloadBytes = totalBytes,
                CurrentDownloadBytes = totalBytes
            });
            
            Debug.Log("<color=green>[Test] 模拟下载完成！触发资源就绪事件！</color>");
            yield return new WaitForSeconds(1f);
            
            // 通知 UI 资源已经就绪
            EventCenter.Publish(new ResourceInitializedEvent());
        }
    }
}
