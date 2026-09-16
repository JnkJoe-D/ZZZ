using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(EventClip))]
    public class EventClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showEvent = true;

        private enum ParamType
        {
            String = 0,
            Float = 1,
            Int = 2
        }

        public override void DrawInspector(ClipBase clip)
        {
            var eventClip = clip as EventClip;
            if (eventClip == null) return;

            if (eventClip.parameters == null)
            {
                eventClip.parameters = new List<ATEventParam>();
            }

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 技能事件与动态参数卡片
            _showEvent = EditorGUILayout.Foldout(_showEvent, $"事件与参数配置 ({eventClip.parameters.Count})", true, EditorStyles.foldoutHeader);
            if (_showEvent)
            {
                EditorGUILayout.BeginVertical("box");
                eventClip.eventName = EditorGUILayout.TextField("事件名称", eventClip.eventName);

                EditorGUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("动态参数列表", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("+ 添加参数", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    eventClip.parameters.Add(new ATEventParam { key = "param_" + eventClip.parameters.Count });
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();

                int removeIndex = -1;
                for (int i = 0; i < eventClip.parameters.Count; i++)
                {
                    var param = eventClip.parameters[i];
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();

                    param.key = EditorGUILayout.TextField("Key", param.key, GUILayout.MinWidth(100));

                    // 推断类型
                    ParamType currentType = ParamType.String;
                    if (!string.IsNullOrEmpty(param.stringValue)) currentType = ParamType.String;
                    else if (Math.Abs(param.floatValue) > float.Epsilon) currentType = ParamType.Float;
                    else if (param.intValue != 0) currentType = ParamType.Int;

                    var newType = (ParamType)EditorGUILayout.EnumPopup(currentType, GUILayout.Width(70));

                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        removeIndex = i;
                        GUI.FocusControl(null);
                    }
                    EditorGUILayout.EndHorizontal();

                    switch (newType)
                    {
                        case ParamType.String:
                            param.stringValue = EditorGUILayout.TextField("字符串值", param.stringValue);
                            break;
                        case ParamType.Float:
                            param.floatValue = EditorGUILayout.FloatField("浮点数值", param.floatValue);
                            break;
                        case ParamType.Int:
                            param.intValue = EditorGUILayout.IntField("整数值", param.intValue);
                            break;
                    }

                    EditorGUILayout.EndVertical();
                }

                if (removeIndex >= 0)
                {
                    eventClip.parameters.RemoveAt(removeIndex);
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Event Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Event Clip");
            }
        }
    }
}
