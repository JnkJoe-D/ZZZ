using UnityEngine;
using Game.Framework;

using Game.GamePlay;
namespace Game.GamePlay
{
    /// <summary>
    /// 网络功能测试脚本
    /// 挂载到场景中即可通过快捷键测试网络联通性
    /// </summary>
    public class NetworkTest : MonoBehaviour
    {
        [Header("快捷键配置")]
        public KeyCode connectKey = KeyCode.C;
        public KeyCode disconnectKey = KeyCode.D;
        public KeyCode loginTestKey = KeyCode.L;

        public KeyCode uiTestKey    = KeyCode.U;

        private void Start()
        {
            // 订阅网络事件
            EventCenter.Subscribe<NetConnectedEvent>(OnConnected);
            EventCenter.Subscribe<NetDisconnectedEvent>(OnDisconnected);
            EventCenter.Subscribe<HeartbeatResponseEvent>(OnHeartbeat);
            EventCenter.Subscribe<ServerErrorEvent>(OnServerError);
            EventCenter.Subscribe<NetReconnectingEvent>(OnReconnecting);
            EventCenter.Subscribe<NetReconnectedEvent>(OnReconnected);
        }

        private void OnDestroy()
        {
            // 取消订阅
            EventCenter.Unsubscribe<NetConnectedEvent>(OnConnected);
            EventCenter.Unsubscribe<NetDisconnectedEvent>(OnDisconnected);
            EventCenter.Unsubscribe<HeartbeatResponseEvent>(OnHeartbeat);
            EventCenter.Unsubscribe<ServerErrorEvent>(OnServerError);
            EventCenter.Unsubscribe<NetReconnectingEvent>(OnReconnecting);
            EventCenter.Unsubscribe<NetReconnectedEvent>(OnReconnected);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(connectKey))
            {
                GLog.Info(LogTags.Network, "尝试连接 TCP...");
                NetworkManager.Instance?.ConnectTcp();
            }

            if (UnityEngine.Input.GetKeyDown(disconnectKey))
            {
                GLog.Info(LogTags.Network, "尝试主动断开 TCP...");
                NetworkManager.Instance?.DisconnectTcp();
            }

            if (UnityEngine.Input.GetKeyDown(loginTestKey))
            {
                // 注意：由于当前只有手写的核心消息，login.proto 还没编译，
                // 这里只能演示发包结构，暂时无法发送真正的 C2S_Login 对象
                // 除非手动在 GeneratedMessages.cs 里也手写一份 C2S_Login
                GLog.Info(LogTags.Network, "登录协议尚未编译，目前仅支持心跳测试");
            }

            if (UnityEngine.Input.GetKeyDown(uiTestKey))
            {
                GLog.Info(LogTags.Network, "尝试打开测试 UI...");
            }

            // --- Luban 配置测试 ---
            if (UnityEngine.Input.GetKeyDown(KeyCode.K))
            {
                var tables = ConfigManager.Instance.Tables;
                if (tables != null)
                {
                    var item = tables.Tbitem.Get(1001);
                    if (item != null)
                    {
                        GLog.Info(LogTags.Network, $"成功读取道具: ID={item.Id}, Name={item.Name}, Desc={item.Desc}");
                    }
                    else
                    {
                        GLog.Warning(LogTags.Network, "找不到 ID 为 1 的道具");
                    }
                }
                else
                {
                    GLog.Error(LogTags.Network, "ConfigManager Tables 尚未初始化！");
                }
            }
        }

        // ── 事件回调 ────────────────────────────

        private void OnConnected(NetConnectedEvent evt)
        {
            GLog.Info(LogTags.Network, $"连接成功！Host: {evt.Host}, Port: {evt.Port}");
        }

        private void OnDisconnected(NetDisconnectedEvent evt)
        {
            GLog.Info(LogTags.Network, $"连接断开！原因: {evt.Reason}, 消息: {evt.Message}");
        }

        private void OnHeartbeat(HeartbeatResponseEvent evt)
        {
            GLog.Info(LogTags.Network, $"收到心跳响应 | RTT: {evt.RttMs}ms | ServerTime: {evt.ServerTime}");
        }

        private void OnServerError(ServerErrorEvent evt)
        {
            GLog.Info(LogTags.Network, $"服务端报错 | Code: {evt.Code}, Msg: {evt.Message}");
        }

        private void OnReconnecting(NetReconnectingEvent evt)
        {
            GLog.Info(LogTags.Network, $"正在尝试重连... 第 {evt.Attempt} 次 | 等待 {evt.WaitSeconds:F1}s");
        }

        private void OnReconnected(NetReconnectedEvent evt)
        {
            GLog.Info(LogTags.Network, "重连成功！");
        }
    }
}
