using System.Linq;
using Game.Input;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    public class CharacterDebugHUD : MonoBehaviour
    {
        [SerializeField] private CharacterEntity targetEntity;

        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle historyStyle;
        private GUIStyle heldActiveStyle;
        private GUIStyle heldInactiveStyle;

        private Texture2D backgroundTexture;
        private Texture2D heldBgTexture;

        // 所有可被 Held 的按键类型
        private static readonly (InputCommand type, string label)[] HeldKeyDefs =
        {
            (InputCommand.Move, "Move"),
            (InputCommand.Evade, "Evade"),
            (InputCommand.BasicAttack, "Attack"),
            (InputCommand.SpecialAttack, "Special"),
        };

        private void Start()
        {
            if (targetEntity == null)
            {
                targetEntity = GetComponent<CharacterEntity>();
                if (targetEntity == null)
                {
                    targetEntity = GameObject.FindWithTag("Player")?.GetComponent<CharacterEntity>();
                }
            }
        }

        private static bool isHudVisible = false;

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                isHudVisible = !isHudVisible;
            }
        }

        private void InitStyles()
        {
            if (boxStyle != null)
            {
                return;
            }

            backgroundTexture = CreateRoundedTex(128, 128, 15, new Color(0.12f, 0.12f, 0.12f, 0.85f));
            heldBgTexture = CreateRoundedTex(128, 128, 15, new Color(0.1f, 0.1f, 0.15f, 0.9f));

            boxStyle = new GUIStyle
            {
                normal = { background = backgroundTexture },
                padding = new RectOffset(15, 15, 15, 15)
            };

            labelStyle = new GUIStyle
            {
                normal = { textColor = Color.white },
                fontSize = 14,
                margin = new RectOffset(0, 0, 2, 2)
            };

            titleStyle = new GUIStyle(labelStyle)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16
            };
            titleStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);

            historyStyle = new GUIStyle(labelStyle)
            {
                fontSize = 13
            };
            historyStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            heldActiveStyle = new GUIStyle(labelStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            heldActiveStyle.normal.textColor = new Color(0.3f, 1f, 0.5f);

            heldInactiveStyle = new GUIStyle(heldActiveStyle);
            heldInactiveStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
            heldInactiveStyle.fontStyle = FontStyle.Normal;
        }

        private void OnGUI()
        {
            if (!isHudVisible) return;

            if (targetEntity == null)
            {
                return;
            }

            InitStyles();

            DrawHeldKeysPanel();
            DrawMainPanel();
        }

        private void DrawHeldKeysPanel()
        {
            var provider = targetEntity.InputProvider;
            if (provider == null) return;

            float panelWidth = 160;
            float lineH = 22;
            float panelHeight = 40 + HeldKeyDefs.Length * lineH;
            float margin = 20;
            Rect rect = new Rect(margin, margin, panelWidth, panelHeight);

            var heldBoxStyle = new GUIStyle
            {
                normal = { background = heldBgTexture },
                padding = new RectOffset(12, 12, 10, 10)
            };

            GUILayout.BeginArea(rect, heldBoxStyle);
            {
                GUILayout.Label("HELD KEYS", titleStyle);
                GUILayout.Space(4);

                foreach (var (type, label) in HeldKeyDefs)
                {
                    bool isHeld = provider.IsHeld((int)type);
                    string prefix = isHeld ? "●" : "○";
                    GUILayout.Label($" {prefix}  {label}", isHeld ? heldActiveStyle : heldInactiveStyle);
                }
            }
            GUILayout.EndArea();
        }

        private void DrawMainPanel()
        {
            float width = 360;
            float height = 750;
            float margin = 20;
            Rect rect = new Rect(Screen.width - width - margin, margin, width, height);

            GUILayout.BeginArea(rect, boxStyle);
            {
                GUILayout.Label("CHARACTER DEBUG HUD", titleStyle);
                GUILayout.Space(10);

                var machine = targetEntity.Machine;
                if (machine != null)
                {
                    DrawInfo("Current State", machine.CurrentState?.GetType().Name ?? "None");
                    DrawInfo("Previous State", machine.PreviousState?.GetType().Name ?? "None", new Color(0.7f, 0.7f, 0.7f));
                    DrawInfo("Ground SubState", GetGroundSubStateLabel(machine.CurrentState), new Color(0.55f, 0.9f, 0.65f));
                }

                DrawInfo(
                    "Target Ground",
                    targetEntity.RuntimeData?.TargetGroundSubState.ToString() ?? "None",
                    new Color(0.75f, 0.9f, 1f));


                DrawInfo(
                    "Last Route",
                    targetEntity.RuntimeData == null
                        ? "None"
                        : $"{targetEntity.RuntimeData.LastRouteSource} / {targetEntity.RuntimeData.LastResolvedCommandType}/{targetEntity.RuntimeData.LastResolvedCommandPhase}",
                    new Color(1f, 0.8f, 0.45f));
                DrawInfo(
                    "Route Detail",
                    targetEntity.RuntimeData == null
                        ? "None"
                        : $"{targetEntity.RuntimeData.LastRouteTag ?? "-"} / {targetEntity.RuntimeData.LastResolvedActionId}",
                    new Color(0.85f, 0.85f, 0.85f));

                GUILayout.Space(15);
                GUILayout.Label("COMMAND BUFFER", titleStyle);
                if (targetEntity.CommandBuffer != null)
                {
                    var commands = targetEntity.CommandBuffer.GetUnconsumedCommands();
                    if (!commands.Any())
                    {
                        GUILayout.Label("  (Empty)", historyStyle);
                    }
                    else
                    {
                        foreach (CharacterCommand command in commands)
                        {
                            DrawInfo($"> {command.Type}/{command.Phase}", $"{(Time.time - command.Timestamp):F2}s ago", Color.yellow);
                        }
                    }
                }

                GUILayout.Space(15);
                GUILayout.Label("EXECUTION HISTORY (Latest 10)", titleStyle);
                if (targetEntity.ActionController != null)
                {
                    var history = targetEntity.ActionController.ExecutionHistory;
                    if (history.Count == 0)
                    {
                        GUILayout.Label("  (No records)", historyStyle);
                    }
                    else
                    {
                        for (int i = 0; i < history.Count; i++)
                        {
                            var record = history[i];
                            string timeStr = record.Timestamp.ToString("F1");
                            string triggerStr = record.Type == InputCommand.None 
                                ? "<color=#ffb366>AutoCondition</color>" 
                                : $"{record.Type}/{record.Phase}";

                            GUILayout.Label(
                                $"<color=#aaaaaa>[{timeStr}]</color> {triggerStr} {record.Source} {record.RouteTag ?? "-"} <color=#66ccff>-></color> {record.ActionId}",
                                historyStyle);
                        }
                    }
                }
                GUILayout.Space(15);
                GUILayout.Label("STATUS & BUFFS", titleStyle);
                if (targetEntity.StatusModule != null)
                {
                    // Attributes
                    var attrSet = targetEntity.StatusModule.Attributes;
                    if (attrSet != null)
                    {
                        foreach (AttributeId attrId in System.Enum.GetValues(typeof(AttributeId)))
                        {
                            if (attrId == AttributeId.None) continue;
                            if (attrSet.Has(attrId))
                            {
                                float current = attrSet.GetCurrent(attrId);
                                float final = attrSet.GetFinal(attrId);
                                DrawInfo($"- {attrId}", $"{current:F1} / {final:F1}", new Color(0.6f, 0.9f, 0.6f));
                            }
                        }
                    }

                    // Buffs
                    var buffs = targetEntity.StatusModule.Buffs;
                    if (buffs != null && buffs.ActiveBuffs.Count > 0)
                    {
                        GUILayout.Space(5);
                        foreach (var buff in buffs.ActiveBuffs)
                        {
                            string buffName = buff.Definition != null ? buff.Definition.DisplayName : "Unknown";
                            string timeStr = buff.IsPermanent ? "Permanent" : $"{buff.RemainingTime:F1}s";
                            DrawInfo($"+ [{buffName}]", $"Stack: {buff.CurrentStack} | {timeStr}", new Color(0.9f, 0.7f, 0.9f));
                        }
                    }
                    else if (buffs != null)
                    {
                        GUILayout.Label("  (No active buffs)", historyStyle);
                    }
                }
                else
                {
                    GUILayout.Label("  (StatusModule not init)", historyStyle);
                }
            }
            GUILayout.EndArea();
        }

        private void DrawInfo(string label, string value, Color? valueColor = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", labelStyle, GUILayout.Width(110));
            Color oldColor = GUI.color;
            if (valueColor.HasValue)
            {
                GUI.color = valueColor.Value;
            }

            GUILayout.Label(value, labelStyle);
            GUI.color = oldColor;
            GUILayout.EndHorizontal();
        }

        private static string GetGroundSubStateLabel(object currentState)
        {
            if (currentState is not CharacterGroundState groundState)
            {
                return "-";
            }

            GroundSubState subState = groundState.CurrentSubState;
            return subState != null ? subState.GetType().Name : "None";
        }

        private Texture2D CreateRoundedTex(int width, int height, int radius, Color color)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] cols = new Color[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cols[y * width + x] = IsInsideRoundedRect(x, y, width, height, radius) ? color : Color.clear;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private bool IsInsideRoundedRect(int x, int y, int w, int h, int r)
        {
            if (x < r && y < r)
            {
                return Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) <= r;
            }

            if (x > w - r && y < r)
            {
                return Vector2.Distance(new Vector2(x, y), new Vector2(w - r, r)) <= r;
            }

            if (x < r && y > h - r)
            {
                return Vector2.Distance(new Vector2(x, y), new Vector2(r, h - r)) <= r;
            }

            if (x > w - r && y > h - r)
            {
                return Vector2.Distance(new Vector2(x, y), new Vector2(w - r, h - r)) <= r;
            }

            return true;
        }
    }
}
