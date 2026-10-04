using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Game.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Framework
{
    /// <summary>
    /// HitEffectId 属性绘制器。
    /// 读取 Assets/Configs/zzz_tbhiteffect.json 配置表，以 "[id] name" 的下拉列表形式呈现，
    /// 并提供一键刷新配表数据的按钮。
    /// </summary>
    [CustomPropertyDrawer(typeof(HitEffectIdAttribute))]
    public class HitEffectIdDrawer : PropertyDrawer
    {
        private static string[] _hitEffectNames;
        private static int[] _hitEffectIds;

        public static void LoadHitEffectData()
        {
            string path = "Assets/Configs/zzz_tbhiteffect.json";
            if (!File.Exists(path))
            {
                _hitEffectNames = Array.Empty<string>();
                _hitEffectIds = Array.Empty<int>();
                return;
            }

            try
            {
                var lines = File.ReadAllLines(path);
                var namesList = new List<string>();
                var idsList = new List<int>();

                namesList.Add("无效果 (0)");
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
                            namesList.Add($"[{currentId}] {nameStr}");
                            idsList.Add(currentId);
                            currentId = 0; // reset
                        }
                    }
                }

                _hitEffectNames = namesList.ToArray();
                _hitEffectIds = idsList.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HitEffectIdDrawer] 读取 zzz_tbhiteffect.json 失败: {ex.Message}");
                _hitEffectNames = Array.Empty<string>();
                _hitEffectIds = Array.Empty<int>();
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (_hitEffectNames == null)
            {
                LoadHitEffectData();
            }

            EditorGUI.BeginProperty(position, label, property);

            if (_hitEffectNames == null || _hitEffectNames.Length == 0)
            {
                property.intValue = EditorGUI.IntField(position, label, property.intValue);
                EditorGUI.EndProperty();
                return;
            }

            const float refreshBtnWidth = 42f;
            const float spacing = 4f;

            var popupRect = new Rect(position.x, position.y, position.width - refreshBtnWidth - spacing, position.height);
            var btnRect = new Rect(position.x + position.width - refreshBtnWidth, position.y, refreshBtnWidth, position.height);

            int currentVal = property.intValue;
            int currentIndex = Array.IndexOf(_hitEffectIds, currentVal);
            int popupIndex = currentIndex >= 0 ? currentIndex : 0;

            int newIndex = EditorGUI.Popup(popupRect, label.text, popupIndex, _hitEffectNames);
            if (newIndex != popupIndex && newIndex >= 0 && newIndex < _hitEffectIds.Length)
            {
                property.intValue = _hitEffectIds[newIndex];
            }

            if (GUI.Button(btnRect, "刷新"))
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
