using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// ActionTimeline 资产的 Inspector 自定义绘制器
    /// 保证 Duration 字段只读显示（由轨道片段自动计算），避免手动误改
    /// </summary>
    [CustomEditor(typeof(ActionTimeline))]
    public class ActionTimelineEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (prop.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(prop, true);
                    }
                }
                else if (prop.name == "duration")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(prop, new GUIContent("Duration", "时间轴总持续时间（根据轨道片段自动计算，只读）"), true);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
