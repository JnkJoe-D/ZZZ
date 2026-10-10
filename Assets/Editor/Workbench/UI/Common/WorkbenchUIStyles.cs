using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - UI 统一视觉样式与固定布局常量规范
    /// 集中管理固定尺寸、卡片容器与颜色徽标，杜绝文本长度波动引起的界面跳变。
    /// </summary>
    public static class WorkbenchUIStyles
    {
        // 固定尺寸规范常量
        public const float LabelWidth = 175f;
        public const float FixedButtonSmall = 60f;
        public const float FixedButtonMedium = 85f;
        public const float FixedButtonLarge = 110f;
        public const float FixedRowHeight = 26f;

        // 表格固定列宽规范
        public const float ColIndexWidth = 36f;
        public const float ColNameWidth = 240f;
        public const float ColStatusWidth = 125f;
        public const float ColPathWidth = 280f;
        public const float ColActionWidth = 90f;

        private static GUIStyle _cardBoxStyle;
        private static GUIStyle _cardHeaderStyle;
        private static GUIStyle _badgeStyle;
        private static GUIStyle _tableHeaderStyle;
        private static GUIStyle _tableRowStyle;
        private static GUIStyle _tableRowAltStyle;

        public static GUIStyle CardBox
        {
            get
            {
                if (_cardBoxStyle == null)
                {
                    _cardBoxStyle = new GUIStyle("HelpBox")
                    {
                        padding = new RectOffset(12, 12, 8, 10),
                        margin = new RectOffset(4, 4, 4, 6)
                    };
                }
                return _cardBoxStyle;
            }
        }

        public static GUIStyle CardHeader
        {
            get
            {
                if (_cardHeaderStyle == null)
                {
                    _cardHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        margin = new RectOffset(0, 0, 0, 4)
                    };
                }
                return _cardHeaderStyle;
            }
        }

        private static GUIStyle _cellLabel;
        public static GUIStyle CellLabel => _cellLabel ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(2, 2, 0, 0)
        };

        private static GUIStyle _cellBoldLabel;
        public static GUIStyle CellBoldLabel => _cellBoldLabel ??= new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(2, 2, 0, 0)
        };

        private static GUIStyle _cellMiniLabel;
        public static GUIStyle CellMiniLabel => _cellMiniLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(2, 2, 0, 0)
        };

        private static GUIStyle _cellMiniBoldLabel;
        public static GUIStyle CellMiniBoldLabel => _cellMiniBoldLabel ??= new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(2, 2, 0, 0)
        };

        public static GUIStyle TableHeader
        {
            get
            {
                if (_tableHeaderStyle == null)
                {
                    _tableHeaderStyle = new GUIStyle(EditorStyles.toolbarButton)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                        fixedHeight = 22f
                    };
                }
                return _tableHeaderStyle;
            }
        }

        public static GUIStyle TableRow
        {
            get
            {
                if (_tableRowStyle == null)
                {
                    _tableRowStyle = new GUIStyle(GUIStyle.none)
                    {
                        fixedHeight = FixedRowHeight,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(4, 4, 2, 2)
                    };
                }
                return _tableRowStyle;
            }
        }

        public static GUIStyle BadgeStyle
        {
            get
            {
                if (_badgeStyle == null)
                {
                    _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Color.white }
                    };
                }
                return _badgeStyle;
            }
        }

        /// <summary>
        /// 绘制四级来源固定徽标（固定 80px 宽，固定 18px 高）
        /// </summary>
        public static void DrawSourceBadge(ConfigSource source, float width = 80f)
        {
            Color color;
            string text;
            switch (source)
            {
                case ConfigSource.TaskOverride:
                    color = new Color(0.6f, 0.3f, 0.8f, 0.9f); // 紫色
                    text = "任务临时";
                    break;
                case ConfigSource.CharacterOverride:
                    color = new Color(0.9f, 0.5f, 0.1f, 0.9f); // 橙色
                    text = "角色覆盖";
                    break;
                case ConfigSource.WorkspaceInherited:
                    color = new Color(0.2f, 0.6f, 0.35f, 0.9f); // 绿色
                    text = "工作区继承";
                    break;
                case ConfigSource.SystemDefault:
                default:
                    color = new Color(0.45f, 0.45f, 0.45f, 0.9f); // 灰色
                    text = "系统默认";
                    break;
            }

            var rect = GUILayoutUtility.GetRect(width, 18f, GUILayout.Width(width), GUILayout.Height(18f));
            EditorGUI.DrawRect(rect, color);
            GUI.Label(rect, text, BadgeStyle);
        }

        /// <summary>
        /// 绘制状态固定徽标（固定宽度，固定高度）
        /// </summary>
        public static void DrawStatusBadge(bool ok, string text, float width = 75f)
        {
            Color color = ok
                ? new Color(0.18f, 0.58f, 0.32f, 0.9f) // 绿
                : new Color(0.85f, 0.25f, 0.2f, 0.9f); // 红

            var rect = GUILayoutUtility.GetRect(width, 18f, GUILayout.Width(width), GUILayout.Height(18f));
            EditorGUI.DrawRect(rect, color);
            GUI.Label(rect, text, BadgeStyle);
        }

        /// <summary>
        /// 绘制多维映射状态固定徽标（固定宽度，固定高度 18px，带 Tooltip 提示）
        /// </summary>
        public static void DrawMappingStatusBadge(ActionMappingStatus status, float width = 145f)
        {
            Color color;
            string text;
            string tooltip;

            switch (status)
            {
                case ActionMappingStatus.ExactMatch:
                    color = new Color(0.18f, 0.62f, 0.32f, 0.95f);
                    text = "完全匹配";
                    tooltip = "名称与数字编号 ID 均完全对齐";
                    break;
                case ActionMappingStatus.FuzzyNameExactId:
                    color = new Color(0.15f, 0.55f, 0.75f, 0.95f);
                    text = "名称模糊 / Id 匹配";
                    tooltip = "数字 ID 完全一致，名称存在前缀或修饰语差异";
                    break;
                case ActionMappingStatus.ExactNameDiffId:
                    color = new Color(0.85f, 0.55f, 0.15f, 0.95f);
                    text = "名称匹配 / Id 不等";
                    tooltip = "名称格式吻合，但 Action 资产内 ID 尚未写入或配表 ID 变动";
                    break;
                case ActionMappingStatus.FuzzyNameDiffId:
                    color = new Color(0.7f, 0.35f, 0.55f, 0.95f);
                    text = "名称模糊 / Id 不等";
                    tooltip = "名称存在相似度，但双方 ID 不相等或未写入";
                    break;
                case ActionMappingStatus.NoMatch:
                default:
                    color = new Color(0.65f, 0.25f, 0.2f, 0.95f);
                    text = "无匹配资产";
                    tooltip = "工程中未检测到具备合理置信度的 Action 资产，需新建生成";
                    break;
            }

            var rect = GUILayoutUtility.GetRect(width, 18f, GUILayout.Width(width), GUILayout.Height(18f));
            EditorGUI.DrawRect(rect, color);
            GUI.Label(rect, new GUIContent(text, tooltip), BadgeStyle);
        }

        private static GUIStyle _readOnlyTextStyle;
        private static GUIStyle _statTitleStyle;

        public static GUIStyle ReadOnlyTextStyle
        {
            get
            {
                if (_readOnlyTextStyle == null)
                {
                    _readOnlyTextStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 12,
                        clipping = TextClipping.Clip
                    };
                }
                return _readOnlyTextStyle;
            }
        }

        public static GUIStyle StatTitleStyle
        {
            get
            {
                if (_statTitleStyle == null)
                {
                    _statTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        margin = new RectOffset(0, 0, 2, 0)
                    };
                }
                return _statTitleStyle;
            }
        }

        /// <summary>
        /// 绘制统计数据卡片（抗挤压、抗跳变、防止中文字体截断）
        /// </summary>
        public static void DrawStatCard(string title, string countText, string subText, float width, Color accentColor)
        {
            EditorGUILayout.BeginVertical(CardBox, GUILayout.Width(width), GUILayout.Height(76f));
            {
                var rect = GUILayoutUtility.GetRect(width - 24, 3f, GUILayout.Height(3f));
                EditorGUI.DrawRect(rect, accentColor);
                GUILayout.Space(2f);

                EditorGUILayout.LabelField(title, StatTitleStyle, GUILayout.Height(18f));
                EditorGUILayout.LabelField(countText, EditorStyles.boldLabel, GUILayout.Height(20f));
                if (!string.IsNullOrEmpty(subText))
                {
                    EditorGUILayout.LabelField(subText, EditorStyles.miniLabel, GUILayout.Height(14f));
                }
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制明确的只读属性行（去除文本输入框的伪编辑边框，附带快捷复制按钮）
        /// </summary>
        public static void DrawReadOnlyField(GUIContent label, string value, ConfigSource? source = null, float labelWidth = LabelWidth)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
            {
                if (label != null)
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
                }

                // 只读文本显示区（平整浅色底衬，彻底摆脱可编辑 textField 样式）
                Rect textRect = EditorGUILayout.GetControlRect(false, 20f, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(textRect, new Color(0.15f, 0.15f, 0.15f, 0.45f));
                Rect labelInner = new Rect(textRect.x + 6f, textRect.y, textRect.width - 12f, textRect.height);
                GUI.Label(labelInner, value ?? string.Empty, ReadOnlyTextStyle);

                // 快捷复制按钮
                if (GUILayout.Button("复制", EditorStyles.miniButton, GUILayout.Width(42f), GUILayout.Height(18f)))
                {
                    EditorGUIUtility.systemCopyBuffer = value ?? string.Empty;
                }

                if (source.HasValue)
                {
                    DrawSourceBadge(source.Value, 80f);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawReadOnlyField(string label, string value, ConfigSource? source = null, float labelWidth = LabelWidth)
        {
            DrawReadOnlyField(new GUIContent(label), value, source, labelWidth);
        }

        /// <summary>
        /// 开始卡片分组
        /// </summary>
        public static void BeginCard(string headerTitle)
        {
            EditorGUILayout.BeginVertical(CardBox);
            if (!string.IsNullOrEmpty(headerTitle))
            {
                EditorGUILayout.LabelField(headerTitle, CardHeader);
                EditorGUILayout.Space(2f);
            }
        }

        /// <summary>
        /// 结束卡片分组
        /// </summary>
        public static void EndCard()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }
}
