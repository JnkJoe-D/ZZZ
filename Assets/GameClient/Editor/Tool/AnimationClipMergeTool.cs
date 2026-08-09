using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tool
{
    /// <summary>
    /// FBX 多动画片段（姿态骨骼 + 表情BlendShape）合并与提取工具
    /// </summary>
    public class AnimationClipMergeTool : EditorWindow
    {
        [System.Serializable]
        public class MergePairItem
        {
            public bool isSelected = true;
            public string pairKey;
            public AnimationClip bodyClip;
            public AnimationClip faceClip;
            public string targetName;
            public string sourceFbxPath;
        }

        private UnityEngine.Object _targetFbxAsset;
        private string _outputDirectory = "";
        private List<MergePairItem> _scannedPairs = new();
        private Vector2 _scrollPos;

        // 手动单对合并栏
        private bool _showManualSection = false;
        private AnimationClip _manualClipA;
        private AnimationClip _manualClipB;
        private string _manualOutputName = "Merged_Animation";

        // 命名匹配正则规则 (识别如 .001, _face, _facial, _exp 等表情动画后缀)
        private static readonly Regex FaceSuffixRegex = new(
            @"^(.*?)(?:\.001|\.002|_face|_facial|_exp|_expression|_shapekey|_shape|_bs|\s*\(\d+\))$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        [MenuItem("Tools/Animation/Animation Clip Merge Tool (动画合并工具)", false, 101)]
        public static void OpenWindow()
        {
            var window = GetWindow<AnimationClipMergeTool>("动画片段合并工具");
            window.minSize = new Vector2(650, 560);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(6);

            DrawHeader();
            EditorGUILayout.Space(8);

            DrawSourceAndOutputSection();
            EditorGUILayout.Space(8);

            DrawBatchPairsList();
            EditorGUILayout.Space(12);

            DrawManualMergeSection();
            EditorGUILayout.Space(12);

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.HelpBox(
                "【FBX 独立动画合并工具】\n" +
                "解决 Blender 导出时将角色骨骼姿态与面部表情拆分为两个 Action（如 idle 与 idle.001）的问题。\n" +
                "本工具可自动扫描 FBX 内的配对动画片段，将骨骼曲线（Transform）与表情曲线（BlendShape / 属性）合并为单一独立的 .anim 资产文件。",
                MessageType.Info);
        }

        private void DrawSourceAndOutputSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("资源与导出设置", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                EditorGUI.BeginChangeCheck();
                _targetFbxAsset = EditorGUILayout.ObjectField("目标 FBX / 文件夹", _targetFbxAsset, typeof(UnityEngine.Object), false);
                if (EditorGUI.EndChangeCheck())
                {
                    OnTargetAssetChanged();
                }

                // 拖拽区域
                Event evt = Event.current;
                Rect dropArea = GUILayoutUtility.GetRect(0.0f, 32.0f, GUILayout.ExpandWidth(true));
                GUI.Box(dropArea, "或者拖拽 FBX 模型文件 / 文件夹到此处", EditorStyles.centeredGreyMiniLabel);
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    if (dropArea.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            if (DragAndDrop.objectReferences.Length > 0)
                            {
                                _targetFbxAsset = DragAndDrop.objectReferences[0];
                                OnTargetAssetChanged();
                                GUI.changed = true;
                            }
                        }
                    }
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                _outputDirectory = EditorGUILayout.TextField("导出文件夹", _outputDirectory);
                if (GUILayout.Button("浏览...", GUILayout.Width(70)))
                {
                    string selected = EditorUtility.OpenFolderPanel("选择合并动画导出目录", "Assets", "");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        if (selected.StartsWith(Application.dataPath))
                        {
                            _outputDirectory = "Assets" + selected.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            _outputDirectory = selected;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("重新扫描并匹配动画片段", GUILayout.Height(26)))
                {
                    ScanAndMatchPairs();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void OnTargetAssetChanged()
        {
            if (_targetFbxAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(_targetFbxAsset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string dir = Directory.Exists(assetPath) ? assetPath : Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
                    _outputDirectory = $"{dir}/Merged_Anim";
                    ScanAndMatchPairs();
                }
            }
        }

        private void DrawBatchPairsList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"已识别的配对列表 ({_scannedPairs.Count} 组)", EditorStyles.boldLabel);

                if (_scannedPairs.Count > 0)
                {
                    if (GUILayout.Button("全选", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        _scannedPairs.ForEach(p => p.isSelected = true);
                    }
                    if (GUILayout.Button("全不选", EditorStyles.miniButton, GUILayout.Width(55)))
                    {
                        _scannedPairs.ForEach(p => p.isSelected = false);
                    }
                    if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        _scannedPairs.Clear();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                if (_scannedPairs.Count == 0)
                {
                    EditorGUILayout.HelpBox("暂无匹配的动画对。请在上方指定包含动画的 FBX 文件并点击“重新扫描”。", MessageType.None);
                }
                else
                {
                    // 表头
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    GUILayout.Label("选择", GUILayout.Width(40));
                    GUILayout.Label("主姿态动画 (Body)", GUILayout.MinWidth(150));
                    GUILayout.Label("表情动画 (Face/Aux)", GUILayout.MinWidth(150));
                    GUILayout.Label("合并导出名称 (.anim)", GUILayout.MinWidth(150));
                    EditorGUILayout.EndHorizontal();

                    for (int i = 0; i < _scannedPairs.Count; i++)
                    {
                        var item = _scannedPairs[i];
                        EditorGUILayout.BeginHorizontal(i % 2 == 0 ? EditorStyles.helpBox : GUIStyle.none);

                        item.isSelected = EditorGUILayout.Toggle(item.isSelected, GUILayout.Width(35));
                        item.bodyClip = (AnimationClip)EditorGUILayout.ObjectField(item.bodyClip, typeof(AnimationClip), false, GUILayout.MinWidth(150));
                        item.faceClip = (AnimationClip)EditorGUILayout.ObjectField(item.faceClip, typeof(AnimationClip), false, GUILayout.MinWidth(150));
                        item.targetName = EditorGUILayout.TextField(item.targetName, GUILayout.MinWidth(150));

                        if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(22)))
                        {
                            _scannedPairs.RemoveAt(i);
                            i--;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(8);
                    GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f, 1f);
                    if (GUILayout.Button($"一键合并选中的动画并导出到目录 ({_scannedPairs.Count(p => p.isSelected)} 组)", GUILayout.Height(36)))
                    {
                        ExecuteBatchMerge();
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
        }

        private void DrawManualMergeSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showManualSection = EditorGUILayout.Foldout(_showManualSection, "手动单对动画合并 (高级/自由合并)", true);
                if (_showManualSection)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(4);

                    _manualClipA = (AnimationClip)EditorGUILayout.ObjectField("动画片段 A (主姿态/骨骼)", _manualClipA, typeof(AnimationClip), false);
                    _manualClipB = (AnimationClip)EditorGUILayout.ObjectField("动画片段 B (表情/BlendShape)", _manualClipB, typeof(AnimationClip), false);
                    _manualOutputName = EditorGUILayout.TextField("导出动画名称", _manualOutputName);

                    EditorGUILayout.Space(6);
                    if (GUILayout.Button("合并并保存当前单对动画", GUILayout.Height(28)))
                    {
                        ExecuteManualMerge();
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void ScanAndMatchPairs()
        {
            _scannedPairs.Clear();
            if (_targetFbxAsset == null) return;

            string rootPath = AssetDatabase.GetAssetPath(_targetFbxAsset);
            if (string.IsNullOrEmpty(rootPath)) return;

            List<string> fbxPaths = new();
            if (Directory.Exists(rootPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { rootPath });
                foreach (var guid in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    {
                        fbxPaths.Add(p);
                    }
                }
            }
            else if (rootPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                fbxPaths.Add(rootPath);
            }

            foreach (var fbxPath in fbxPaths)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                var clips = assets.OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (clips.Count == 0) continue;

                // 建立基础名称映射
                // 比如 idle.001 -> base: idle; idle -> base: idle
                Dictionary<string, List<AnimationClip>> grouped = new(StringComparer.OrdinalIgnoreCase);

                foreach (var clip in clips)
                {
                    string clipName = clip.name;
                    var match = FaceSuffixRegex.Match(clipName);
                    string baseName = match.Success ? match.Groups[1].Value.Trim() : clipName;

                    if (!grouped.ContainsKey(baseName))
                    {
                        grouped[baseName] = new List<AnimationClip>();
                    }
                    grouped[baseName].Add(clip);
                }

                foreach (var kv in grouped)
                {
                    string baseName = kv.Key;
                    var groupClips = kv.Value;

                    if (groupClips.Count >= 2)
                    {
                        // 区分 body clip 和 face clip
                        // 带有后缀的（如 .001）为 faceClip，无后缀的为 bodyClip
                        var faceClip = groupClips.FirstOrDefault(c => FaceSuffixRegex.IsMatch(c.name));
                        var bodyClip = groupClips.FirstOrDefault(c => c != faceClip) ?? groupClips[0];

                        _scannedPairs.Add(new MergePairItem
                        {
                            isSelected = true,
                            pairKey = baseName,
                            bodyClip = bodyClip,
                            faceClip = faceClip,
                            targetName = baseName,
                            sourceFbxPath = fbxPath
                        });
                    }
                    else if (groupClips.Count == 1)
                    {
                        // 单独的片段也加入列表（以防用户想要单提取或手动对齐）
                        _scannedPairs.Add(new MergePairItem
                        {
                            isSelected = false,
                            pairKey = baseName,
                            bodyClip = groupClips[0],
                            faceClip = null,
                            targetName = baseName,
                            sourceFbxPath = fbxPath
                        });
                    }
                }
            }

            Debug.Log($"<color=cyan>[AnimationClipMergeTool] 扫描完成，共找到 {_scannedPairs.Count} 组动画项，其中配对成功 {_scannedPairs.Count(p => p.faceClip != null)} 组。</color>");
        }

        private void ExecuteBatchMerge()
        {
            var targets = _scannedPairs.Where(p => p.isSelected && (p.bodyClip != null || p.faceClip != null)).ToList();
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "未选中任何有效的合并项！", "确定");
                return;
            }

            if (string.IsNullOrEmpty(_outputDirectory))
            {
                EditorUtility.DisplayDialog("提示", "请先指定导出文件夹！", "确定");
                return;
            }

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            int successCount = 0;
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var item = targets[i];
                    string fileName = string.IsNullOrEmpty(item.targetName) ? "MergedClip" : item.targetName;
                    string outPath = $"{_outputDirectory}/{fileName}.anim".Replace("\\", "/");

                    EditorUtility.DisplayProgressBar("正在合并动画片段", $"处理中 ({i + 1}/{targets.Count}): {fileName}", (float)(i + 1) / targets.Count);

                    var mergedClip = MergeClips(item.bodyClip, item.faceClip, fileName);
                    if (mergedClip != null)
                    {
                        AssetDatabase.CreateAsset(mergedClip, outPath);
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

            EditorUtility.DisplayDialog("合并完成", $"成功生成并保存了 {successCount}/{targets.Count} 个 .anim 动画文件！\n路径：{_outputDirectory}", "确定");
            Debug.Log($"<color=green>[AnimationClipMergeTool] 批量合并完成！共生成 {successCount} 个动画文件保存在: {_outputDirectory}</color>");
        }

        private void ExecuteManualMerge()
        {
            if (_manualClipA == null && _manualClipB == null)
            {
                EditorUtility.DisplayDialog("提示", "请至少指定一个待合并的动画片段！", "确定");
                return;
            }

            string outDir = string.IsNullOrEmpty(_outputDirectory) ? "Assets" : _outputDirectory;
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            string targetName = string.IsNullOrEmpty(_manualOutputName) ? "Merged_Animation" : _manualOutputName;
            string outPath = $"{outDir}/{targetName}.anim".Replace("\\", "/");

            var mergedClip = MergeClips(_manualClipA, _manualClipB, targetName);
            if (mergedClip != null)
            {
                AssetDatabase.CreateAsset(mergedClip, outPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(mergedClip);
                EditorUtility.DisplayDialog("成功", $"已成功保存合并动画至: {outPath}", "确定");
            }
        }

        /// <summary>
        /// 核心方法：将两个动画片段的曲线（Bone Transform 骨骼曲线 + SkinnedMeshRenderer BlendShape 曲线等）深度合并为一个新的独立 AnimationClip
        /// </summary>
        public static AnimationClip MergeClips(AnimationClip clipA, AnimationClip clipB, string newClipName)
        {
            if (clipA == null && clipB == null) return null;

            AnimationClip primaryClip = clipA != null ? clipA : clipB;
            AnimationClip secondaryClip = clipA != null ? clipB : null;

            var newClip = new AnimationClip
            {
                name = newClipName,
                frameRate = primaryClip.frameRate,
                legacy = primaryClip.legacy,
                wrapMode = primaryClip.wrapMode
            };

            // 1. 复制 Primary Clip 的所有浮点曲线 (Transform / BlendShape / Material / 属性等)
            var primaryFloatBindings = AnimationUtility.GetCurveBindings(primaryClip);
            foreach (var binding in primaryFloatBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(primaryClip, binding);
                AnimationUtility.SetEditorCurve(newClip, binding, curve);
            }

            // 2. 复制 Primary Clip 的所有引用曲线 (Sprite / Object Reference)
            var primaryObjBindings = AnimationUtility.GetObjectReferenceCurveBindings(primaryClip);
            foreach (var binding in primaryObjBindings)
            {
                var keyframes = AnimationUtility.GetObjectReferenceCurve(primaryClip, binding);
                AnimationUtility.SetObjectReferenceCurve(newClip, binding, keyframes);
            }

            // 3. 复制 Secondary Clip 的所有浮点曲线 (补充表情 BlendShape / 额外骨骼等)
            if (secondaryClip != null)
            {
                var secondaryFloatBindings = AnimationUtility.GetCurveBindings(secondaryClip);
                foreach (var binding in secondaryFloatBindings)
                {
                    var curve = AnimationUtility.GetEditorCurve(secondaryClip, binding);
                    // 写入到新 Clip 中（对于表情+姿态，通常 binding 不重复；若重复则以表情/次级片段数据为准或叠加）
                    AnimationUtility.SetEditorCurve(newClip, binding, curve);
                }

                var secondaryObjBindings = AnimationUtility.GetObjectReferenceCurveBindings(secondaryClip);
                foreach (var binding in secondaryObjBindings)
                {
                    var keyframes = AnimationUtility.GetObjectReferenceCurve(secondaryClip, binding);
                    AnimationUtility.SetObjectReferenceCurve(newClip, binding, keyframes);
                }
            }

            // 4. 复制事件 Events
            var events = AnimationUtility.GetAnimationEvents(primaryClip);
            if (events != null && events.Length > 0)
            {
                AnimationUtility.SetAnimationEvents(newClip, events);
            }

            // 5. 设置通用动画循环标记 (如果是 Loop 则保持)
            var primarySettings = AnimationUtility.GetAnimationClipSettings(primaryClip);
            AnimationUtility.SetAnimationClipSettings(newClip, primarySettings);

            return newClip;
        }
    }
}
