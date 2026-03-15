using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 行为树运行时 HUD，用于在游戏界面中实时显示 AI 状态、黑板值和运行节点。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BehaviorTreeRuntimeDebugHud : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private bool visible = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F8;

        [Header("Tracking")]
        [SerializeField] private bool autoTrackSceneAgents = true;
        [SerializeField] private BehaviorTreeCharacterAgent targetAgent;
        [SerializeField] private float refreshIntervalSeconds = 0.25f;

        [Header("Layout")]
        [SerializeField] private Vector2 panelMargin = new Vector2(16f, 16f);
        [SerializeField] private float panelWidth = 460f;
        [SerializeField] private int maxBlackboardEntries = 24;
        [SerializeField] private bool showRunningNodes = true;
        [SerializeField] private bool drawTargetLine = true;

        private readonly List<BehaviorTreeCharacterAgent> trackedAgents = new List<BehaviorTreeCharacterAgent>();
        private readonly StringBuilder builder = new StringBuilder(2048);

        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle titleStyle;
        private Texture2D panelTexture;
        private float nextRefreshAt;

        /// <summary>
        /// 处理 HUD 开关、刷新目标代理列表，并绘制目标连线。
        /// </summary>
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
            }

            RefreshAgents(force: false);
            DrawWorldDebug();
        }

        /// <summary>
        /// 在屏幕右上角绘制灰色半透明的调试面板。
        /// </summary>
        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            RefreshAgents(force: false);
            EnsureStyles();

            float panelX = Mathf.Max(0f, Screen.width - panelWidth - panelMargin.x);
            float y = panelMargin.y;
            foreach (BehaviorTreeCharacterAgent agent in trackedAgents.Where(IsAgentUsable))
            {
                string content = BuildAgentDebugText(agent);
                float height = Mathf.Max(140f, textStyle.CalcHeight(new GUIContent(content), panelWidth - 20f) + 20f);
                Rect panelRect = new Rect(panelX, y, panelWidth, height);
                GUI.Box(panelRect, GUIContent.none, panelStyle);

                Rect titleRect = new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, 24f);
                GUI.Label(titleRect, $"Behavior Tree Runtime Debug - {agent.name}", titleStyle);

                Rect contentRect = new Rect(panelRect.x + 10f, panelRect.y + 34f, panelRect.width - 20f, panelRect.height - 44f);
                GUI.Label(contentRect, content, textStyle);

                y += height + 10f;
            }

            if (trackedAgents.Count == 0)
            {
                const float emptyHeight = 80f;
                float emptyPanelX = Mathf.Max(0f, Screen.width - panelWidth - panelMargin.x);
                Rect panelRect = new Rect(emptyPanelX, panelMargin.y, panelWidth, emptyHeight);
                GUI.Box(panelRect, GUIContent.none, panelStyle);
                GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, 24f), "Behavior Tree Runtime Debug", titleStyle);
                GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 34f, panelRect.width - 20f, 30f), "No active BehaviorTreeCharacterAgent found.", textStyle);
            }
        }

        /// <summary>
        /// 释放运行时创建的面板背景资源。
        /// </summary>
        private void OnDisable()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
                panelTexture = null;
            }
        }

        [ContextMenu("Refresh Debug Targets")]
        /// <summary>
        /// 手动强制刷新当前跟踪的 AI 列表。
        /// </summary>
        private void RefreshNow()
        {
            RefreshAgents(force: true);
        }

        /// <summary>
        /// 刷新 HUD 要显示的 AI 列表。
        /// </summary>
        /// <param name="force">是否无视刷新间隔立即刷新。</param>
        private void RefreshAgents(bool force)
        {
            if (!force && Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = Time.unscaledTime + Mathf.Max(0.05f, refreshIntervalSeconds);
            trackedAgents.Clear();

            if (targetAgent != null)
            {
                trackedAgents.Add(targetAgent);
            }

            if (autoTrackSceneAgents)
            {
                foreach (BehaviorTreeCharacterAgent agent in FindObjectsByType<BehaviorTreeCharacterAgent>(FindObjectsSortMode.None))
                {
                    if (agent != null && !trackedAgents.Contains(agent))
                    {
                        trackedAgents.Add(agent);
                    }
                }
            }

            trackedAgents.RemoveAll(agent => agent == null);
        }

        /// <summary>
        /// 判断某个代理是否仍可用于显示。
        /// </summary>
        /// <param name="agent">待检测的代理。</param>
        /// <returns>是否可用。</returns>
        private static bool IsAgentUsable(BehaviorTreeCharacterAgent agent)
        {
            return agent != null && agent.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 用青蓝色世界线条把 AI 和其当前目标连接起来。
        /// </summary>
        private void DrawWorldDebug()
        {
            if (!drawTargetLine)
            {
                return;
            }

            foreach (BehaviorTreeCharacterAgent agent in trackedAgents.Where(IsAgentUsable))
            {
                BehaviorTreeBlackboard blackboard = agent.Blackboard;
                if (blackboard == null)
                {
                    continue;
                }

                bool hasTarget = blackboard.GetValueOrDefault<bool>(BehaviorTreeCharacterBlackboardKeys.HasTarget, false);
                if (!hasTarget)
                {
                    continue;
                }

                Vector3 targetPosition = new Vector3(
                    blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.TargetPositionX, 0f),
                    blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.TargetPositionY, 0f),
                    blackboard.GetValueOrDefault<float>(BehaviorTreeCharacterBlackboardKeys.TargetPositionZ, 0f));

                Vector3 origin = agent.transform.position + Vector3.up * 1.2f;
                Debug.DrawLine(origin, targetPosition + Vector3.up * 1.2f, new Color(0.2f, 0.6f, 1f), 0f, false);
            }
        }

        /// <summary>
        /// 初始化 HUD 的 GUI 样式。
        /// </summary>
        private void EnsureStyles()
        {
            if (panelTexture == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.18f, 0.18f, 0.18f, 0.72f));
                panelTexture.Apply();
            }

            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = panelTexture, textColor = new Color(0.87f, 0.94f, 1f) },
                    padding = new RectOffset(10, 10, 8, 8)
                };
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                    normal = { textColor = new Color(0.92f, 0.94f, 0.98f) }
                };
            }

            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = false,
                    wordWrap = true,
                    fontSize = 12,
                    normal = { textColor = new Color(0.88f, 0.9f, 0.94f) }
                };
            }
        }

        /// <summary>
        /// 构造单个 AI 的调试文本。
        /// </summary>
        /// <param name="agent">要显示的 AI 代理。</param>
        /// <returns>格式化后的调试文本。</returns>
        private string BuildAgentDebugText(BehaviorTreeCharacterAgent agent)
        {
            builder.Clear();

            BehaviorTreeInstance instance = agent.Instance;
            CharacterStateToText(agent, instance);
            BlackboardToText(agent.Blackboard);
            RunningNodesToText(instance);

            return builder.ToString();
        }

        /// <summary>
        /// 追加当前树状态、角色状态和输入信息。
        /// </summary>
        /// <param name="agent">目标 AI 代理。</param>
        /// <param name="instance">对应的行为树实例。</param>
        private void CharacterStateToText(BehaviorTreeCharacterAgent agent, BehaviorTreeInstance instance)
        {
            builder.AppendLine($"Tree Asset: {agent.BehaviorTree?.name ?? "<none>"}");
            builder.AppendLine($"Tree Status: {instance?.CurrentStatus.ToString() ?? "<none>"}");
            builder.AppendLine($"Current Node: {ResolveCurrentNodeLabel(instance)}");
            builder.AppendLine($"Tick / Elapsed: {instance?.Context.TickCount ?? 0} / {(instance?.Context.ElapsedTime ?? 0f):0.00}s");
            builder.AppendLine($"Character State: {agent.CurrentCharacterStateName}");

            Vector3 worldPosition = agent.Character != null ? agent.Character.transform.position : agent.transform.position;
            builder.AppendLine($"World Pos: ({worldPosition.x:0.00}, {worldPosition.y:0.00}, {worldPosition.z:0.00})");

            AIInputProvider inputProvider = agent.InputProvider;
            Vector2 movementInput = inputProvider != null ? inputProvider.GetMovementDirection() : Vector2.zero;
            builder.AppendLine();
        }

        /// <summary>
        /// 追加黑板值显示。
        /// </summary>
        /// <param name="blackboard">要显示的黑板。</param>
        private void BlackboardToText(BehaviorTreeBlackboard blackboard)
        {
            builder.AppendLine("Blackboard");
            if (blackboard == null)
            {
                builder.AppendLine("  <null>");
                builder.AppendLine();
                return;
            }

            int count = 0;
            foreach (KeyValuePair<string, object> entry in blackboard.Entries.OrderBy(pair => pair.Key))
            {
                if (count >= maxBlackboardEntries)
                {
                    builder.AppendLine($"  ... ({blackboard.Count - maxBlackboardEntries} more)");
                    break;
                }

                string typeName = blackboard.GetRegisteredValueType(entry.Key)?.ToString() ?? "Unknown";
                builder.AppendLine($"  {entry.Key} [{typeName}] = {FormatValue(entry.Value)}");
                count++;
            }

            builder.AppendLine();
        }

        /// <summary>
        /// 追加当前运行中的节点列表。
        /// </summary>
        /// <param name="instance">行为树实例。</param>
        private void RunningNodesToText(BehaviorTreeInstance instance)
        {
            if (!showRunningNodes)
            {
                return;
            }

            builder.AppendLine("Running Nodes");
            if (instance == null)
            {
                builder.AppendLine("  <no instance>");
                return;
            }

            List<BehaviorTreeNodeRuntimeSnapshot> runningNodes = instance.GetNodeStates()
                .Where(snapshot => snapshot.IsRunning)
                .OrderBy(snapshot => snapshot.LastVisitedTick)
                .ToList();

            if (runningNodes.Count == 0)
            {
                builder.AppendLine("  <none>");
                return;
            }

            foreach (BehaviorTreeNodeRuntimeSnapshot snapshot in runningNodes)
            {
                builder.AppendLine(
                    $"  {ResolveNodeLabel(instance, snapshot.NodeId)} | status={snapshot.LastStatus} | child={snapshot.ActiveChildIndex} | active={snapshot.ActiveDuration:0.00}s");
            }
        }

        /// <summary>
        /// 解析当前节点的标题文本。
        /// </summary>
        /// <param name="instance">行为树实例。</param>
        /// <returns>当前节点标题。</returns>
        private static string ResolveCurrentNodeLabel(BehaviorTreeInstance instance)
        {
            if (instance?.Context == null || string.IsNullOrWhiteSpace(instance.Context.CurrentNodeId))
            {
                return "<none>";
            }

            return ResolveNodeLabel(instance, instance.Context.CurrentNodeId);
        }

        /// <summary>
        /// 根据节点 ID 解析节点标题与类型。
        /// </summary>
        /// <param name="instance">行为树实例。</param>
        /// <param name="nodeId">节点 ID。</param>
        /// <returns>格式化后的节点名称。</returns>
        private static string ResolveNodeLabel(BehaviorTreeInstance instance, string nodeId)
        {
            if (instance?.Definition?.Nodes == null || string.IsNullOrWhiteSpace(nodeId))
            {
                return nodeId ?? "<none>";
            }

            BehaviorTreeDefinitionNode node = instance.Definition.Nodes.FirstOrDefault(candidate => candidate != null && candidate.NodeId == nodeId);
            if (node == null)
            {
                return nodeId;
            }

            return $"{node.Title} ({node.NodeKind})";
        }

        /// <summary>
        /// 把调试值格式化为可读文本。
        /// </summary>
        /// <param name="value">待格式化的值。</param>
        /// <returns>格式化后的文本。</returns>
        private static string FormatValue(object value)
        {
            return value switch
            {
                null => "<null>",
                float floatValue => floatValue.ToString("0.###"),
                Vector2 vector2 => $"({vector2.x:0.##}, {vector2.y:0.##})",
                Vector3 vector3 => $"({vector3.x:0.##}, {vector3.y:0.##}, {vector3.z:0.##})",
                _ => value.ToString()
            };
        }
    }
}
