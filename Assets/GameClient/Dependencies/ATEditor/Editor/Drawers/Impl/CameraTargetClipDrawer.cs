using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(CameraTargetClip))]
    public class CameraTargetClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showTarget = true;
        private static bool _showAxis = true;
        private static bool _showTransitions = true;
        private static bool _showDamping = true;
        private static bool _showRestore = true;

        private static readonly Dictionary<string, bool?> _boneValidationStatus = new Dictionary<string, bool?>();

        public override void DrawInspector(ClipBase clip)
        {
            var targetClip = clip as CameraTargetClip;
            if (targetClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 目标与骨骼配置
            _showTarget = EditorGUILayout.Foldout(_showTarget, "目标与跟随骨骼", true, EditorStyles.foldoutHeader);
            if (_showTarget)
            {
                EditorGUILayout.BeginVertical("box");
                DrawFieldByName(targetClip, "targetScope");
                DrawFieldByName(targetClip, "bindPoint");
                if (targetClip.bindPoint == BindPoint.CustomBone)
                {
                    DrawCustomBoneField(targetClip);
                }
                DrawFieldByName(targetClip, "referenceSpace");
                DrawFieldByName(targetClip, "positionOffset");
                EditorGUILayout.EndVertical();
            }

            // 3. 轴向同步过滤 (单行 Toggle 组，复刻 Rigidbody Constraints 风格)
            _showAxis = EditorGUILayout.Foldout(_showAxis, "同步轴向约束", true, EditorStyles.foldoutHeader);
            if (_showAxis)
            {
                EditorGUILayout.BeginVertical("box");
                DrawAxisToggleGroup(targetClip);
                EditorGUILayout.EndVertical();
            }

            // 4. 进出过渡与曲线配置
            _showTransitions = EditorGUILayout.Foldout(_showTransitions, "进出过渡与曲线", true, EditorStyles.foldoutHeader);
            if (_showTransitions)
            {
                EditorGUILayout.BeginVertical("box");
                targetClip.BlendInDuration = EditorGUILayout.FloatField("进入过渡时间 (s)", targetClip.BlendInDuration);
                DrawFieldByName(targetClip, "blendInCurve");
                EditorGUILayout.Space(2);
                targetClip.BlendOutDuration = EditorGUILayout.FloatField("退出过渡时间 (s)", targetClip.BlendOutDuration);
                DrawFieldByName(targetClip, "blendOutCurve");
                EditorGUILayout.EndVertical();
            }

            // 5. 平滑跟随与阻尼设置
            _showDamping = EditorGUILayout.Foldout(_showDamping, "运动平滑阻尼 (防骨骼抖动)", true, EditorStyles.foldoutHeader);
            if (_showDamping)
            {
                EditorGUILayout.BeginVertical("box");
                DrawFieldByName(targetClip, "enableDamping");
                if (targetClip.enableDamping)
                {
                    DrawFieldByName(targetClip, "smoothTime");
                }
                EditorGUILayout.EndVertical();
            }

            // 6. 恢复策略
            _showRestore = EditorGUILayout.Foldout(_showRestore, "生命周期还原策略", true, EditorStyles.foldoutHeader);
            if (_showRestore)
            {
                EditorGUILayout.BeginVertical("box");
                DrawFieldByName(targetClip, "restoreOnExit");
                DrawFieldByName(targetClip, "restoreOnStop");
                if (targetClip.restoreOnStop)
                {
                    DrawFieldByName(targetClip, "interruptRestoreDuration");
                }
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Camera Target Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                else
                {
                    MarkTimelineDirty("Modify Camera Target Clip");
                }
            }
        }

        private void DrawFieldByName(CameraTargetClip obj, string fieldName)
        {
            var field = typeof(CameraTargetClip).GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                DrawField(field, obj);
            }
        }

        /// <summary>
        /// 绘制类似 Rigidbody Constraints 的同一行 Toggle 组
        /// </summary>
        private void DrawAxisToggleGroup(CameraTargetClip targetClip)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("同步轴向", "勾选的轴向将同步跟随目标骨骼，未勾选轴向保持基准点原始位置"));

            float buttonWidth = 38f;
            targetClip.syncX = GUILayout.Toggle(targetClip.syncX, "X", EditorStyles.miniButtonLeft, GUILayout.Width(buttonWidth));
            targetClip.syncY = GUILayout.Toggle(targetClip.syncY, "Y", EditorStyles.miniButtonMid, GUILayout.Width(buttonWidth));
            targetClip.syncZ = GUILayout.Toggle(targetClip.syncZ, "Z", EditorStyles.miniButtonRight, GUILayout.Width(buttonWidth));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCustomBoneField(CameraTargetClip targetClip)
        {
            EditorGUILayout.BeginHorizontal();

            string clipKey = !string.IsNullOrEmpty(targetClip.clipId) ? targetClip.clipId : targetClip.GetHashCode().ToString();
            _boneValidationStatus.TryGetValue(clipKey, out bool? isValid);

            Color oldBgColor = GUI.backgroundColor;
            if (isValid.HasValue)
            {
                GUI.backgroundColor = isValid.Value 
                    ? new Color(0.6f, 1f, 0.6f, 1f)   // 有效：绿色
                    : new Color(1f, 0.55f, 0.55f, 1f); // 无效：红色
            }

            string newName = EditorGUILayout.TextField("自定义骨骼名称", targetClip.customBoneName);
            if (newName != targetClip.customBoneName)
            {
                targetClip.customBoneName = newName;
                _boneValidationStatus.Remove(clipKey);
            }

            GUI.backgroundColor = oldBgColor;

            if (GUILayout.Button(new GUIContent("检测", "检测当前预览角色层级中是否存在该自定义骨骼"), GUILayout.Width(50), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                bool found = CheckBoneExists(targetClip.customBoneName, out string msg);
                _boneValidationStatus[clipKey] = found;
                if (found)
                {
                    ATLog.Info($"<color=green>[CameraTarget 骨骼检测通过]</color> {msg}");
                }
                else
                {
                    ATLog.Warning($"<color=red>[CameraTarget 骨骼检测失败]</color> {msg}");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static bool CheckBoneExists(string boneName, out string message)
        {
            if (string.IsNullOrWhiteSpace(boneName))
            {
                message = "骨骼名称为空！";
                return false;
            }

            GameObject previewTarget = null;
            if (EditorWindow.HasOpenInstances<ATEditorWindow>())
            {
                var window = EditorWindow.GetWindow<ATEditorWindow>(false, "动作时间轴编辑器", false);
                var editorState = window != null ? window.GetState() : null;
                if (editorState != null)
                {
                    if (editorState.PreviewContext != null && editorState.PreviewContext.Owner != null)
                    {
                        previewTarget = editorState.PreviewContext.Owner;
                    }
                    else if (editorState.previewTarget != null)
                    {
                        previewTarget = editorState.previewTarget;
                    }
                }
            }

            if (previewTarget == null)
            {
                message = "未找到场景或编辑器预览角色，无法检测！";
                return false;
            }

            var getter = new ATBoneGetter(previewTarget);
            var bone = getter.GetBone(BindPoint.CustomBone, boneName);
            if (bone != null && bone != previewTarget.transform)
            {
                message = $"成功匹配骨骼: {bone.name} (路径: {GetHierarchyPath(bone, previewTarget.transform)})";
                return true;
            }

            message = $"在预览角色 '{previewTarget.name}' 的子层级中未检索到名为 '{boneName}' 的骨骼 Transform！";
            return false;
        }

        private static string GetHierarchyPath(Transform target, Transform root)
        {
            if (target == null || target == root) return target != null ? target.name : string.Empty;
            string path = target.name;
            Transform curr = target.parent;
            while (curr != null && curr != root)
            {
                path = curr.name + "/" + path;
                curr = curr.parent;
            }
            return path;
        }
    }
}
