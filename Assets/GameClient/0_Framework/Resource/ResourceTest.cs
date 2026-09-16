using UnityEngine;
using System.Collections;
using Game.Framework;


namespace Game.Framework
{
    /// <summary>
    /// 资源加载测试脚本
    /// 挂在场景中，当资源系统初始化完成后尝试从预制体加载 Cube
    /// </summary>
    public class ResourceTest : MonoBehaviour
    {
        [Header("准备加载的资源路径")]
        public string assetPath = "Assets/ABAssets/Cube.prefab";

        private void Start()
        {
            // 订阅初始化完成事件
            EventCenter.Subscribe<ResourceInitializedEvent>(OnResourceInited);
        }

        private void OnDestroy()
        {
            EventCenter.Unsubscribe<ResourceInitializedEvent>(OnResourceInited);
        }

        private void OnResourceInited(ResourceInitializedEvent e)
        {
            GLog.Info(LogTags.Resource, "资源系统就绪，准备加载测试对象...");
            StartCoroutine(DoTest());
        }

        private IEnumerator DoTest()
        {
            // 等待一小会儿确保稳定
            yield return new WaitForSeconds(0.5f);

            GLog.Info(LogTags.Resource, $"开始异步加载: {assetPath}");
            
            // 使用我们封装的 ResourceManager API
            yield return ResourceManager.Instance.InstantiateAsync(
                assetPath,
                (go) =>
                {
                    if (go != null)
                    {
                        GLog.Info(LogTags.Resource, $"成功实例化对象: {go.name}");
                        go.transform.position = Vector3.zero;
                    }
                    else
                    {
                        GLog.Error(LogTags.Resource, $"加载资源失败: {assetPath}。请检查路径是否正确，以及 YooAsset 收集器中是否包含了该资源。");
                    }
                }
            );
        }
    }
}
