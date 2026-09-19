using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    /// <summary>
    /// ATEditor 工作区管理窗口
    /// 支持查看、新建、修改已有工作区（如更新预制体引用）以及删除工作区
    /// </summary>
    public class ATWorkspaceManagerWindow : EditorWindow
    {
        private ATEditorWorkspaceDatabase database;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        // 当前选中的工作区（编辑模式）
        private ATWorkspaceDefinition selectedWorkspace;
        private ATWorkspaceDefinition editingBuffer;

        // 新建模式与分类管理模式标记
        private bool isCreatingNew = false;
        private bool isManagingCategories = false;
        private ATWorkspaceDefinition newBuffer;
        private string newCategoryInput = string.Empty;

        // 错误提示信息
        private string statusMessage = string.Empty;
        private MessageType statusMessageType = MessageType.Info;

        [MenuItem("ATEditor/工作区管理器")]
        public static void OpenWindow()
        {
            var window = GetWindow<ATWorkspaceManagerWindow>("工作区管理器");
            window.minSize = new Vector2(650, 420);
            window.Show();
        }

        public static void OpenForNewWorkspace()
        {
            var window = GetWindow<ATWorkspaceManagerWindow>("工作区管理器");
            window.minSize = new Vector2(650, 420);
            window.StartNewWorkspaceMode();
            window.Show();
        }

        private void OnEnable()
        {
            database = ATEditorWorkspaceDatabase.Instance;
            if (database != null && database.Workspaces.Count > 0 && selectedWorkspace == null)
            {
                SelectWorkspace(database.Workspaces[0]);
            }
        }

        private void SelectWorkspace(ATWorkspaceDefinition ws)
        {
            isCreatingNew = false;
            isManagingCategories = false;
            selectedWorkspace = ws;
            editingBuffer = ws != null ? ws.Clone() : null;
            statusMessage = string.Empty;
            GUI.FocusControl(null);
        }

        private void StartNewWorkspaceMode()
        {
            isCreatingNew = true;
            isManagingCategories = false;
            selectedWorkspace = null;
            newBuffer = new ATWorkspaceDefinition
            {
                Id = "Player_NewCharacter",
                Category = "Player",
                DisplayName = "新角色",
                FolderName = "Player/NewCharacter",
                SpawnPosition = Vector3.zero,
                SpawnRotationEuler = Vector3.zero
            };
            statusMessage = string.Empty;
            GUI.FocusControl(null);
        }

        private void OnGUI()
        {
            if (database == null)
            {
                database = ATEditorWorkspaceDatabase.Instance;
            }

            EditorGUILayout.BeginHorizontal();

            // 1. 左侧工作区列表
            DrawLeftPanel();

            // 分割线
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

            // 2. 右侧配置详情
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(220));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("工作区列表", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, EditorStyles.helpBox);

            var categories = database.GetCategories();
            foreach (var category in categories)
            {
                var list = database.GetWorkspacesByCategory(category);
                if (list.Count == 0 && category != "None" && category != "Player" && category != "Monster" && category != "Common")
                {
                    continue;
                }

                EditorGUILayout.LabelField($"▾ {category} ({list.Count})", EditorStyles.boldLabel);

                foreach (var ws in list)
                {
                    bool isSelected = !isCreatingNew && !isManagingCategories && selectedWorkspace != null && selectedWorkspace.Id == ws.Id;
                    GUIStyle style = isSelected ? new GUIStyle("SelectionRect") : EditorStyles.label;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(12);

                    string label = string.IsNullOrEmpty(ws.DisplayName) ? ws.Id : ws.DisplayName;
                    if (GUILayout.Button(label, style, GUILayout.Height(20)))
                    {
                        SelectWorkspace(ws);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            // 底部【+ 新建工作区】与【管理分类】按钮
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 新建工作区", GUILayout.Height(26)))
            {
                StartNewWorkspaceMode();
            }
            if (GUILayout.Button("管理分类", GUILayout.Height(26), GUILayout.Width(70)))
            {
                isManagingCategories = true;
                isCreatingNew = false;
                selectedWorkspace = null;
                statusMessage = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            EditorGUILayout.Space(4);

            if (isManagingCategories)
            {
                DrawCategoryManagerForm();
            }
            else if (isCreatingNew)
            {
                DrawNewWorkspaceForm();
            }
            else if (editingBuffer != null)
            {
                DrawEditWorkspaceForm();
            }
            else
            {
                EditorGUILayout.HelpBox("请在左侧选择一个工作区进行编辑，或点击【+ 新建工作区】。", MessageType.Info);
            }

            // 底部状态提示
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEditWorkspaceForm()
        {
            EditorGUILayout.LabelField($"编辑工作区: {editingBuffer.DisplayName}", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

            // ID (只读)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("唯一标识 (ID)", editingBuffer.Id);
            EditorGUI.EndDisabledGroup();

            // 分类下拉列表（检索已有分类）
            var categories = database.GetCategories();
            int catIndex = categories.IndexOf(editingBuffer.Category);
            if (catIndex < 0) catIndex = 0; // 默认 None
            int newCatIndex = EditorGUILayout.Popup("所属分类 (Category)", catIndex, categories.ToArray());
            editingBuffer.Category = categories[newCatIndex];

            // 显示名
            editingBuffer.DisplayName = EditorGUILayout.TextField("显示名称 (DisplayName)", editingBuffer.DisplayName);

            // 文件夹
            editingBuffer.FolderName = EditorGUILayout.TextField("物理子目录 (FolderName)", editingBuffer.FolderName);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("预览设置", EditorStyles.boldLabel);

            // 预览预制体（支持直接替换新模型）
            editingBuffer.PreviewPrefab = (GameObject)EditorGUILayout.ObjectField("绑定预览 Prefab", editingBuffer.PreviewPrefab, typeof(GameObject), false);

            editingBuffer.SpawnPosition = EditorGUILayout.Vector3Field("生成坐标偏移", editingBuffer.SpawnPosition);
            editingBuffer.SpawnRotationEuler = EditorGUILayout.Vector3Field("生成旋转 (Euler)", editingBuffer.SpawnRotationEuler);

            EditorGUILayout.Space(12);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("保存修改", GUILayout.Height(28), GUILayout.Width(100)))
            {
                SaveWorkspaceChanges();
            }

            GUILayout.Space(10);

            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("删除工作区", GUILayout.Height(28), GUILayout.Width(100)))
            {
                DeleteCurrentWorkspace();
            }
            GUI.backgroundColor = oldBg;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawNewWorkspaceForm()
        {
            EditorGUILayout.LabelField("新建角色工作区", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

            // 分类下拉列表（检索已有分类）
            var categories = database.GetCategories();
            int catIndex = categories.IndexOf(newBuffer.Category);
            if (catIndex < 0) catIndex = 0;
            int newCatIndex = EditorGUILayout.Popup("所属分类 (Category)", catIndex, categories.ToArray());
            newBuffer.Category = categories[newCatIndex];

            newBuffer.Id = EditorGUILayout.TextField("唯一标识 (ID)", newBuffer.Id);
            newBuffer.DisplayName = EditorGUILayout.TextField("显示名称 (DisplayName)", newBuffer.DisplayName);
            newBuffer.FolderName = EditorGUILayout.TextField("物理子目录 (FolderName)", newBuffer.FolderName);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("预览设置", EditorStyles.boldLabel);

            newBuffer.PreviewPrefab = (GameObject)EditorGUILayout.ObjectField("绑定预览 Prefab", newBuffer.PreviewPrefab, typeof(GameObject), false);
            newBuffer.SpawnPosition = EditorGUILayout.Vector3Field("生成坐标偏移", newBuffer.SpawnPosition);
            newBuffer.SpawnRotationEuler = EditorGUILayout.Vector3Field("生成旋转 (Euler)", newBuffer.SpawnRotationEuler);

            EditorGUILayout.Space(12);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("创建并激活工作区", GUILayout.Height(28), GUILayout.Width(140)))
            {
                CreateNewWorkspace();
            }
            GUILayout.Space(8);
            if (GUILayout.Button("取消", GUILayout.Height(28), GUILayout.Width(80)))
            {
                isCreatingNew = false;
                if (database.Workspaces.Count > 0)
                {
                    SelectWorkspace(database.Workspaces[0]);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawCategoryManagerForm()
        {
            EditorGUILayout.LabelField("分类管理", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• 系统预设分类 'None' 恒定常驻，不可编辑、不可删除。\n" +
                "• 删除某个分类时，该分类下的所有工作区将自动重置为 'None' 分类，绝不误删工作区及其动作资产。",
                MessageType.Info);
            EditorGUILayout.Space(6);

            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

            var categories = database.GetCategories();
            for (int i = 0; i < categories.Count; i++)
            {
                string cat = categories[i];
                bool isNone = string.Equals(cat, ATEditorWorkspaceDatabase.DefaultCategory, StringComparison.OrdinalIgnoreCase);

                EditorGUILayout.BeginHorizontal("box");

                if (isNone)
                {
                    EditorGUILayout.LabelField("📁 None  [系统预设，不可修改/删除]", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"📁 {cat}", EditorStyles.boldLabel, GUILayout.Width(200));

                    GUILayout.FlexibleSpace();

                    var oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("删除分类", GUILayout.Width(80)))
                    {
                        DeleteCategory(cat);
                    }
                    GUI.backgroundColor = oldBg;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("新建分类", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newCategoryInput = EditorGUILayout.TextField("分类名称", newCategoryInput);
            if (GUILayout.Button("添加分类", GUILayout.Width(90)))
            {
                AddNewCategory();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);
            if (GUILayout.Button("返回工作区编辑", GUILayout.Height(28), GUILayout.Width(130)))
            {
                isManagingCategories = false;
                if (database.Workspaces.Count > 0)
                {
                    SelectWorkspace(database.Workspaces[0]);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void AddNewCategory()
        {
            if (database.AddCategory(newCategoryInput, out string error))
            {
                statusMessage = $"分类 '{newCategoryInput}' 添加成功！";
                statusMessageType = MessageType.Info;
                newCategoryInput = string.Empty;
                GUI.FocusControl(null);
            }
            else
            {
                statusMessage = $"添加分类失败: {error}";
                statusMessageType = MessageType.Error;
            }
        }

        private void DeleteCategory(string catName)
        {
            int wsCount = database.GetWorkspacesByCategory(catName).Count;
            bool confirm = EditorUtility.DisplayDialog(
                "确认删除分类",
                $"确定要删除分类 '{catName}' 吗？\n" +
                $"注意：该分类下的 {wsCount} 个工作区将自动重置为 'None' 分类，不会被删除。",
                "确定删除",
                "取消");

            if (!confirm) return;

            if (database.DeleteCategory(catName, out string error))
            {
                statusMessage = $"分类 '{catName}' 已删除，其下工作区已归入 'None'。";
                statusMessageType = MessageType.Info;
            }
            else
            {
                statusMessage = $"删除分类失败: {error}";
                statusMessageType = MessageType.Error;
            }
        }

        private void SaveWorkspaceChanges()
        {
            if (selectedWorkspace == null || editingBuffer == null) return;

            string oldFolder = selectedWorkspace.FolderName?.Trim('/');
            string newFolder = editingBuffer.FolderName?.Trim('/');

            // 1. 检查物理子目录是否发生变更
            if (!string.Equals(oldFolder, newFolder, StringComparison.OrdinalIgnoreCase))
            {
                string soRoot = "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines";
                string jsonRoot = "Assets/Resources/Serializations/JSON/ActionTimelines";

                string oldSoDir = Path.Combine(soRoot, oldFolder).Replace("\\", "/");
                string oldJsonDir = Path.Combine(jsonRoot, oldFolder).Replace("\\", "/");

                bool oldSoExists = Directory.Exists(oldSoDir);
                bool oldJsonExists = Directory.Exists(oldJsonDir);

                if (oldSoExists || oldJsonExists)
                {
                    int choice = EditorUtility.DisplayDialogComplex(
                        "物理子目录变更",
                        $"检测到工作区物理子目录从:\n'{oldFolder}'\n变更为:\n'{newFolder}'\n\n是否同步重命名实际文件夹并将已有的动作资产移动到新目录？",
                        "同步重命名",
                        "仅更新配置",
                        "取消");

                    if (choice == 2) // 取消
                    {
                        return;
                    }

                    if (choice == 0) // 同步重命名
                    {
                        string newSoDir = Path.Combine(soRoot, newFolder).Replace("\\", "/");
                        string newJsonDir = Path.Combine(jsonRoot, newFolder).Replace("\\", "/");

                        RenameDirectoryWithMeta(oldSoDir, newSoDir);
                        RenameDirectoryWithMeta(oldJsonDir, newJsonDir);

                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    }
                    else if (choice == 1) // 仅更新配置
                    {
                        EnsureWorkspaceDirectories(editingBuffer);
                    }
                }
                else
                {
                    EnsureWorkspaceDirectories(editingBuffer);
                }
            }

            if (database.UpdateWorkspace(editingBuffer, out string error))
            {
                statusMessage = $"工作区 '{editingBuffer.DisplayName}' 修改已保存！";
                statusMessageType = MessageType.Info;

                selectedWorkspace = database.GetWorkspaceById(editingBuffer.Id);
                editingBuffer = selectedWorkspace != null ? selectedWorkspace.Clone() : null;

                // 如果当前编辑器打开且正在使用此工作区，立即触发热替换更新
                if (HasOpenInstances<ATEditorWindow>())
                {
                    var window = GetWindow<ATEditorWindow>();
                    if (window != null && window.State != null && window.State.ActiveWorkspaceId == editingBuffer.Id)
                    {
                        window.EnsureWorkspacePreviewTarget(forceRecreate: true);
                        window.InitPreview();
                        window.Repaint();
                        SceneView.RepaintAll();
                    }
                }
            }
            else
            {
                statusMessage = $"保存失败: {error}";
                statusMessageType = MessageType.Error;
            }
        }

        private void RenameDirectoryWithMeta(string oldDir, string newDir)
        {
            if (!Directory.Exists(oldDir)) return;

            string parentDir = Path.GetDirectoryName(newDir);
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            // 优先使用 AssetDatabase.MoveAsset 保持 Unity 内部关联
            string moveResult = AssetDatabase.MoveAsset(oldDir, newDir);
            if (!string.IsNullOrEmpty(moveResult))
            {
                // 如果 MoveAsset 失败，回退到文件系统重命名并迁移 .meta
                try
                {
                    Directory.Move(oldDir, newDir);
                    string oldMeta = oldDir + ".meta";
                    string newMeta = newDir + ".meta";
                    if (File.Exists(oldMeta))
                    {
                        File.Move(oldMeta, newMeta);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorkspaceManager] 重命名文件夹失败: {ex.Message}");
                }
            }
        }

        private void CreateNewWorkspace()
        {
            if (database.AddWorkspace(newBuffer, out string error))
            {
                // 确保物理目录存在
                EnsureWorkspaceDirectories(newBuffer);

                statusMessage = $"工作区 '{newBuffer.DisplayName}' 创建成功！";
                statusMessageType = MessageType.Info;

                string createdId = newBuffer.Id;
                isCreatingNew = false;
                SelectWorkspace(database.GetWorkspaceById(createdId));

                // 切换主窗口工作区
                if (HasOpenInstances<ATEditorWindow>())
                {
                    var window = GetWindow<ATEditorWindow>();
                    window?.SwitchWorkspace(createdId, true);
                }
            }
            else
            {
                statusMessage = $"创建失败: {error}";
                statusMessageType = MessageType.Error;
            }
        }

        private void DeleteCurrentWorkspace()
        {
            if (selectedWorkspace == null) return;

            bool confirm = EditorUtility.DisplayDialog(
                "确认删除工作区",
                $"确定要删除工作区 '{selectedWorkspace.DisplayName}' 吗？\n注意：这仅从工作区列表中注销，不会删除磁盘上的动作资产。",
                "确定删除",
                "取消");

            if (!confirm) return;

            string id = selectedWorkspace.Id;
            if (database.RemoveWorkspace(id))
            {
                statusMessage = $"工作区 '{id}' 已注销。";
                statusMessageType = MessageType.Info;

                if (database.Workspaces.Count > 0)
                {
                    SelectWorkspace(database.Workspaces[0]);
                }
                else
                {
                    selectedWorkspace = null;
                    editingBuffer = null;
                }

                // 如果删除的是当前主窗口激活的工作区，回退切换或清理
                if (HasOpenInstances<ATEditorWindow>())
                {
                    var window = GetWindow<ATEditorWindow>();
                    if (window != null && window.State != null && window.State.ActiveWorkspaceId == id)
                    {
                        string fallbackId = database.Workspaces.Count > 0 ? database.Workspaces[0].Id : null;
                        if (!string.IsNullOrEmpty(fallbackId))
                        {
                            window.SwitchWorkspace(fallbackId, false);
                        }
                        else
                        {
                            window.DestroyAllPreviewTargets();
                        }
                    }
                }
            }
        }

        private void EnsureWorkspaceDirectories(ATWorkspaceDefinition ws)
        {
            try
            {
                string soRoot = "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines";
                string jsonRoot = "Assets/Resources/Serializations/JSON/ActionTimelines";

                string soDir = Path.Combine(soRoot, ws.FolderName).Replace("\\", "/");
                string jsonDir = Path.Combine(jsonRoot, ws.FolderName).Replace("\\", "/");

                if (!Directory.Exists(soDir)) Directory.CreateDirectory(soDir);
                if (!Directory.Exists(jsonDir)) Directory.CreateDirectory(jsonDir);

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ATWorkspaceManager] 创建工作区目录异常: {ex.Message}");
            }
        }
    }
}
