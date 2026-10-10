using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Game.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Framework
{
    // 命中效果编号属性绘制器
    // 支持按角色工作区上下文过滤专属条目 并支持一键切换全局列表
    [CustomPropertyDrawer(typeof(HitEffectIdAttribute))]
    public class HitEffectIdDrawer : PropertyDrawer
    {
        private static string[] _allHitEffectNames;
        private static int[] _allHitEffectIds;
        private static readonly Dictionary<string, (string[] names, int[] ids)> _scopedCache = new Dictionary<string, (string[] names, int[] ids)>(StringComparer.OrdinalIgnoreCase);

        // 记录属性实例是否强制查看全局
        private static bool _globalViewMode = false;

        // 常用角色中英文对照别名字典
        private static readonly Dictionary<string, string[]> _characterAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Ellen", new string[] { "Ellen", "艾莲", "艾莲乔" } },
            { "QingYi", new string[] { "QingYi", "青衣" } },
            { "Anby", new string[] { "Anby", "安比" } },
            { "Miyabi", new string[] { "Miyabi", "星见雅" } },
            { "TyrfingInfested", new string[] { "Tyrfing", "提尔锋" } },
            { "Unagi", new string[] { "Unagi", "鳗鱼" } }
        };

        // 加载全部配表数据
        public static void LoadHitEffectData()
        {
            string path = "Assets/Configs/zzz_tbhiteffect.json";
            _scopedCache.Clear();

            if (!File.Exists(path))
            {
                _allHitEffectNames = Array.Empty<string>();
                _allHitEffectIds = Array.Empty<int>();
                return;
            }

            try
            {
                var lines = File.ReadAllLines(path);
                var namesList = new List<string>();
                var idsList = new List<int>();

                namesList.Add("无效果 0");
                idsList.Add(0);

                int currentId = 0;
                foreach (var line in lines)
                {
                    if (line.Contains("\"id\":"))
                    {
                        string idStr = Regex.Match(line, @"\d+").Value;
                        if (int.TryParse(idStr, out int id))
                        {
                            currentId = id;
                        }
                    }
                    else if (line.Contains("\"name\":"))
                    {
                        var match = Regex.Match(line, "\"name\"\\s*:\\s*\"(.*?)\"");
                        if (match.Success && currentId != 0)
                        {
                            string nameStr = match.Groups[1].Value;
                            namesList.Add($"{currentId}  {nameStr}");
                            idsList.Add(currentId);
                            currentId = 0;
                        }
                    }
                }

                _allHitEffectNames = namesList.ToArray();
                _allHitEffectIds = idsList.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError("读取命中效果配表数据失败：" + ex.Message);
                _allHitEffectNames = Array.Empty<string>();
                _allHitEffectIds = Array.Empty<int>();
            }
        }

        // 获取特定角色工作区专属的过滤条目列表
        private static (string[] names, int[] ids) GetScopedHitEffects(string characterName)
        {
            if (string.IsNullOrEmpty(characterName) || _allHitEffectNames == null)
            {
                return (_allHitEffectNames, _allHitEffectIds);
            }

            if (_scopedCache.TryGetValue(characterName, out var cached))
            {
                return cached;
            }

            // 获取该角色的匹配关键词列表
            List<string> keywords = new List<string> { characterName };
            if (_characterAliases.TryGetValue(characterName, out var aliases))
            {
                for (int i = 0; i < aliases.Length; i++)
                {
                    if (!keywords.Contains(aliases[i]))
                    {
                        keywords.Add(aliases[i]);
                    }
                }
            }

            var scopedNames = new List<string>();
            var scopedIds = new List<int>();

            // 首项固定为无效果
            scopedNames.Add("无效果 0");
            scopedIds.Add(0);

            for (int i = 1; i < _allHitEffectNames.Length; i++)
            {
                string itemText = _allHitEffectNames[i];
                bool matched = false;
                for (int k = 0; k < keywords.Count; k++)
                {
                    if (itemText.IndexOf(keywords[k], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched)
                {
                    scopedNames.Add(itemText);
                    scopedIds.Add(_allHitEffectIds[i]);
                }
            }

            // 如果该角色有专属条目 则返回过滤列表 否则回退为全量
            if (scopedNames.Count > 1)
            {
                var result = (scopedNames.ToArray(), scopedIds.ToArray());
                _scopedCache[characterName] = result;
                return result;
            }

            return (_allHitEffectNames, _allHitEffectIds);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (_allHitEffectNames == null)
            {
                LoadHitEffectData();
            }

            EditorGUI.BeginProperty(position, label, property);

            if (_allHitEffectNames == null || _allHitEffectNames.Length == 0)
            {
                property.intValue = EditorGUI.IntField(position, label, property.intValue);
                EditorGUI.EndProperty();
                return;
            }

            // 识别当前载体资产所属的角色
            string activeCharName = string.Empty;
            var targetObj = property.serializedObject != null ? property.serializedObject.targetObject : null;
            if (targetObj != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(targetObj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string normalized = assetPath.Replace('\\', '/');
                    foreach (var kv in _characterAliases)
                    {
                        if (normalized.IndexOf("/" + kv.Key + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            activeCharName = kv.Key;
                            break;
                        }
                    }
                }
            }

            // 决定当前生效的条目列表
            string[] displayNames = _allHitEffectNames;
            int[] displayIds = _allHitEffectIds;
            bool isScopedActive = false;

            if (!string.IsNullOrEmpty(activeCharName) && !_globalViewMode)
            {
                var scoped = GetScopedHitEffects(activeCharName);
                if (scoped.names != null && scoped.names.Length > 1)
                {
                    displayNames = scoped.names;
                    displayIds = scoped.ids;
                    isScopedActive = true;
                }
            }

            // 动态按钮宽度计算
            float refreshBtnWidth = position.width < 220f ? 28f : 36f;
            float toggleBtnWidth = position.width < 220f ? 28f : 36f;
            float spacing = 2f;
            float totalBtnWidth = refreshBtnWidth + toggleBtnWidth + spacing;

            var popupRect = new Rect(position.x, position.y, position.width - totalBtnWidth - spacing, position.height);
            var toggleRect = new Rect(position.x + position.width - totalBtnWidth, position.y, toggleBtnWidth, position.height);
            var refreshRect = new Rect(position.x + position.width - refreshBtnWidth, position.y, refreshBtnWidth, position.height);

            int currentVal = property.intValue;
            int currentIndex = Array.IndexOf(displayIds, currentVal);

            // 如果当前值不在专属过滤列表内部 临时显示当前编号
            int popupIndex;
            if (currentIndex >= 0)
            {
                popupIndex = currentIndex;
            }
            else
            {
                // 当前值属于其他角色或者未匹配条目
                popupIndex = 0;
            }

            string popupLabel = label.text;
            if (isScopedActive)
            {
                popupLabel = label.text + " 专属";
            }

            int newIndex = EditorGUI.Popup(popupRect, popupLabel, popupIndex, displayNames);
            if (newIndex != popupIndex && newIndex >= 0 && newIndex < displayIds.Length)
            {
                property.intValue = displayIds[newIndex];
            }

            // 作用域切换按钮
            string toggleTitle = _globalViewMode ? "专" : "全";
            string toggleTooltip = _globalViewMode ? "切换为当前角色专属条目过滤" : "切换为全局所有角色完整列表";
            if (GUI.Button(toggleRect, new GUIContent(toggleTitle, toggleTooltip)))
            {
                _globalViewMode = !_globalViewMode;
            }

            // 刷新配表按钮
            if (GUI.Button(refreshRect, new GUIContent("刷", "重新从磁盘加载命中效果配表数据")))
            {
                LoadHitEffectData();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
