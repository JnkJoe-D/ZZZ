#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Framework.Editor
{
    /// <summary>
    /// GLog 编辑器配置与标签过滤控制台窗口。
    /// 
    /// 提供以下分页功能：
    /// 1. 【标签过滤】：全局日志级别切换 (MinLevel)、运行时模块标签黑白名单过滤与搜索、快速测试日志面板。
    /// 2. 【模块配色】：为各模块标签定制在 Unity 控制台中显示的富文本颜色、实时效果预览、重置默认。
    /// 3. 【文件日志】：运行期持久化日志目录快捷打开、立即刷新与历史旧日志清理。
    /// </summary>
    public sealed class GLogConsoleWindow : EditorWindow
    {
        private int _currentTab;
        private readonly string[] _tabTitles = new[] { "标签过滤与控制", "模块颜色配置", "文件日志管理" };

        private Vector2 _filterScrollPos;
        private Vector2 _colorScrollPos;
        private string _filterSearch = "";
        private string _colorSearch = "";

        private readonly List<TagItem> _tags = new List<TagItem>();
        private GUIStyle _previewStyle;

        private class TagItem
        {
            public string Name;
            public string Category;
            public bool Enabled;
        }

        [MenuItem("Window/Framework/GLog 控制台与标签过滤", false, 200)]
        public static void Open()
        {
            var window = GetWindow<GLogConsoleWindow>("GLog 控制台");
            window.minSize = new Vector2(460, 560);
            window.Show();
        }

        private void OnEnable()
        {
            InitTagList();
        }

        private void InitTagList()
        {
            _tags.Clear();

            // 提取 LogTags 类中的所有常量与分类
            var fields = typeof(LogTags).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var f in fields)
            {
                if (f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                {
                    string tagName = (string)f.GetValue(null);
                    string category = InferCategory(tagName);

                    _tags.Add(new TagItem
                    {
                        Name = tagName,
                        Category = category,
                        Enabled = GLog.IsTagEnabled(tagName)
                    });
                }
            }
        }

        private static string InferCategory(string tagName)
        {
            switch (tagName)
            {
                case LogTags.Framework:
                case LogTags.GameRoot:
                case LogTags.Resource:
                case LogTags.Scene:
                case LogTags.Pool:
                case LogTags.FSM:
                case LogTags.Event:
                case LogTags.Config:
                case LogTags.Audio:
                    return "框架层 (Framework)";

                case LogTags.Network:
                case LogTags.Tcp:
                case LogTags.Udp:
                case LogTags.Heartbeat:
                    return "网络层 (Network)";

                case LogTags.Combat:
                case LogTags.Action:
                case LogTags.Input:
                case LogTags.Skill:
                case LogTags.Buff:
                case LogTags.AI:
                case LogTags.Team:
                case LogTags.Monster:
                case LogTags.Player:
                    return "玩法层 (Gameplay)";

                case LogTags.UI:
                case LogTags.Camera:
                case LogTags.VFX:
                case LogTags.Anim:
                    return "表现层 (Presentation)";

                case LogTags.ATEditor:
                case LogTags.BehaviorTree:
                    return "编辑器工具 (Tools)";

                default:
                    return "其他 (General)";
            }
        }

        private void OnGUI()
        {
            if (_previewStyle == null)
            {
                _previewStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    fontSize = 12
                };
            }

            DrawHeader();
            EditorGUILayout.Space(4);

            _currentTab = GUILayout.Toolbar(_currentTab, _tabTitles, GUILayout.Height(28));
            EditorGUILayout.Space(6);

            switch (_currentTab)
            {
                case 0:
                    DrawTabFilter();
                    break;
                case 1:
                    DrawTabColors();
                    break;
                case 2:
                    DrawTabFileLogs();
                    break;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("GLog 日志系统控制面板", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("支持日志分级过滤、模块动态静音、控制台标签富文本配色与文件日志管理。", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════
        //  Tab 0: 标签过滤与控制
        // ═══════════════════════════════════════

        private void DrawTabFilter()
        {
            // 全局级别设置
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("全局级别配置", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newLevel = (LogLevel)EditorGUILayout.EnumPopup("全局最低级别 (MinLevel)", GLog.MinLevel);
            if (EditorGUI.EndChangeCheck())
            {
                GLog.SetMinLevel(newLevel);
            }

            EditorGUILayout.HelpBox(
                $"低于 [{GLog.MinLevel}] 的日志将被运行时丢弃。\n" +
                $"Release 构建时 Trace/Debug/Info 由 [Conditional] 彻底剥离，Warning/Error 始终保留。",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 标签黑白名单
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("标签动态静音 / 过滤", EditorStyles.boldLabel);

            if (GUILayout.Button("全部启用", EditorStyles.miniButtonLeft, GUILayout.Width(65)))
            {
                foreach (var t in _tags)
                {
                    t.Enabled = true;
                    GLog.EnableTag(t.Name);
                }
            }

            if (GUILayout.Button("全部静音", EditorStyles.miniButtonRight, GUILayout.Width(65)))
            {
                foreach (var t in _tags)
                {
                    t.Enabled = false;
                    GLog.DisableTag(t.Name);
                }
            }
            EditorGUILayout.EndHorizontal();

            // 搜索框
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _filterSearch = EditorGUILayout.TextField(_filterSearch, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                _filterSearch = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            _filterScrollPos = EditorGUILayout.BeginScrollView(_filterScrollPos, GUILayout.Height(220));

            string currentCategory = null;
            foreach (var tagItem in _tags)
            {
                if (!string.IsNullOrEmpty(_filterSearch) &&
                    tagItem.Name.IndexOf(_filterSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (tagItem.Category != currentCategory)
                {
                    currentCategory = tagItem.Category;
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField($"—— {currentCategory} ——", EditorStyles.miniBoldLabel);
                }

                EditorGUI.BeginChangeCheck();
                bool enabled = EditorGUILayout.ToggleLeft($"[{tagItem.Name}]", tagItem.Enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    tagItem.Enabled = enabled;
                    if (enabled) GLog.EnableTag(tagItem.Name);
                    else GLog.DisableTag(tagItem.Name);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 测试发送
            DrawTestSection();
        }

        // ═══════════════════════════════════════
        //  Tab 1: 模块颜色配置
        // ═══════════════════════════════════════

        private void DrawTabColors()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("控制台模块标签富文本着色", EditorStyles.boldLabel);

            if (GUILayout.Button("恢复全部默认配色", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定将所有模块标签颜色恢复为默认预设吗？", "确定", "取消"))
                {
                    LogColorConfig.ResetAllToDefault();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "为各模块标签定制在 Unity Console 中显示的颜色。修改即时生效并保存于本地 EditorPrefs 中。\n" +
                "文件日志 (FileLog) 将继续保持纯文本无富文本标记，确保真机日志整洁易查。",
                MessageType.None);

            // 搜索框
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _colorSearch = EditorGUILayout.TextField(_colorSearch, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                _colorSearch = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            _colorScrollPos = EditorGUILayout.BeginScrollView(_colorScrollPos);

            string currentCategory = null;
            foreach (var tagItem in _tags)
            {
                if (!string.IsNullOrEmpty(_colorSearch) &&
                    tagItem.Name.IndexOf(_colorSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (tagItem.Category != currentCategory)
                {
                    currentCategory = tagItem.Category;
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField($"—— {currentCategory} ——", EditorStyles.miniBoldLabel);
                }

                EditorGUILayout.BeginHorizontal();

                // 标签名字
                EditorGUILayout.LabelField(tagItem.Name, GUILayout.Width(100));

                // 拾色器
                Color currentColor = LogColorConfig.GetTagColor(tagItem.Name);
                EditorGUI.BeginChangeCheck();
                Color newColor = EditorGUILayout.ColorField(GUIContent.none, currentColor, false, false, false, GUILayout.Width(50));
                if (EditorGUI.EndChangeCheck())
                {
                    LogColorConfig.SetTagColor(tagItem.Name, newColor);
                }

                // 实时效果预览
                string previewTag = LogColorConfig.GetColoredTag(tagItem.Name);
                EditorGUILayout.LabelField($"{previewTag} 示例日志", _previewStyle, GUILayout.MinWidth(150));

                // 单项重置按钮
                bool hasCustom = LogColorConfig.HasCustomColor(tagItem.Name);
                EditorGUI.BeginDisabledGroup(!hasCustom);
                if (GUILayout.Button("重置", EditorStyles.miniButton, GUILayout.Width(42)))
                {
                    LogColorConfig.ResetTagColor(tagItem.Name);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════
        //  Tab 2: 文件日志管理
        // ═══════════════════════════════════════

        private void DrawTabFileLogs()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("文件日志持久化 (FileLog)", EditorStyles.boldLabel);

            string logDir = Path.Combine(Application.persistentDataPath, "Logs");
            EditorGUILayout.LabelField("真机持久化路径:", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(logDir, EditorStyles.textField, GUILayout.Height(18));

            EditorGUILayout.HelpBox(
                "文件日志仅在打包后的程序 (Standalone / Mobile) 运行阶段自动激活并后台写入，编辑器下默认不产生文件。\n" +
                "每次启动自动独立建文件，并自动滚动清理超出保留数量的历史日志（默认保留最近 7 个）。",
                MessageType.Info);

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打开日志文件夹", GUILayout.Height(26)))
            {
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                Process.Start("explorer.exe", logDir.Replace('/', '\\'));
            }

            if (GUILayout.Button("立即刷新 (Flush)", GUILayout.Height(26)))
            {
                GLog.Flush();
            }

            if (GUILayout.Button("清理旧日志", GUILayout.Height(26)))
            {
                if (Directory.Exists(logDir))
                {
                    int deleted = 0;
                    foreach (var f in Directory.GetFiles(logDir, "log_*.txt"))
                    {
                        try { File.Delete(f); deleted++; }
                        catch { /* ignore */ }
                    }
                    EditorUtility.DisplayDialog("提示", $"已清理 {deleted} 个历史日志文件。", "确定");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTestSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("发送测试日志 (验证着色与过滤)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Debug (框架)"))
            {
                GLog.Debug(LogTags.Framework, "这是一条 GLog.Debug 测试消息。");
            }
            if (GUILayout.Button("Info (战斗)"))
            {
                GLog.Info(LogTags.Combat, "这是战斗模块伤害结算测试消息: 1250。");
            }
            if (GUILayout.Button("Warning (网络)"))
            {
                GLog.Warning(LogTags.Network, "网络心跳存在 120ms 波动警告。");
            }
            if (GUILayout.Button("Error (UI)"))
            {
                GLog.Error(LogTags.UI, "UI 面板资源加载失败测试消息。");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
