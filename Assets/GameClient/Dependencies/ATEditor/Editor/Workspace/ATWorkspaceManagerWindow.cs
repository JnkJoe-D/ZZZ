using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Editor.Workspace;

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

        // 新建模式与工作区设置模式标记
        private bool isCreatingNew = false;
        private bool isWorkspaceSettings = false;
        private ATWorkspaceDefinition newBuffer;

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
            isWorkspaceSettings = false;
            selectedWorkspace = ws;
            editingBuffer = ws != null ? ws.Clone() : null;
            statusMessage = string.Empty;
            GUI.FocusControl(null);
        }

        private void StartNewWorkspaceMode()
        {
            isCreatingNew = true;
            isWorkspaceSettings = false;
            selectedWorkspace = null;
            newBuffer = new ATWorkspaceDefinition
            {
                Id = "Role_NewCharacter",
                Category = "Role",
                DisplayName = "新角色",
                FolderName = "Role/NewCharacter",
                SpawnPosition = Vector3.zero,
                SpawnRotationEuler = Vector3.zero
            };
            statusMessage = string.Empty;
            GUI.FocusControl(null);
        }

        private void OpenWorkspaceSettingsMode()
        {
            isWorkspaceSettings = true;
            isCreatingNew = false;
            selectedWorkspace = null;
            editingBuffer = null;
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
                    bool isSelected = !isCreatingNew && !isWorkspaceSettings && selectedWorkspace != null && selectedWorkspace.Id == ws.Id;
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

            // 底部【+ 新建工作区】与【设置】按钮
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 新建工作区", GUILayout.Height(26)))
            {
                StartNewWorkspaceMode();
            }
            if (GUILayout.Button("设置", GUILayout.Height(26), GUILayout.Width(70)))
            {
                OpenWorkspaceSettingsMode();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            EditorGUILayout.Space(4);

            if (isWorkspaceSettings)
            {
                DrawWorkspaceSettingsForm();
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

        private void DrawWorkspaceSettingsForm()
        {
            EditorGUILayout.LabelField("工作区设置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• 配置全局工作区目录前缀与公共数据源。实际角色目录 = 前缀 + 角色的物理子目录 (FolderName)。\n" +
                "• 修改前缀后，所有角色的实际时间轴读写路径将自动基于新前缀推导，灵活支持不同项目布局。\n" +
                "• 公共工作区配置文件 (SharedWorkspaces.json) 用于跨工具共享角色定义并双向感知增删。",
                MessageType.Info);
            EditorGUILayout.Space(6);

            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

            // 1. 读写目录前缀配置
            EditorGUILayout.LabelField("时间轴资产目录前缀配置", EditorStyles.boldLabel);
            
            // JSON 目录前缀
            EditorGUILayout.BeginHorizontal();
            string newJsonRoot = EditorGUILayout.TextField("JSON 目录前缀", database.JsonRootDirectory);
            if (newJsonRoot != database.JsonRootDirectory)
            {
                database.JsonRootDirectory = newJsonRoot;
            }
            if (GUILayout.Button("选择...", GUILayout.Width(60)))
            {
                string defaultPath = !string.IsNullOrEmpty(database.JsonRootDirectory) ? database.JsonRootDirectory : Application.dataPath;
                string selected = EditorUtility.OpenFolderPanel("选择 JSON 读写目录前缀", defaultPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    database.JsonRootDirectory = MakeProjectRelativePath(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            // SO 目录前缀
            EditorGUILayout.BeginHorizontal();
            string newSoRoot = EditorGUILayout.TextField("SO 目录前缀", database.SoRootDirectory);
            if (newSoRoot != database.SoRootDirectory)
            {
                database.SoRootDirectory = newSoRoot;
            }
            if (GUILayout.Button("选择...", GUILayout.Width(60)))
            {
                string defaultPath = !string.IsNullOrEmpty(database.SoRootDirectory) ? database.SoRootDirectory : Application.dataPath;
                string selected = EditorUtility.OpenFolderPanel("选择 SO 读写目录前缀", defaultPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    database.SoRootDirectory = MakeProjectRelativePath(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            // 动态合成目录实时预览
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📁 实际目录合成预览 (前缀 + FolderName)", EditorStyles.miniBoldLabel);
            
            var sampleWs = database.Workspaces.Count > 0 ? database.Workspaces[0] : null;
            if (sampleWs != null)
            {
                string sampleFolder = sampleWs.FolderName;
                string sampleJsonDir = database.GetWorkspaceJsonDirectory(sampleWs);
                string sampleSoDir = database.GetWorkspaceAssetDirectory(sampleWs);
                bool jsonDirExists = Directory.Exists(sampleJsonDir);
                bool soDirExists = Directory.Exists(sampleSoDir);

                EditorGUILayout.LabelField($"示例角色: {sampleWs.DisplayName} [{sampleWs.Id}] (子目录: {sampleFolder})", EditorStyles.miniLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• JSON 完整路径: {sampleJsonDir}", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Label(jsonDirExists ? "[已存在]" : "[未创建]", jsonDirExists ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel, GUILayout.Width(55));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• SO 完整路径: {sampleSoDir}", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Label(soDirExists ? "[已存在]" : "[未创建]", soDirExists ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel, GUILayout.Width(55));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("当前暂无已配置角色工作区", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 2. 公共配置数据源
            EditorGUILayout.LabelField("公共工作区数据源配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            string newSharedPath = EditorGUILayout.TextField("公共配置路径", database.SharedWorkspaceJsonPath);
            if (newSharedPath != database.SharedWorkspaceJsonPath)
            {
                database.SharedWorkspaceJsonPath = newSharedPath;
            }
            if (GUILayout.Button("选择...", GUILayout.Width(60)))
            {
                string defaultDir = Path.GetDirectoryName(database.SharedWorkspaceJsonPath);
                string selected = EditorUtility.OpenFilePanel("选择公共工作区配置文件 (SharedWorkspaces.json)", defaultDir, "json");
                if (!string.IsNullOrEmpty(selected))
                {
                    database.SharedWorkspaceJsonPath = MakeProjectRelativePath(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            // 文件状态检测
            string fullJsonPath = SharedWorkspaceFileIO.GetFullPath(database.SharedWorkspaceJsonPath);
            bool fileExists = File.Exists(fullJsonPath);
            EditorGUILayout.BeginHorizontal("box");
            if (fileExists)
            {
                EditorGUILayout.LabelField($"✓ 配置文件有效 (当前已加载 {database.Workspaces.Count} 个角色工作区)", EditorStyles.miniBoldLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"⚠ 文件不存在 (保存时将自动创建并初始化)", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            // 重新同步按钮行
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从公共文件重新加载", GUILayout.Height(24)))
            {
                database.SyncFromSharedJson(true);
                statusMessage = "已从公共配置文件刷新工作区列表！";
                statusMessageType = MessageType.Info;
            }
            if (GUILayout.Button("强制写回公共文件", GUILayout.Height(24)))
            {
                database.SaveToSharedJson();
                statusMessage = "已将当前工作区配置强制写回公共文件！";
                statusMessageType = MessageType.Info;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 3. 系统角色分类
            EditorGUILayout.LabelField("系统角色分类规范 (固定)", EditorStyles.boldLabel);
            var categories = database.GetCategories();
            for (int i = 0; i < categories.Count; i++)
            {
                string cat = categories[i];
                EditorGUILayout.BeginHorizontal("box");
                string desc = cat == "Role" ? "角色分类 (用于玩家操作实体)" :
                              cat == "Monster" ? "怪物分类 (用于敌人与Boss实体)" :
                              cat == "Common" ? "通用模板 (用于基础共享动作)" : "固定分类";
                EditorGUILayout.LabelField($"📁 {cat}  —  {desc}", EditorStyles.miniBoldLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12);

            // 4. 底部操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("确保所有工作区物理目录存在", GUILayout.Height(28)))
            {
                EnsureAllWorkspaceDirectories();
            }
            GUILayout.Space(8);
            if (GUILayout.Button("保存设置", GUILayout.Height(28), GUILayout.Width(90)))
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                statusMessage = "工作区设置已成功保存！";
                statusMessageType = MessageType.Info;
            }
            GUILayout.Space(8);
            if (GUILayout.Button("返回工作区编辑", GUILayout.Height(28), GUILayout.Width(110)))
            {
                isWorkspaceSettings = false;
                if (database.Workspaces.Count > 0)
                {
                    SelectWorkspace(database.Workspaces[0]);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private static string MakeProjectRelativePath(string fullOrRelativePath)
        {
            if (string.IsNullOrEmpty(fullOrRelativePath)) return string.Empty;
            string projectDir = Directory.GetCurrentDirectory().Replace('\\', '/');
            string normalized = fullOrRelativePath.Replace('\\', '/');
            if (normalized.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
            {
                string rel = normalized.Substring(projectDir.Length).TrimStart('/');
                return rel;
            }
            return normalized;
        }

        private void EnsureAllWorkspaceDirectories()
        {
            if (database == null || database.Workspaces == null) return;
            int created = 0;
            foreach (var ws in database.Workspaces)
            {
                string soDir = database.GetWorkspaceAssetDirectory(ws);
                string jsonDir = database.GetWorkspaceJsonDirectory(ws);
                if (!Directory.Exists(soDir)) { Directory.CreateDirectory(soDir); created++; }
                if (!Directory.Exists(jsonDir)) { Directory.CreateDirectory(jsonDir); created++; }
            }
            if (created > 0)
            {
                AssetDatabase.Refresh();
                statusMessage = $"已检查所有工作区，新建立 {created} 个缺失物理目录。";
            }
            else
            {
                statusMessage = "所有工作区的物理目录均已存在，无需新建。";
            }
            statusMessageType = MessageType.Info;
        }

        private void SaveWorkspaceChanges()
        {
            if (selectedWorkspace == null || editingBuffer == null) return;

            string oldFolder = selectedWorkspace.FolderName?.Trim('/');
            string newFolder = editingBuffer.FolderName?.Trim('/');

            // 1. 检查物理子目录是否发生变更
            if (!string.Equals(oldFolder, newFolder, StringComparison.OrdinalIgnoreCase))
            {
                string soRoot = database.SoRootDirectory;
                string jsonRoot = database.JsonRootDirectory;

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
                string soDir = database.GetWorkspaceAssetDirectory(ws);
                string jsonDir = database.GetWorkspaceJsonDirectory(ws);

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
