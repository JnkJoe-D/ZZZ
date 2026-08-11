using Game.Framework;
using Game.Resource;
using Game.Network;
using Game.Logic;
using UnityEngine;

namespace Game.UI
{
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/HotUpdate/HotUpdatePanel.prefab", Layer = UILayer.Loading, IsFullScreen = true)]
    public class HotUpdateModule : UIModule<HotUpdateView, HotUpdateModel>
    {
        protected override void OnCreate()
        {   
            // 订阅资源下载进度事件
            EventCenter.Subscribe<HotUpdateProgressEvent>(OnDownloadProgress);
            // 订阅需要用户确认更新的事件（弹出 MessageBox）
            EventCenter.Subscribe<HotUpdateRequireConfirmEvent>(OnRequireConfirm);
            // 订阅更新失败事件
            EventCenter.Subscribe<HotUpdateFailedEvent>(OnUpdateFailed);
            // 订阅状态阶段事件（如检查版本，更新清单的进度等）
            EventCenter.Subscribe<HotUpdateStatusEvent>(OnStatusUpdate);
            // 订阅更新完成事件以拦截关闭并等待引擎后续指令
            EventCenter.Subscribe<HotUpdateCompletedEvent>(OnUpdateCompleted);

            // 订阅进入网络互连 / 登录阶段的最高指令
            EventCenter.Subscribe<GameLoginStageStartEvent>(OnLoginStageStart);

            // 订阅网络连接相关事件
            EventCenter.Subscribe<NetConnectedEvent>(OnNetConnected);
            EventCenter.Subscribe<NetDisconnectedEvent>(OnNetDisconnected);
            EventCenter.Subscribe<NetReconnectingEvent>(OnNetReconnecting);
            EventCenter.Subscribe<NetReconnectedEvent>(OnReconnectSuccess);
            EventCenter.Subscribe<NetReconnectFailedEvent>(OnNetReconnectFailed);
            
            View?.UpdateView(Model);
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            EventCenter.Unsubscribe<HotUpdateProgressEvent>(OnDownloadProgress);
            EventCenter.Unsubscribe<HotUpdateStatusEvent>(OnStatusUpdate);
            EventCenter.Unsubscribe<HotUpdateCompletedEvent>(OnUpdateCompleted);
            
            EventCenter.Unsubscribe<GameLoginStageStartEvent>(OnLoginStageStart);

            EventCenter.Unsubscribe<NetConnectedEvent>(OnNetConnected);
            EventCenter.Unsubscribe<NetDisconnectedEvent>(OnNetDisconnected);
            EventCenter.Unsubscribe<NetReconnectingEvent>(OnNetReconnecting);
            EventCenter.Unsubscribe<NetReconnectedEvent>(OnReconnectSuccess);
            EventCenter.Unsubscribe<NetReconnectFailedEvent>(OnNetReconnectFailed);

            StopDotAnim();
        }

        private void OnDownloadProgress(HotUpdateProgressEvent e)
        {
            Model.DownloadProgress = e.Progress;
            Model.StatusText = $"正在下载资源：{e.CurrentDownloadBytes / 1048576f:F2} MB / {e.TotalDownloadBytes / 1048576f:F2} MB";
            
            // 计算简单的速度 (可根据真实时间 Delta 计算)
            Model.SpeedText = "下载中...";
            
            View?.UpdateView(Model);
        }

        private void OnRequireConfirm(HotUpdateRequireConfirmEvent e)
        {
            // 这是关键点：不再局限在 Widget，而是调用全局的 MessageBox
            UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
            {
                Title = "发现新版本",
                Content = $"本次需要更新 {e.TotalDownloadBytes / 1048576f:F10} MB 资源，是否立即下载？",
                ConfirmText = "立即更新",
                CancelText = "退出游戏",
                OnConfirm = () => 
                {
                    // 通知底层继续下载
                    e.ConfirmAction?.Invoke();
                },
                OnCancel = () => 
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            });
        }

        private void OnStatusUpdate(HotUpdateStatusEvent e)
        {
            Model.DownloadProgress = e.Progress;
            Model.StatusText = e.StatusText;
            Model.SpeedText = ""; // 检查阶段不需要网络速度
            View?.UpdateView(Model);
        }

        private void OnUpdateCompleted(HotUpdateCompletedEvent e)
        {
            // 此处仅仅代表 YooAsset 下载和检验通过。引擎的其他（如配置，Lua）等可能还在加载
            // 所以先不开始网络连动。仅仅报告状态
            Debug.Log("[HotUpdateModule] 资源就绪，等待整个引擎初始化结束...");
            Model.DownloadProgress = 1f;
            Model.StatusText = "资源已准备就绪，正在加载核心配置...";
            View?.UpdateView(Model);
        }

        private void OnLoginStageStart(GameLoginStageStartEvent e)
        {
            Debug.Log("[HotUpdateModule] 核心就绪，转入连接服务器动画阶段...");
            Model.StatusText = "正在连接网络服务器";
            View?.UpdateView(Model);

            StartDotAnim();
        }

        // ==========================================
        // 网络状态连动
        // ==========================================
        private Coroutine _dotAnimRoutine;

        private void StartDotAnim()
        {
            if (_dotAnimRoutine != null || View == null) return;
            _dotAnimRoutine = View.StartCoroutine(DotAnimTask());
        }

        private void StopDotAnim()
        {
            if (_dotAnimRoutine != null && View != null)
            {
                View.StopCoroutine(_dotAnimRoutine);
                _dotAnimRoutine = null;
            }
        }

        private System.Collections.IEnumerator DotAnimTask()
        {
            string[] dots = { ".", "..", "...", "....", "....." };
            int i = 0;
            while (true)
            {
                Model.SpeedText = dots[i];
                View?.UpdateView(Model);
                i = (i + 1) % dots.Length;
                yield return new UnityEngine.WaitForSeconds(0.35f);
            }
        }

        private void OnNetConnected(NetConnectedEvent e)
        {
            StopDotAnim();
            Model.StatusText = "连接服务器成功";
            Model.SpeedText = "";
            View?.UpdateView(Model);

            // 延迟0.5秒后切入游戏，关闭本热更与加载屏
            View.StartCoroutine(DelayClose());
        }
        
        private System.Collections.IEnumerator DelayClose()
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);
            UIManager.Instance.Close(this);
            // 这里一并打开层级更低的背景视频层与窗口操作层
            UIManager.Instance.Open<LoginBackgroundModule>();
            UIManager.Instance.Open<LoginModule>();
        }

        private void OnNetDisconnected(NetDisconnectedEvent e)
        {
            // 如果是断线不一定等于死机，也可能会被 NetworkManager 自动切入重连循环
            // 在这里只需稍加观察，它马上会抛 NetReconnectingEvent
        }

        private void OnNetReconnecting(NetReconnectingEvent e)
        {
            Model.StatusText = $"正在重新连接服务器({e.Attempt})";
            StartDotAnim();
            View?.UpdateView(Model);
        }
        private void OnReconnectSuccess(NetReconnectedEvent e)
        {
            StopDotAnim();
            Model.StatusText = "重新连接服务器成功";
            Model.SpeedText = "";
            View?.UpdateView(Model);

            // 延迟0.5秒后切入游戏，关闭本热更与加载屏
            View.StartCoroutine(DelayClose());
        }
        private void OnNetReconnectFailed(NetReconnectFailedEvent e)
        {
            StopDotAnim();
            Model.StatusText = "连接服务器失败";
            Model.SpeedText = "";
            View?.UpdateView(Model);

            UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
            {
                Title = "连接错误",
                Content = "无法连接到主服务器，请检查网络设置或稍后重试。",
                ConfirmText = "重试",
                CancelText = "退出游戏",
                OnConfirm = () => 
                {
                    Model.StatusText = "正在重新连接服务器";
                    StartDotAnim();
                    View?.UpdateView(Model);
                    // 手动要求底部网络组件重新连接
                    NetworkManager.Instance.ConnectTcp();
                },
                OnCancel = () => 
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    UnityEngine.Application.Quit();
#endif
                }
            });
        }

        private void OnUpdateFailed(HotUpdateFailedEvent e)
        {
            StopDotAnim();
            
            string statusMsg = "更新失败";
            switch (e.Reason)
            {
                case Game.Resource.HotUpdateFailReason.InitializeFailed:
                    statusMsg = "初始化资源系统失败";
                    break;
                case Game.Resource.HotUpdateFailReason.VersionRequestFailed:
                    statusMsg = "无法连接到资源服务器";
                    break;
                case Game.Resource.HotUpdateFailReason.ManifestUpdateFailed:
                    statusMsg = "检查资源更新失败";
                    break;
                case Game.Resource.HotUpdateFailReason.DownloadFailed:
                    statusMsg = "下载资源文件失败";
                    break;
                default:
                    statusMsg = "更新过程中发生未知错误";
                    break;
            }

            Model.StatusText = statusMsg;
            Model.SpeedText = "";
            View?.UpdateView(Model);

            UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
            {
                Title = "更新失败",
                Content = $"({statusMsg}) 遇到错误：{e.Message}\n请检查网络后重试。",
                ConfirmText = "重试",
                CancelText = "退出游戏",
                OnConfirm = () => 
                {
                    Model.StatusText = "正在重新开始检查更新";
                    View?.UpdateView(Model);
                    EventCenter.Publish(new Game.Resource.HotUpdateRetryEvent());
                },
                OnCancel = () => 
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    UnityEngine.Application.Quit();
#endif
                }
            });
        }

        
    }
}
