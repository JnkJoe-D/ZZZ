using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Framework;
using Game.Resource;
using Game.Pool;

namespace Game.Scene
{
    /// <summary>
    /// 全局场景管理器
    /// 
    /// 职责：
    ///   1. 统筹场景生命周期，协调旧资源清理与新资源加载
    ///   2. 对外提供统一的场景切换接口
    ///   3. 广播场景切换进度回调给 UI 系统
    /// </summary>
    public class SceneManager : Game.Framework.Singleton<SceneManager>
    {
        private MonoBehaviour _coroutineHost;

        public string CurrentSceneName { get; private set; } = string.Empty;
        public bool IsLoading { get; private set; } = false;

        public void Initialize(MonoBehaviour host)
        {
            _coroutineHost = host;
            Debug.Log("[SceneManager] 初始化完成");
        }

        /// <summary>
        /// 切换主场景（混合加载模式：场景文件 + 必须预装的主资源）
        /// </summary>
        public void ChangeScene(SceneTransitionParams transitionParams)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneManager] 当前正在加载场景中，忽略请求: {transitionParams.SceneName}");
                return;
            }

            if (transitionParams == null || string.IsNullOrEmpty(transitionParams.SceneName))
            {
                Debug.LogError("[SceneManager] 转场参数为空或场景名称无效！");
                return;
            }

            _coroutineHost.StartCoroutine(ChangeSceneRoutine(transitionParams));
        }

        /// <summary>
        /// 兼容旧版本的调用（仅提供场景名词）
        /// </summary>
        public void ChangeScene(string sceneName, bool showLoading = true)
        {
            ChangeScene(new SceneTransitionParams { SceneName = sceneName, ShowLoading = showLoading });
        }

        private IEnumerator ChangeSceneRoutine(SceneTransitionParams p)
        {
            IsLoading = true;
            Debug.Log($"[SceneManager] 开始切换场景: {CurrentSceneName} -> {p.SceneName}");

            // ──────────────────────────────────────────
            // 阶段 1：清理预备 (进度 0% ~ 10%)
            // ──────────────────────────────────────────
            EventCenter.Publish(new SceneChangeBeginEvent { TransitionParams = p });
            EventCenter.Publish(new SceneLoadProgressEvent { Progress = 0f, LoadingText = "正在清理旧资源..." });
            
            yield return null; // 留 1 帧给 Loading 界面弹起

            // 清理旧缓存（空闲对象）
            GlobalPoolManager.ClearAll();
            
            EventCenter.Publish(new SceneLoadProgressEvent { Progress = 0.1f, LoadingText = "准备加载新地图..." });
            
            // ──────────────────────────────────────────
            // 阶段 2：场景文件异步加载 (进度 10% ~ 40%)
            // ──────────────────────────────────────────
            bool isSceneLoaded = false;
            
            // 换算 YooAsset 内部 0~1 的进度到我们 UI 定义的 10%~40% 区间 (即 0.3 的跨度)
            yield return ResourceManager.Instance.LoadSceneAsync(
                p.SceneName,
                onComplete: () => isSceneLoaded = true,
                onProgress: innerProgress => 
                {
                    EventCenter.Publish(new SceneLoadProgressEvent 
                    { 
                        Progress = 0.1f + innerProgress * 0.3f, 
                        LoadingText = "正在加载场景数据..." 
                    });
                },
                isAdditive: false
            );

            if (!isSceneLoaded)
            {
                EventCenter.Publish(new SceneChangeEndEvent { SceneName = p.SceneName, Success = false });
                IsLoading = false;
                yield break;
            }

            // ──────────────────────────────────────────
            // 阶段 3：并行预热实体资源 (进度 40% ~ 90%)
            // ──────────────────────────────────────────
            EventCenter.Publish(new SceneLoadProgressEvent { Progress = 0.4f, LoadingText = "正在加载核心资产..." });

            if (p.RequiredAssets != null && p.RequiredAssets.Count > 0)
            {
                int totalCount = p.RequiredAssets.Count;
                int loadedCount = 0;

                // 启动所有所需资源的异步加载任务
                var loadTasks = new System.Collections.Generic.List<Coroutine>();
                
                foreach (var assetPath in p.RequiredAssets)
                {
                    // 这里利用 ResourceManager.LoadAssetAsync 不关心返回值，仅仅是为了让他进内存池并触发 YooAsset 下载
                    // 注意：真实工业框架可能需要一个并发计数器。为简化代码，我们启动所有协程并监控 loadedCount
                    var task = _coroutineHost.StartCoroutine(ResourceManager.Instance.LoadAssetAsync<UnityEngine.Object>(
                        assetPath,
                        _ => 
                        {
                            loadedCount++;
                            float assetProgress = 0.4f + ((float)loadedCount / totalCount) * 0.5f; // 跨度 0.5
                            EventCenter.Publish(new SceneLoadProgressEvent 
                            { 
                                Progress = assetProgress, 
                                LoadingText = $"正在构建世界结构 ({loadedCount}/{totalCount})..." 
                            });
                        }
                    ,null));
                    loadTasks.Add(task);
                }

                // 挂起直到所有的前置预加载的资源全部完毕
                yield return new WaitUntil(() => loadedCount == totalCount);
            }
            
            // ──────────────────────────────────────────
            // 阶段 4：初始化与收尾 (进度 90% ~ 100%)
            // ──────────────────────────────────────────
            EventCenter.Publish(new SceneLoadProgressEvent { Progress = 0.9f, LoadingText = "初始化游戏逻辑..." });
            
            // 记录新场景并触发旧无用资产释放
            CurrentSceneName = p.SceneName;
            ResourceManager.Instance.UnloadUnused();
            yield return null;

            EventCenter.Publish(new SceneLoadProgressEvent { Progress = 1f, LoadingText = "加载完成" });
            
            IsLoading = false;
            EventCenter.Publish(new SceneChangeEndEvent { SceneName = p.SceneName, Success = true });
            Debug.Log($"[SceneManager] 场景及关联资产切换完成: {p.SceneName}");
        }

        /// <summary>
        /// 异步叠加加载场景（通常用于常驻 UI 场景或副场景）
        /// </summary>
        public void LoadAdditiveScene(string sceneName)
        {
            _coroutineHost.StartCoroutine(LoadAdditiveSceneRoutine(sceneName));
        }

        private IEnumerator LoadAdditiveSceneRoutine(string sceneName)
        {
            Debug.Log($"[SceneManager] 开始叠加加载场景: {sceneName}");
            
            bool isLoadSuccess = false;
            yield return ResourceManager.Instance.LoadSceneAsync(
                sceneName,
                onComplete: () => isLoadSuccess = true,
                onProgress: null,
                isAdditive: true
            );
            
            if (isLoadSuccess)
            {
                Debug.Log($"[SceneManager] 叠加场景加载成功: {sceneName}");
            }
        }
        
        /// <summary>
        /// 关闭管理器
        /// </summary>
        public void Shutdown()
        {
            IsLoading = false;
        }
    }
}
