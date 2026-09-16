using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine.UIElements;


namespace Game.Editor.UI
{
    public class UIAutoBindEditorWindow : EditorWindow
    {
        // 支持的组件类型
        private enum ComponentType
        {
            Button,
            Text,
            InputField,
            TMP_Text,
            TMP_InputField,
            Image,
            RawImage,
            Slider,
            Toggle,
            ScrollRect,
            Scrollbar,
            VideoPlayer
        }

        // 绑定数据
        [System.Serializable]
        private class BindData
        {
            public GameObject targetObject;
            public ComponentType componentType;
            public string relativePath;
        }

        // 编辑器状态
        private RectTransform parent;
        private Dictionary<ComponentType, bool> componentTypeStates = new Dictionary<ComponentType, bool>();
        private List<BindData> bindResults = new List<BindData>();
        private Vector2 scrollPosition;
        private bool initialized = false;
        private bool selecting = false;
        private bool showHighlight = true; // 新增：是否显示高亮

        private void OnEnable()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemGUI;
        }

        private void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            if (!showHighlight) return;

            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            if (parent != null && obj == parent.gameObject)
            {
                DrawHighlightRect(selectionRect, new Color(0.2f, 0.6f, 1f, 0.25f)); // 蓝色高亮父节点
            }
            else if (IsBoundObject(obj))
            {
                DrawHighlightRect(selectionRect, new Color(0.4f, 0.8f, 0.4f, 0.25f)); // 绿色高亮绑定的子节点
            }
        }

        private bool IsBoundObject(GameObject obj)
        {
            if (bindResults == null || bindResults.Count == 0) return false;
            foreach (var data in bindResults)
            {
                if (data.targetObject == obj) return true;
            }
            return false;
        }

        private void DrawHighlightRect(Rect rect, Color color)
        {
            Rect drawRect = new Rect(rect);
            drawRect.xMin = 32; // x偏移避免遮挡下拉折叠箭头

            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(drawRect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
        }

        [MenuItem("Tools/UGUI/BindComponent")]
        public static void ShowWindow()
        {
            UIAutoBindEditorWindow window = GetWindow<UIAutoBindEditorWindow>("UI组件绑定工具");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void Initialize()
        {
            if (initialized) return;

            // 初始化组件类型状态
            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
            {
                componentTypeStates[type] = false; // 默认全不选
            }

            // 尝试自动选择当前选中的Canvas
            if (Selection.activeGameObject != null)
            {
                RectTransform selectedUiObject = Selection.activeGameObject.GetComponent<RectTransform>();
                if (selectedUiObject != null)
                {
                    parent = selectedUiObject;
                }
            }

            initialized = true;
        }

        private void OnGUI()
        {
            Initialize();

            EditorGUILayout.Space(10);

            // 主布局分为左右两部分
            EditorGUILayout.BeginHorizontal();

            // 左边布局
            DrawLeftPanel();

            // 右边布局
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(400));

            EditorGUILayout.LabelField("UI组件绑定工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 画布选择区域
            DrawCanvasSelection();

            EditorGUILayout.Space(10);

            // 组件类型选择区域
            DrawComponentTypeSelection();

            EditorGUILayout.Space(10);

            // 操作按钮区域
            DrawActionButtons();

            EditorGUILayout.EndVertical();
        }

        private void DrawCanvasSelection()
        {
            EditorGUILayout.LabelField("当前选择对象", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // 画布对象字段
            parent = (RectTransform)EditorGUILayout.ObjectField(parent, typeof(RectTransform), true);
            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                if (Selection.activeGameObject != null)
                {
                    RectTransform selectedUiObject = Selection.activeGameObject.GetComponent<RectTransform>();
                    if (selectedUiObject != null)
                    {
                        parent = selectedUiObject;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (parent != null)
            {
                EditorGUILayout.HelpBox($"当前选择对象: {parent.gameObject.name}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("请选择一个Ui对象", MessageType.Warning);
            }
        }

        private void DrawComponentTypeSelection()
        {
            EditorGUILayout.LabelField("绑定组件类型", EditorStyles.boldLabel);

            // 组件类型选择
            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
            {
                componentTypeStates[type] = EditorGUILayout.Toggle(type.ToString(), componentTypeStates[type]);
            }

            EditorGUILayout.Space(5);

            // 全选/取消全选按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选"))
            {
                foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
                {
                    componentTypeStates[type] = true;
                }
            }
            if (GUILayout.Button("取消全选"))
            {
                foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
                {
                    componentTypeStates[type] = false;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionButtons()
        {
            // 一键绑定按钮
            GUI.enabled = parent != null;
            if (GUILayout.Button("一键绑定组件", GUILayout.Height(30)))
            {
                BindComponents();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(5);

            // 一键复制代码按钮
            GUI.color = bindResults.Count > 0 ? Color.green : Color.red;
            GUI.enabled = bindResults.Count > 0;
            if (GUILayout.Button("一键复制C#代码", GUILayout.Height(30)))
            {
                CopyCodeToClipboard();
            }
            GUI.color = Color.white;
            GUI.enabled = true;

            //获取子对象相对路径按钮
            GUI.color = selecting? Color.yellow : Color.white;
            if (GUILayout.Button("获取子对象相对路径", GUILayout.Height(30)))
            {
                if (parent == null)
                {
                    EditorUtility.DisplayDialog("错误", "请先选择父对象", "确定");
                    return;
                }
                selecting = !selecting;
            }
            GUI.color = Color.white;
            if (selecting)
            {
                EditorGUILayout.HelpBox($"请在场景中选择子对象", MessageType.Info);
                if (Selection.activeGameObject != null && Selection.activeGameObject != parent.gameObject)
                {
                    Transform selectedTransform = Selection.activeGameObject.transform;
                    if (selectedTransform.IsChildOf(parent))
                    {
                        string relativePath = GetRelativePath(selectedTransform, parent);
                        GUIUtility.systemCopyBuffer = relativePath;
                        selecting = false;
                        // EditorUtility.DisplayDialog("成功", $"子对象{selectedTransform.gameObject.name}相对路径已复制到剪贴板", "确定");
                    }
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("高亮显示设置", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            showHighlight = EditorGUILayout.Toggle("在 Hierarchy 中显示高亮", showHighlight);
            if (EditorGUI.EndChangeCheck())
            {
                EditorApplication.RepaintHierarchyWindow();
            }

            if (GUILayout.Button("清除所有绑定与高亮", GUILayout.Height(30)))
            {
                bindResults.Clear();
                parent = null;
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField("绑定结果", EditorStyles.boldLabel);

            if (bindResults.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无绑定结果，请先绑定", MessageType.Info);
            }
            else
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                for (int i = 0; i < bindResults.Count; i++)
                {
                    DrawBindResultItem(i);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBindResultItem(int index)
        {
            if (index >= bindResults.Count) return;
            bool shouldRemove = false;
            BindData data = bindResults[index];

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            // 对象字段（只读，可点击聚焦）
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(data.targetObject, typeof(GameObject), true, GUILayout.Width(150));
            EditorGUI.EndDisabledGroup();

            // 组件类型标签
            EditorGUILayout.LabelField(data.componentType.ToString(), GUILayout.Width(100));

            // Remove按钮
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                shouldRemove = true;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            // 在布局结束后执行删除操作
            if (shouldRemove)
            {
                bindResults.RemoveAt(index);
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        private void BindComponents()
        {
            if (parent == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择目标Canvas", "确定");
                return;
            }

            bindResults.Clear();
            List<GameObject> highlightedObjects = new List<GameObject>();

            // 遍历Canvas下所有子对象
            Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child == parent.transform) continue;

                CheckAndAddComponent(child.gameObject, highlightedObjects);
            }

            // 高亮显示绑定的对象
            HighlightObjects(highlightedObjects);

            // // 显示结果
            // EditorUtility.DisplayDialog("完成", $"成功绑定 {bindResults.Count} 个组件", "确定");
            Repaint();
        }

        private void CheckAndAddComponent(GameObject obj, List<GameObject> highlightedObjects)
        {
            foreach (var kvp in componentTypeStates)
            {
                if (!kvp.Value) continue;

                Component component = null;
                string relativePath = GetRelativePath(obj.transform, parent.transform);

                switch (kvp.Key)
                {
                    case ComponentType.Button:
                        component = obj.GetComponent<UnityEngine.UI.Button>();
                        break;
                    case ComponentType.Text:
                        component = obj.GetComponent<UnityEngine.UI.Text>();
                        break;
                    case ComponentType.InputField:
                        component = obj.GetComponent<UnityEngine.UI.InputField>();
                        break;
                    case ComponentType.TMP_Text:
                        component = obj.GetComponent<TMPro.TMP_Text>();
                        break;
                    case ComponentType.TMP_InputField:
                        component = obj.GetComponent<TMPro.TMP_InputField>();
                        break;
                    case ComponentType.Image:
                        component = obj.GetComponent<UnityEngine.UI.Image>();
                        break;
                    case ComponentType.RawImage:
                        component = obj.GetComponent<UnityEngine.UI.RawImage>();
                        break;
                    case ComponentType.Slider:
                        component = obj.GetComponent<UnityEngine.UI.Slider>();
                        break;
                    case ComponentType.Toggle:
                        component = obj.GetComponent<UnityEngine.UI.Toggle>();
                        break;
                    case ComponentType.ScrollRect:
                        component = obj.GetComponent<UnityEngine.UI.ScrollRect>();
                        break;
                    case ComponentType.Scrollbar:
                        component = obj.GetComponent<UnityEngine.UI.Scrollbar>();
                        break;
                    case ComponentType.VideoPlayer:
                        component = obj.GetComponent<UnityEngine.Video.VideoPlayer>();
                        break;
                }

                if (component != null)
                {
                    bindResults.Add(new BindData
                    {
                        targetObject = obj,
                        componentType = kvp.Key,
                        relativePath = relativePath
                    });

                    highlightedObjects.Add(obj);
                    break; // 一个对象可能包含多个组件类型，但我们只添加一次
                }
            }
        }

        private string GetRelativePath(Transform child, Transform root)
        {
            List<string> pathParts = new List<string>();
            Transform current = child;

            while (current != null && current != root)
            {
                pathParts.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", pathParts);
        }

        private void HighlightObjects(List<GameObject> objects)
        {
            // 刷新 Hierarchy 窗口以应用自定义的背景高亮
            EditorApplication.RepaintHierarchyWindow();

            // 依然保留 Ping 以便引导视线并自动展开层级嵌套
            if (objects != null && objects.Count > 0)
            {
                EditorGUIUtility.PingObject(objects[0]);
            }
        }

        private void CopyCodeToClipboard()
        {
            if (bindResults.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有可复制的绑定结果", "确定");
                return;
            }

            StringBuilder codeBuilder = new StringBuilder();
            // 生成字段声明
            codeBuilder.AppendLine("// 自动生成的UI组件字段");
            foreach (var data in bindResults)
            {
                string typeName = GetComponentTypeName(data.componentType);
                string fieldName = GetValidFieldName(data.targetObject.name);
                codeBuilder.AppendLine($"public {typeName} {fieldName} {{get;private set;}}");
            }

            codeBuilder.AppendLine();

            // 生成绑定方法
            codeBuilder.AppendLine("private void BindUIComponents()");
            codeBuilder.AppendLine("{");
            codeBuilder.AppendLine("    // 自动绑定UI组件");

            foreach (var data in bindResults)
            {
                string typeName = GetComponentTypeName(data.componentType);
                string fieldName = GetValidFieldName(data.targetObject.name);

                codeBuilder.AppendLine($"    {fieldName} = transform.Find(\"{data.relativePath}\").GetComponent<{typeName}>();");
            }

            codeBuilder.AppendLine("}");
            codeBuilder.AppendLine();
            string finalCode = codeBuilder.ToString();
            GUIUtility.systemCopyBuffer = finalCode;

            EditorUtility.DisplayDialog("成功", "C#代码已复制到剪贴板", "确定");
        }

        private string GetComponentTypeName(ComponentType type)
        {
            switch (type)
            {
                case ComponentType.Button: return "Button";
                case ComponentType.Text: return "Text";
                case ComponentType.InputField: return "InputField";
                case ComponentType.TMP_Text: return "TMP_Text";
                case ComponentType.TMP_InputField: return "TMP_InputField";
                case ComponentType.Image: return "Image";
                case ComponentType.RawImage: return "RawImage";
                case ComponentType.Slider: return "Slider";
                case ComponentType.Toggle: return "Toggle";
                case ComponentType.ScrollRect: return "ScrollRect";
                case ComponentType.Scrollbar: return "Scrollbar";
                case ComponentType.VideoPlayer: return "UnityEngine.Video.VideoPlayer";
                default: return "Component";
            }
        }

        private string GetValidFieldName(string objectName)
        {
            // 清理对象名，使其成为有效的C#字段名
            string fieldName = objectName.Replace(" ", "_")
                                        .Replace("-", "_")
                                        .Replace("(", "")
                                        .Replace(")", "")
                                        .Replace(".", "_");

            // 确保字段名以字母开头
            if (fieldName.Length > 0 && !char.IsLetter(fieldName[0]))
            {
                fieldName = "ui_" + fieldName;
            }

            // 添加前缀避免与关键字冲突
            if (IsCSharpKeyword(fieldName))
            {
                fieldName = "ui_" + fieldName;
            }

            return fieldName; // 使用小写符合C#命名规范
        }

        private bool IsCSharpKeyword(string word)
        {
            string[] keywords = {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
                "char", "checked", "class", "const", "continue", "decimal", "default",
                "delegate", "do", "double", "else", "enum", "event", "explicit",
                "extern", "false", "finally", "fixed", "float", "for", "foreach",
                "goto", "if", "implicit", "in", "int", "interface", "internal",
                "is", "lock", "long", "namespace", "new", "null", "object",
                "operator", "out", "override", "params", "private", "protected",
                "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string", "struct", "switch",
                "this", "throw", "true", "try", "typeof", "uint", "ulong",
                "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
            };

            return Array.Exists(keywords, keyword => keyword == word.ToLower());
        }

        private void OnInspectorUpdate()
        {
            // 定期重绘以更新UI状态
            Repaint();
        }
    }
}
