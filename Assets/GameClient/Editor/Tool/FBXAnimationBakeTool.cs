using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tool
{
    /// <summary>
    /// Root Transform Rotation 基于的基准
    /// </summary>
    public enum BasedUponRotation
    {
        [InspectorName("Original")]
        Original = 0,
        [InspectorName("Body Orientation")]
        BodyOrientation = 1,
    }

    /// <summary>
    /// Root Transform Position (Y) 基于的基准
    /// </summary>
    public enum BasedUponHeightY
    {
        [InspectorName("Original")]
        Original = 0,
        [InspectorName("Center of Mass")]
        CenterOfMass = 1,
        [InspectorName("Feet")]
        Feet = 2,
    }

    /// <summary>
    /// Root Transform Position (XZ) 基于的基准
    /// </summary>
    public enum BasedUponPositionXZ
    {
        [InspectorName("Original")]
        Original = 0,
        [InspectorName("Center of Mass")]
        CenterOfMass = 1,
    }

    /// <summary>
    /// FBX 内嵌动画 Root Transform (Bake Into Pose & Based Upon) 批量配置工具
    /// </summary>
    public class FBXAnimationBakeTool : EditorWindow
    {
        [Header("Target Assets")]
        [SerializeField] private List<UnityEngine.Object> _targetObjects = new();

        [Header("Rotation (Root Transform Rotation)")]
        private bool _modifyRotation = true;
        private bool _bakeRotation = true;
        private BasedUponRotation _rotationBasedUpon = BasedUponRotation.Original;

        [Header("Position Y (Root Transform Position Y / Height)")]
        private bool _modifyHeightY = true;
        private bool _bakeHeightY = true;
        private BasedUponHeightY _heightYBasedUpon = BasedUponHeightY.Original;

        [Header("Position XZ (Root Transform Position XZ)")]
        private bool _modifyPositionXZ = false;
        private bool _bakePositionXZ = false;
        private BasedUponPositionXZ _positionXZBasedUpon = BasedUponPositionXZ.Original;

        [Header("Loop Settings")]
        private bool _modifyLoop = false;
        private bool _loopTime = false;
        private bool _loopPose = false;

        [Header("Take Length Settings")]
        private bool _modifyTakeLength = false;
        private bool _trimEndFrame = false;

        private Vector2 _scrollPos;
        private SerializedObject _serializedObject;
        private SerializedProperty _targetObjectsProp;

        [MenuItem("Tools/Animation/FBX Animation Bake Tool", false, 100)]
        public static void OpenWindow()
        {
            var window = GetWindow<FBXAnimationBakeTool>("FBX Animation Bake Tool");
            window.minSize = new Vector2(460, 520);
            window.Show();
        }

        [MenuItem("Assets/Tools/Animation/Bake Into Pose (Selected FBX)", false, 20)]
        public static void QuickBakeSelected()
        {
            var selectedObjects = Selection.objects;
            var fbxPaths = CollectFBXPaths(selectedObjects);

            if (fbxPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "未选中任何 FBX 资源或包含 FBX 的文件夹！", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("确认配置",
                    $"即将为选中的 {fbxPaths.Count} 个 FBX 文件配置内嵌动画：\n" +
                    $"- Root Transform Rotation: Bake Into Pose = true, Based Upon = Original\n" +
                    $"- Root Transform Position(Y): Bake Into Pose = true, Based Upon = Original\n" +
                    $"\n是否继续？", "确定", "取消"))
            {
                return;
            }

            int successCount = 0;
            try
            {
                for (int i = 0; i < fbxPaths.Count; i++)
                {
                    string path = fbxPaths[i];
                    EditorUtility.DisplayProgressBar("正在配置 FBX 动画", $"处理中 ({i + 1}/{fbxPaths.Count}): {Path.GetFileName(path)}", (float)(i + 1) / fbxPaths.Count);

                    if (ProcessFBX(path,
                        modifyRot: true, bakeRot: true, rotBasedUpon: BasedUponRotation.Original,
                        modifyY: true, bakeY: true, yBasedUpon: BasedUponHeightY.Original,
                        modifyXZ: false, bakeXZ: false, xzBasedUpon: BasedUponPositionXZ.Original,
                        modifyLoop: false, loopTime: false, loopPose: false,
                        modifyTakeLength: false, trimEndFrame: false))
                    {
                        successCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"<color=green>[FBXAnimationBakeTool] 批量处理完成！成功更新 {successCount}/{fbxPaths.Count} 个 FBX 文件。</color>");
        }

        [MenuItem("Assets/Tools/Animation/Bake Into Pose (Selected FBX)", true)]
        public static bool ValidateQuickBakeSelected()
        {
            return Selection.objects != null && Selection.objects.Length > 0;
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _targetObjectsProp = _serializedObject.FindProperty(nameof(_targetObjects));

            // 如果窗口打开时有选中的对象，自动填充
            if (Selection.objects != null && Selection.objects.Length > 0 && _targetObjects.Count == 0)
            {
                LoadFromSelection();
            }
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(6);

            DrawHeader();
            EditorGUILayout.Space(6);

            DrawConfigSettings();
            EditorGUILayout.Space(8);

            DrawTargetList();
            EditorGUILayout.Space(12);

            DrawActionButtons();

            EditorGUILayout.EndScrollView();
            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.HelpBox("FBX 内嵌动画 Root Transform 批量配置器\n用于一键批量设置 FBX 文件的 Root Motion Bake Into Pose 及 Based Upon 选项（枚举与 Inspector 原生选项完全对应）。", MessageType.Info);
        }

        private void DrawConfigSettings()
        {
            // 1. Rotation
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _modifyRotation = EditorGUILayout.ToggleLeft(new GUIContent("配置 Root Transform Rotation (旋转)", "是否修改旋转相关的 Bake 配置"), _modifyRotation, EditorStyles.boldLabel);
                if (_modifyRotation)
                {
                    EditorGUI.indentLevel++;
                    _bakeRotation = EditorGUILayout.Toggle(new GUIContent("Bake Into Pose", "锁定旋转"), _bakeRotation);
                    _rotationBasedUpon = (BasedUponRotation)EditorGUILayout.EnumPopup(new GUIContent("Based Upon", "选择基准参考（Original / Body Orientation）"), _rotationBasedUpon);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(4);

            // 2. Position Y
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _modifyHeightY = EditorGUILayout.ToggleLeft(new GUIContent("配置 Root Transform Position (Y) (高度/Y轴)", "是否修改 Y 轴高度相关的 Bake 配置"), _modifyHeightY, EditorStyles.boldLabel);
                if (_modifyHeightY)
                {
                    EditorGUI.indentLevel++;
                    _bakeHeightY = EditorGUILayout.Toggle(new GUIContent("Bake Into Pose", "锁定高度 Y"), _bakeHeightY);
                    _heightYBasedUpon = (BasedUponHeightY)EditorGUILayout.EnumPopup(new GUIContent("Based Upon", "选择基准参考（Original / Center of Mass / Feet）"), _heightYBasedUpon);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(4);

            // 3. Position XZ
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _modifyPositionXZ = EditorGUILayout.ToggleLeft(new GUIContent("配置 Root Transform Position (XZ) (平面/XZ轴)", "可选配置：是否修改 XZ 平面相关的 Bake 配置"), _modifyPositionXZ, EditorStyles.boldLabel);
                if (_modifyPositionXZ)
                {
                    EditorGUI.indentLevel++;
                    _bakePositionXZ = EditorGUILayout.Toggle(new GUIContent("Bake Into Pose", "锁定平面 XZ"), _bakePositionXZ);
                    _positionXZBasedUpon = (BasedUponPositionXZ)EditorGUILayout.EnumPopup(new GUIContent("Based Upon", "选择基准参考（Original / Center of Mass）"), _positionXZBasedUpon);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(4);

            // 4. Loop
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _modifyLoop = EditorGUILayout.ToggleLeft(new GUIContent("配置 Loop 循环设置 (可选)", "是否同步修改动画循环设置"), _modifyLoop, EditorStyles.boldLabel);
                if (_modifyLoop)
                {
                    EditorGUI.indentLevel++;
                    _loopTime = EditorGUILayout.Toggle(new GUIContent("Loop Time", "开启循环"), _loopTime);
                    _loopPose = EditorGUILayout.Toggle(new GUIContent("Loop Pose", "循环姿势平滑"), _loopPose);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(4);

            // 5. Take Length
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _modifyTakeLength = EditorGUILayout.ToggleLeft(new GUIContent("配置 Take 时长 (Auto Trim)", "自动裁剪多余的静止尾帧，重置 Clip 的 End Frame"), _modifyTakeLength, EditorStyles.boldLabel);
                if (_modifyTakeLength)
                {
                    EditorGUI.indentLevel++;
                    _trimEndFrame = EditorGUILayout.Toggle(new GUIContent("Trim End Frame", "检测曲线实际运动的最后一帧，截断后面的静止无效帧"), _trimEndFrame);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawTargetList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("目标 FBX / 文件夹列表", EditorStyles.boldLabel);
                if (GUILayout.Button("从当前选择载入", EditorStyles.miniButton, GUILayout.Width(110)))
                {
                    LoadFromSelection();
                }
                if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    _targetObjects.Clear();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_targetObjectsProp, true);

                // 拖拽区域支持
                Event evt = Event.current;
                Rect dropArea = GUILayoutUtility.GetRect(0.0f, 40.0f, GUILayout.ExpandWidth(true));
                GUI.Box(dropArea, "将 FBX 文件或包含 FBX 的文件夹拖入此处", EditorStyles.centeredGreyMiniLabel);

                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!dropArea.Contains(evt.mousePosition)) break;

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                            {
                                if (!_targetObjects.Contains(draggedObject))
                                {
                                    _targetObjects.Add(draggedObject);
                                }
                            }
                            GUI.changed = true;
                        }
                        break;
                }
            }
        }

        private void DrawActionButtons()
        {
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f, 1f);
            if (GUILayout.Button("开始批量应用配置", GUILayout.Height(38)))
            {
                ExecuteBatch();
            }
            GUI.backgroundColor = Color.white;
        }

        private void LoadFromSelection()
        {
            if (Selection.objects != null && Selection.objects.Length > 0)
            {
                _targetObjects.Clear();
                foreach (var obj in Selection.objects)
                {
                    _targetObjects.Add(obj);
                }
            }
        }

        private void ExecuteBatch()
        {
            var fbxPaths = CollectFBXPaths(_targetObjects);
            if (fbxPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何待处理的 FBX 文件！请在列表中添加 FBX 资源或文件夹。", "确定");
                return;
            }

            int successCount = 0;
            try
            {
                for (int i = 0; i < fbxPaths.Count; i++)
                {
                    string path = fbxPaths[i];
                    EditorUtility.DisplayProgressBar("正在配置 FBX 动画", $"处理中 ({i + 1}/{fbxPaths.Count}): {Path.GetFileName(path)}", (float)(i + 1) / fbxPaths.Count);

                    if (ProcessFBX(path,
                        _modifyRotation, _bakeRotation, _rotationBasedUpon,
                        _modifyHeightY, _bakeHeightY, _heightYBasedUpon,
                        _modifyPositionXZ, _bakePositionXZ, _positionXZBasedUpon,
                        _modifyLoop, _loopTime, _loopPose,
                        _modifyTakeLength, _trimEndFrame))
                    {
                        successCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("处理完成", $"成功更新了 {successCount}/{fbxPaths.Count} 个 FBX 文件的内嵌动画配置！", "确定");
            Debug.Log($"<color=green>[FBXAnimationBakeTool] 批量处理完成！成功更新 {successCount}/{fbxPaths.Count} 个 FBX 文件。</color>");
        }

        private static bool ProcessFBX(string assetPath,
            bool modifyRot, bool bakeRot, BasedUponRotation rotBasedUpon,
            bool modifyY, bool bakeY, BasedUponHeightY yBasedUpon,
            bool modifyXZ, bool bakeXZ, BasedUponPositionXZ xzBasedUpon,
            bool modifyLoop, bool loopTime, bool loopPose,
            bool modifyTakeLength, bool trimEndFrame)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) return false;

            // 获取动画剪辑列表（如果自定义为空，则读取默认剪辑列表）
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"[FBXAnimationBakeTool] FBX 文件未包含任何动画剪辑: {assetPath}");
                return false;
            }

            bool changed = false;
            
            Dictionary<string, float> actualEndFrames = null;
            if (modifyTakeLength && trimEndFrame)
            {
                actualEndFrames = GetActualClipEndFrames(assetPath);
            }

            for (int i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];

                // 1. Rotation
                if (modifyRot)
                {
                    clip.lockRootRotation = bakeRot;
                    clip.keepOriginalOrientation = (rotBasedUpon == BasedUponRotation.Original);
                    changed = true;
                }

                // 2. Position Y
                if (modifyY)
                {
                    clip.lockRootHeightY = bakeY;
                    clip.keepOriginalPositionY = (yBasedUpon == BasedUponHeightY.Original);
                    clip.heightFromFeet = (yBasedUpon == BasedUponHeightY.Feet);
                    changed = true;
                }

                // 3. Position XZ
                if (modifyXZ)
                {
                    clip.lockRootPositionXZ = bakeXZ;
                    clip.keepOriginalPositionXZ = (xzBasedUpon == BasedUponPositionXZ.Original);
                    changed = true;
                }

                // 4. Loop
                if (modifyLoop)
                {
                    clip.loopTime = loopTime;
                    clip.loopPose = loopPose;
                    changed = true;
                }

                // 5. Take Length
                if (modifyTakeLength && trimEndFrame && actualEndFrames != null)
                {
                    if (actualEndFrames.TryGetValue(clip.name, out float actualDurationFrames))
                    {
                        float newEndFrame = clip.firstFrame + actualDurationFrames;
                        if (Mathf.Abs(clip.lastFrame - newEndFrame) > 0.1f)
                        {
                            clip.lastFrame = newEndFrame;
                            changed = true;
                        }
                    }
                }

                clips[i] = clip;
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                return true;
            }

            return false;
        }

        private static Dictionary<string, float> GetActualClipEndFrames(string assetPath)
        {
            var result = new Dictionary<string, float>();
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            
            foreach (var asset in allAssets)
            {
                if (asset is AnimationClip animClip && !animClip.name.StartsWith("__preview__"))
                {
                    float maxTime = 0f;
                    bool hasKeys = false;

                    var bindings = AnimationUtility.GetCurveBindings(animClip);
                    foreach (var binding in bindings)
                    {
                        var curve = AnimationUtility.GetEditorCurve(animClip, binding);
                        if (curve != null && curve.keys.Length > 0)
                        {
                            hasKeys = true;
                            float lastEffectiveTime = 0f;
                            if (curve.keys.Length == 1)
                            {
                                lastEffectiveTime = curve.keys[0].time;
                            }
                            else
                            {
                                float lastValue = curve.keys[curve.keys.Length - 1].value;
                                int effectiveIndex = 0;
                                for (int i = curve.keys.Length - 1; i >= 0; i--)
                                {
                                    // 将阈值从 1e-4f 放宽到 0.005f（兼容 5mm 的位置位移，或 0.5 度的旋转抖动）
                                    // 以过滤掉 3D 软件导出 FBX 时的死区精度抖动（Jitter）
                                    if (Mathf.Abs(curve.keys[i].value - lastValue) > 0.005f)
                                    {
                                        effectiveIndex = Mathf.Min(i + 1, curve.keys.Length - 1);
                                        break;
                                    }
                                }
                                lastEffectiveTime = curve.keys[effectiveIndex].time;
                            }
                            maxTime = Mathf.Max(maxTime, lastEffectiveTime);
                        }
                    }

                    if (hasKeys)
                    {
                        float actualEndFrame = Mathf.Round(maxTime * animClip.frameRate);
                        if (actualEndFrame < 1f) actualEndFrame = 1f;
                        result[animClip.name] = actualEndFrame;
                    }
                }
            }
            return result;
        }

        private static List<string> CollectFBXPaths(IEnumerable<UnityEngine.Object> objects)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (objects == null) return new List<string>();

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                if (Directory.Exists(path))
                {
                    // 递归搜索文件夹下的所有 FBX 文件
                    string[] guids = AssetDatabase.FindAssets("t:Model", new[] { path });
                    foreach (string guid in guids)
                    {
                        string subPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (subPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(subPath);
                        }
                    }
                }
                else if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(path);
                }
            }

            return new List<string>(result);
        }
    }
}
