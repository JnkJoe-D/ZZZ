using System.Collections;
using UnityEngine;

using Game.UI;
using Game.Framework;
using Game.GamePlay;

namespace Game.App
{
    /// <summary>
    /// 游戏全局入口与生命周期管理器
    ///
    /// 职责：
    ///   1. 按正确顺序初始化所有全局子系统
    ///   2. 统一驱动各子系统的 Update / Shutdown
    ///   3. DontDestroyOnLoad，贯穿整个应用生命周期
    ///
    /// 初始化顺序（关键，不可随意调整）：
    ///   Asset → Config → Pool → Lua → Net → Audio → UI → Scene
    ///
    /// 使用方式：
    ///   将此脚本挂在场景中名为 "[GameRoot]" 的 GameObject 上
    ///   或通过 Resources.Load 动态创建（推荐热更接入后改为此方式）
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        // ── 单例（仅限框架内部访问，业务层通过子系统接口访问）
        private static GameRoot _instance;
        public static bool IsInitialized { get; private set; }

        // ── 子系统引用 ─────────────────────────
        // (所有系统现已改为通过 Singleton / MonoSingleton 统一获取 Instance)

        [Header("资源管理配置")]
        [SerializeField] private ResourceConfig _resourceConfig;

        [Header("网络配置")]
        [SerializeField] private string _serverHost = "127.0.0.1";
        [SerializeField] private int    _tcpPort    = 33333;
        [SerializeField] private int    _udpPort    = 33334;

        // ────────────────────────────────────────
        // Unity 生命周期
        // ────────────────────────────────────────

        private void Awake()
        {
            // 单例保护
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            StartCoroutine(InitializeSequence());
        }

        private void Update()
        {
            if (!IsInitialized) return;

            Game.GamePlay.TimeManager.Instance.Update();

            // 驱动延迟事件队列（每帧刷新一次）
            EventCenter.FlushPending();

            // 驱动网络管理器（主线程消息分发、心跳、重连）
            Game.Framework.NetworkManager.Instance?.Update();
            
            float uiDelta = Time.unscaledDeltaTime * Game.GamePlay.TimeManager.Instance.FinalUIScale;
            GameCameraManager.Instance?.Update(uiDelta);

            // TODO: 驱动其他子系统
            // _luaManager?.Update();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        // ────────────────────────────────────────
        // 初始化流水线
        // ────────────────────────────────────────

        /// <summary>
        /// 异步初始化序列
        /// 使用协程保证顺序执行，并支持未来接入进度条 UI
        /// </summary>
        private IEnumerator InitializeSequence()
        {
            GLog.Info(LogTags.GameRoot, "===== 游戏启动 =====");

            // ── Step 1: 全局对象池 ────────────────────
            GlobalPoolManager.Initialize();
            GLog.Info(LogTags.GameRoot, "[1/9] Pool ... OK");
            yield return null;

            // ── Step 2: 核心底层服务启机 ─────────────────────
            FSMManager.Instance.Initialize();
            GLog.Info(LogTags.GameRoot, "[2/11] FSM ... OK");

            Game.Framework.NetworkManager.Instance.Initialize(_serverHost, _tcpPort, _udpPort);
            GLog.Info(LogTags.GameRoot, "[3/11] Network (Prepared) ... OK");

            UIManager.Instance.Initialize(this);
            GLog.Info(LogTags.GameRoot, "[4/11] UI ... OK");
            yield return null;

            // ── Step 3: 唤起热更新界面以接收事件 ────────
            UIManager.Instance.Open<Game.UI.HotUpdateModule>();  //Resource加载
            yield return null;

            // ── Step 4: 资源管理器（YooAsset）────────────────
            yield return StartCoroutine(
                ResourceManager.Instance.InitializeAsync(_resourceConfig, this)
            );
            if (!ResourceManager.Instance.IsInitialized)
            {
                GLog.Error(LogTags.GameRoot, "资源管理器初始化失败，游戏终止");
                yield break;
            }
            GLog.Info(LogTags.GameRoot, "[4/9] Assets ... OK");
            
            // ── Step 4.5: 向技能编辑器注入资源加载适配器 ────────────────
            ATEditor.Runtime.ActionSystemContext.InjectAssetLoader(new ATAssetLoader());
            GLog.Info(LogTags.GameRoot, "[4.5/9] SkillEditor AssetLoader Injected ... OK");
            
            yield return null;

            // ── Step 5: 基础配置加载 ──────────────────
            var configMgrTask = ConfigManager.Instance.InitializeAsync();
             while (!configMgrTask.IsCompleted) yield return null;
            GLog.Info(LogTags.GameRoot, "[5/9] Config ... OK");
            yield return null;

            // ── Step 6: Lua 音频等 ───────────────
            GLog.Info(LogTags.GameRoot, "[6/9] Lua ... (TODO: XLua)");
            
            // ── Step 7: 全局音频管理器 ───────────────
            AudioManager.Instance.Initialize();
            GLog.Info(LogTags.GameRoot, "[7/9] System Audio ... OK");

            // ── Step 8: 场景管理器 ────────────────────
            SceneManager.Instance.Initialize(this);
            GLog.Info(LogTags.GameRoot, "[8/10] Scene ... OK");
            
            // ── Step 9: 输入管理器 ────────────────────
            Game.GamePlay.InputManager.Instance.Initialize();
            GLog.Info(LogTags.GameRoot, "[9/11] Input ... OK");

            // ── Step 10: 相机管理器 ───────────────────
            GameCameraManager.Instance.Initialize();

            // ── Step 11: 玩家连接层与战斗业务管理器 ────────────────
            TeamManager.Instance.Initialize();
            MonsterManager.Instance.Initialize();

            // 核心修复：激活主逻辑循环驱动
            IsInitialized = true;
            GLog.Info(LogTags.GameRoot, "===== 核心子系统流水线就绪，主驱动循环已激活 =====");

            // 发布初始化完成事件，各系统可以订阅此事件做后置操作
            EventCenter.Publish(new GameInitializedEvent());

            // ── Step 12: 发起网络握手与切入登录流 ──────────────────────────────────
            GLog.Info(LogTags.GameRoot, "[12/12] 流水线执行完毕，通知界面切换至连网状态...");
            EventCenter.Publish(new GameLoginStageStartEvent());
            
            // 为了让玩家看清前面的文字动画稍微驻留 0.5s，也可以不加，但这里加个缓冲让画面不切太抖
            yield return new WaitForSeconds(0.5f);
            
            GLog.Info(LogTags.GameRoot, "发起基于 TCP 的主服务连接...");
            Game.Framework.NetworkManager.Instance.ConnectTcp();
        }

        // ────────────────────────────────────────
        // 关闭流程
        // ────────────────────────────────────────
        private void Shutdown()
        {
            GLog.Info(LogTags.GameRoot, "===== 游戏关闭 =====");

            IsInitialized = false;

            // 各子系统 Shutdown（顺序与初始化相反）

            MonsterManager.Instance?.Shutdown();
            TeamManager.Instance?.Shutdown();
            ActionManager.Instance?.Shutdown();
            Game.GamePlay.InputManager.Instance?.Shutdown();
            GameCameraManager.Instance?.Shutdown();
            SceneManager.Instance?.Shutdown();
            FSMManager.Instance?.Shutdown();
            AudioManager.Instance?.Shutdown();
            UIManager.Instance?.Shutdown();
            ResourceManager.Instance?.Shutdown();
            Game.Framework.NetworkManager.Instance?.Shutdown();
            TimeManager.Instance?.ResetToNormal();
            EventCenter.ClearAll();
            GlobalPoolManager.DisposeAll();

            GLog.Info(LogTags.GameRoot, "===== 关闭完成 =====");
        }

        // ────────────────────────────────────────
        // 场景切换钩子（供 SceneManager 调用）
        // ────────────────────────────────────────

        /// <summary>
        /// 场景切换前调用：清理当前场景的事件订阅和对象池缓存
        /// TODO: 由 SceneManager 在切换前调用
        /// </summary>
        public static void OnSceneUnload()
        {
            // 只清理对象池缓存（空闲对象），不清理订阅
            GlobalPoolManager.ClearAll();
            // 卸载场景结束后未使用的资源
            ResourceManager.Instance?.UnloadUnused();
        }
    }

    // ────────────────────────────────────────────────────────────
    // 框架内置事件（放在同文件，避免碎片化文件）
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 游戏初始化完成事件
    /// 所有子系统初始化完毕后由 GameRoot 发布
    /// </summary>
    public struct GameInitializedEvent : IGameEvent { }
    
    /// <summary>
    /// 游戏即将进入登录流程大阶段
    /// 这代表前置资源完全就绪，允许热更面板或其他 UI 转场以显示“连接服务器中...”
    /// </summary>
    public struct GameLoginStageStartEvent : IGameEvent { }
}
