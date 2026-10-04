using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game.Presentation.VFX.CrossFlash;

namespace Game.Editor
{
    [CustomEditor(typeof(CrossFlashTester))]
    public class CrossFlashTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var tester = (CrossFlashTester)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("快捷测试控制 (Play 模式下生效)", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(1f, 0.75f, 0.2f);
                if (GUILayout.Button("▶ 触发十字闪光 (Space)", GUILayout.Height(36)))
                {
                    if (Application.isPlaying)
                    {
                        tester.TriggerFlash();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "请先点击 Unity 顶部的 Play 运行按钮进入播放模式，再进行实时交互测试！", "确定");
                    }
                }

                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
                if (GUILayout.Button("■ 一键清空所有 (C)", GUILayout.Height(36)))
                {
                    if (Application.isPlaying)
                    {
                        tester.ClearAllFlashes();
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "【使用说明】\n" +
                "1. 在单独场景中创建一个空 GameObject 并挂载当前 CrossFlashTester 脚本；\n" +
                "2. 拖拽任意 GameObject (如空节点或相机前方的立方体) 到 Target Center 作为十字中心；\n" +
                "3. 点击 Unity 运行 (Play) 按钮；\n" +
                "4. 按键盘【空格键 Space】多次触发闪光 (支持最多 8 实例并发)；\n" +
                "5. 按键盘【C 键】一键清除所有活跃闪光；\n" +
                "6. 勾选面板上的「Constant Preview (常驻预览)」可保持十字常驻屏幕，便于微调线宽、柔边、颜色等参数。",
                MessageType.Info);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("一键将 CrossFlashRendererFeature 添加到项目 URP 资产", GUILayout.Height(28)))
            {
                InjectRendererFeatureToAllURPData();
            }
        }

        private static void InjectRendererFeatureToAllURPData()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");
            int addedCount = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (rendererData == null) continue;

                bool alreadyExists = false;
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature is CrossFlashRendererFeature)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    var newFeature = ScriptableObject.CreateInstance<CrossFlashRendererFeature>();
                    newFeature.name = "CrossFlashRendererFeature";
                    AssetDatabase.AddObjectToAsset(newFeature, rendererData);

                    // 利用反射安全将 Feature 加入 m_RendererFeatures 列表
                    var featuresField = typeof(ScriptableRendererData).GetField("m_RendererFeatures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (featuresField != null)
                    {
                        var list = featuresField.GetValue(rendererData) as System.Collections.Generic.List<ScriptableRendererFeature>;
                        if (list != null)
                        {
                            list.Add(newFeature);
                            rendererData.SetDirty();
                            EditorUtility.SetDirty(rendererData);
                            addedCount++;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("URP 特性配置完成", $"已成功为 {addedCount} 个 URP RendererData 添加并激活 CrossFlashRendererFeature！", "太棒了");
        }
    }
}
