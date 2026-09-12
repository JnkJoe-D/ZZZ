using System;
using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(BuffClip))]
    public class BuffClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showBuffConfig = true;

        public override void DrawInspector(ClipBase clip)
        {
            var buffClip = clip as BuffClip;
            if (buffClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片 (片段名称、启用、时间等)
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. Buff 状态配置卡片
            _showBuffConfig = EditorGUILayout.Foldout(_showBuffConfig, "Buff 状态配置", true, EditorStyles.foldoutHeader);
            if (_showBuffConfig)
            {
                EditorGUILayout.BeginVertical("box");

                // Buff 下拉选择 (读取 Luban 生成的 zzz_tbbuff.json)
                buffClip.buffId = DrawBuffIdSelector("选择 Buff", buffClip.buffId);

                EditorGUILayout.Space(4);

                // 生命周期模式
                buffClip.lifetimeMode = (BuffClipLifetimeMode)EditorGUILayout.EnumPopup("生命周期模式", buffClip.lifetimeMode);
                if (buffClip.lifetimeMode == BuffClipLifetimeMode.ManageByClip)
                {
                    EditorGUILayout.HelpBox("【跟随片段】进入片段时自动施加，离开片段或切招打断时自动注销移除。", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("【单次施加】进入片段时施加一次，后续生命周期由 Buff 自身持续时间/次数或业务逻辑管理。", MessageType.None);
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                MarkTimelineDirty("Modify Buff Clip");
            }
        }
    }
}
