using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 统一配置与工作区管理中心视图
    /// 包含工作区全局通用设置、角色差异化覆盖以及公共角色工作区管理（新建、编辑、删除与目录初始化）。
    /// 严格采用纯中文界面与鼠标悬浮提示 (Tooltip)，杜绝中英混排与界面跳变。
    /// </summary>
    public static class WorkbenchSettingsView
    {
        private static int _subTab = 0; // 0: 工作区全局配置, 1: 角色差异化覆盖, 2: 角色工作区管理
        private static Vector2 _scrollPos;

        // 工作区配置草稿
        private static WorkbenchWorkspaceConfig _wsDraft;

        // 角色覆盖草稿
        private static string _loadedCharOverrideWorkspaceId;
        private static CharacterOverrideConfig _charDraft;

        // 新建工作区表单状态与联动
        private static bool _showCreateForm = false;
        private static readonly string[] CategoryOptions = new[]
        {
            SharedWorkspaceDefinition.CategoryRole,
            SharedWorkspaceDefinition.CategoryMonster,
            SharedWorkspaceDefinition.CategoryCommon
        };
        private static int _newWsCatIndex = 0;
        private static string _newWsId = "Role_NewCharacter";
        private static string _newWsDisplayName = "新角色";
        private static string _newWsFolderName = "Role/NewCharacter";
        private static bool _autoInitFolderOnCreate = true;

        // 编辑已有工作区状态
        private static string _editingWsId = null;
        private static int _editingWsCatIndex = 0;
        private static string _editingWsDisplayName = string.Empty;
        private static string _editingWsFolderName = string.Empty;

        public static void Draw(WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            // 1. 顶部二级子导航（纯中文）
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(24f));
            {
                string[] subTabs = new[] { "工作区全局配置", "角色差异化覆盖", "角色工作区管理" };
                _subTab = GUILayout.Toolbar(_subTab, subTabs, EditorStyles.toolbarButton, GUILayout.Width(360f));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                if (_subTab == 0)
                {
                    DrawWorkspaceSettingsPanel(onRequireRefresh);
                }
                else if (_subTab == 1)
                {
                    DrawCharacterOverridePanel(context, onRequireRefresh);
                }
                else
                {
                    DrawWorkspaceManagementPanel(context, onRequireRefresh);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawWorkspaceSettingsPanel(Action onRequireRefresh)
        {
            if (_wsDraft == null)
            {
                _wsDraft = WorkbenchConfigStorage.LoadWorkspaceConfig().Clone();
            }

            WorkbenchUIStyles.BeginCard("工作区全局共享配置");
            {
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = WorkbenchUIStyles.LabelWidth;

                // 1. ActionConfigRoot
                EditorGUILayout.BeginHorizontal();
                {
                    var labelContent = new GUIContent("动作资产根目录", "所有角色动作资产 (ActionConfigAsset) 存放的公共根目录");
                    _wsDraft.ActionConfigRoot = EditorGUILayout.TextField(labelContent, _wsDraft.ActionConfigRoot);
                    if (GUILayout.Button("选择...", EditorStyles.miniButton, GUILayout.Width(WorkbenchUIStyles.FixedButtonSmall)))
                    {
                        string selected = EditorUtility.OpenFolderPanel("选择动作资产根目录", _wsDraft.ActionConfigRoot, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            _wsDraft.ActionConfigRoot = WorkbenchPathResolver.ToProjectAssetPath(selected);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                // 2. TimelineJsonRoot
                EditorGUILayout.BeginHorizontal();
                {
                    var labelContent = new GUIContent("时间轴数据根目录", "所有角色时间轴 JSON 序列化数据文件存放的公共根目录");
                    _wsDraft.TimelineJsonRoot = EditorGUILayout.TextField(labelContent, _wsDraft.TimelineJsonRoot);
                    if (GUILayout.Button("选择...", EditorStyles.miniButton, GUILayout.Width(WorkbenchUIStyles.FixedButtonSmall)))
                    {
                        string selected = EditorUtility.OpenFolderPanel("选择时间轴数据根目录", _wsDraft.TimelineJsonRoot, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            _wsDraft.TimelineJsonRoot = WorkbenchPathResolver.ToProjectAssetPath(selected);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                // 3. TimelineAssetRoot
                EditorGUILayout.BeginHorizontal();
                {
                    var labelContent = new GUIContent("时间轴资产根目录", "所有角色时间轴 ScriptableObject 资产文件存放的公共根目录");
                    _wsDraft.TimelineAssetRoot = EditorGUILayout.TextField(labelContent, _wsDraft.TimelineAssetRoot);
                    if (GUILayout.Button("选择...", EditorStyles.miniButton, GUILayout.Width(WorkbenchUIStyles.FixedButtonSmall)))
                    {
                        string selected = EditorUtility.OpenFolderPanel("选择时间轴资产根目录", _wsDraft.TimelineAssetRoot, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            _wsDraft.TimelineAssetRoot = WorkbenchPathResolver.ToProjectAssetPath(selected);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("分类配表数据源配置", EditorStyles.boldLabel);

                // 4. 各分类技能配表路径
                _wsDraft.RoleSkillTablePath = DrawFileField(
                    new GUIContent("角色技能配表路径", "Role 分类角色默认读取的技能/动作配表 JSON 文件路径"),
                    _wsDraft.RoleSkillTablePath, "json");
                _wsDraft.MonsterSkillTablePath = DrawFileField(
                    new GUIContent("怪物技能配表路径", "Monster 分类怪物默认读取的技能/动作配表 JSON 文件路径"),
                    _wsDraft.MonsterSkillTablePath, "json");
                _wsDraft.CommonSkillTablePath = DrawFileField(
                    new GUIContent("通用技能配表路径", "Common 通用模板默认读取的技能/动作配表 JSON 文件路径"),
                    _wsDraft.CommonSkillTablePath, "json");

                EditorGUILayout.Space(6f);

                // 5. 命名模板
                var templateContent = new GUIContent("动作命名模板", "支持占位符：{Category} 分类, {RoleName} 角色名, {ActionName} 动作标识");
                _wsDraft.NamingTemplate = EditorGUILayout.TextField(templateContent, _wsDraft.NamingTemplate);

                // 6. 冲突策略
                var conflictContent = new GUIContent("默认冲突策略", "当目标资产文件已存在时的默认处理规则：跳过、覆盖或阻断报错");
                _wsDraft.DefaultConflictPolicy = (ConflictPolicy)EditorGUILayout.EnumPopup(conflictContent, _wsDraft.DefaultConflictPolicy);

                EditorGUILayout.Space(8f);

                // 保存按钮与重置按钮
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("保存全局配置", GUILayout.Width(130f), GUILayout.Height(26f)))
                    {
                        var report = WorkbenchConfigValidator.ValidateWorkspaceConfig(_wsDraft);
                        if (report.HasErrors)
                        {
                            EditorUtility.DisplayDialog("保存阻断", $"工作区配置存在错误：\n{report.Issues[0].Message}", "确定");
                        }
                        else
                        {
                            WorkbenchConfigStorage.SaveWorkspaceConfig(_wsDraft);
                            EditorUtility.DisplayDialog("提示", "工作区全局配置已安全保存！", "确定");
                            onRequireRefresh?.Invoke();
                        }
                    }

                    if (GUILayout.Button("恢复默认", GUILayout.Width(90f), GUILayout.Height(26f)))
                    {
                        if (EditorUtility.DisplayDialog("重置确认", "确定要将工作区配置恢复为系统默认基准值吗？", "确定", "取消"))
                        {
                            _wsDraft = WorkbenchWorkspaceConfig.CreateDefault();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
            WorkbenchUIStyles.EndCard();
        }

        private static string DrawFileField(GUIContent label, string path, string extension)
        {
            string result = path;
            EditorGUILayout.BeginHorizontal();
            {
                result = EditorGUILayout.TextField(label, path);
                if (GUILayout.Button("选择...", EditorStyles.miniButton, GUILayout.Width(WorkbenchUIStyles.FixedButtonSmall)))
                {
                    string dir = !string.IsNullOrEmpty(path) ? Path.GetDirectoryName(path) : "Assets/Configs";
                    string selected = EditorUtility.OpenFilePanel($"选择 {label.text}", dir, extension);
                    if (!string.IsNullOrEmpty(selected))
                    {
                        result = WorkbenchPathResolver.ToProjectAssetPath(selected);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            return result;
        }

        private static void DrawCharacterOverridePanel(WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            if (context == null)
            {
                EditorGUILayout.HelpBox("请先在顶部上下文栏选择一个具体角色，方可编辑该角色的差异化覆盖。", MessageType.Info);
                return;
            }

            string currentWsId = context.Workspace.Id;
            if (_charDraft == null || _loadedCharOverrideWorkspaceId != currentWsId)
            {
                _loadedCharOverrideWorkspaceId = currentWsId;
                _charDraft = WorkbenchConfigStorage.LoadCharacterOverride(currentWsId).Clone();
            }

            WorkbenchUIStyles.BeginCard($"角色差异化覆盖：{context.Workspace.DisplayName}");
            {
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = WorkbenchUIStyles.LabelWidth;

                // 1. Luban 角色绑定选择
                DrawLubanBindingField();

                EditorGUILayout.Space(4f);

                // 2. Action 子目录覆盖
                EditorGUILayout.BeginHorizontal();
                {
                    var content = new GUIContent("动作目录覆盖", "覆盖默认动作相对路径；留空则自动继承全局规则");
                    _charDraft.ActionSubFolderOverride = EditorGUILayout.TextField(content, _charDraft.ActionSubFolderOverride);
                    WorkbenchUIStyles.DrawSourceBadge(string.IsNullOrEmpty(_charDraft.ActionSubFolderOverride) ? ConfigSource.WorkspaceInherited : ConfigSource.CharacterOverride, 80f);
                }
                EditorGUILayout.EndHorizontal();

                // 3. Timeline 子目录覆盖
                EditorGUILayout.BeginHorizontal();
                {
                    var content = new GUIContent("时间轴目录覆盖", "覆盖默认时间轴相对路径；留空则自动继承全局规则");
                    _charDraft.TimelineSubFolderOverride = EditorGUILayout.TextField(content, _charDraft.TimelineSubFolderOverride);
                    WorkbenchUIStyles.DrawSourceBadge(string.IsNullOrEmpty(_charDraft.TimelineSubFolderOverride) ? ConfigSource.WorkspaceInherited : ConfigSource.CharacterOverride, 80f);
                }
                EditorGUILayout.EndHorizontal();

                // 4. 特殊命名规则覆盖
                EditorGUILayout.BeginHorizontal();
                {
                    var content = new GUIContent("命名模板覆盖", "覆盖全局动作命名模板；留空则自动继承全局规则");
                    _charDraft.NamingRuleOverride = EditorGUILayout.TextField(content, _charDraft.NamingRuleOverride);
                    WorkbenchUIStyles.DrawSourceBadge(string.IsNullOrEmpty(_charDraft.NamingRuleOverride) ? ConfigSource.WorkspaceInherited : ConfigSource.CharacterOverride, 80f);
                }
                EditorGUILayout.EndHorizontal();

                // 5. 专属配表数据源路径覆盖
                EditorGUILayout.BeginHorizontal();
                {
                    var content = new GUIContent("配表数据源覆盖", "覆盖默认技能配表 JSON 路径；留空则自动继承对应分类配置");
                    _charDraft.SkillTablePathOverride = EditorGUILayout.TextField(content, _charDraft.SkillTablePathOverride);
                    if (GUILayout.Button("选择...", EditorStyles.miniButton, GUILayout.Width(WorkbenchUIStyles.FixedButtonSmall)))
                    {
                        string selected = EditorUtility.OpenFilePanel("选择技能配表文件", "Assets/Configs", "json");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            _charDraft.SkillTablePathOverride = WorkbenchPathResolver.ToProjectAssetPath(selected);
                        }
                    }
                    WorkbenchUIStyles.DrawSourceBadge(string.IsNullOrEmpty(_charDraft.SkillTablePathOverride) ? ConfigSource.WorkspaceInherited : ConfigSource.CharacterOverride, 80f);
                }
                EditorGUILayout.EndHorizontal();

                // 6. 冲突策略覆盖
                EditorGUILayout.BeginHorizontal();
                {
                    var content = new GUIContent("覆盖冲突策略", "是否为此角色单独设定资产冲突处理策略");
                    _charDraft.HasConflictPolicyOverride = EditorGUILayout.Toggle(content, _charDraft.HasConflictPolicyOverride);
                    if (_charDraft.HasConflictPolicyOverride)
                    {
                        _charDraft.ConflictPolicyOverride = (ConflictPolicy)EditorGUILayout.EnumPopup(_charDraft.ConflictPolicyOverride, GUILayout.Width(130f));
                        WorkbenchUIStyles.DrawSourceBadge(ConfigSource.CharacterOverride, 80f);
                    }
                    else
                    {
                        WorkbenchUIStyles.DrawSourceBadge(ConfigSource.WorkspaceInherited, 80f);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(8f);

                // 操作按钮
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("保存角色覆盖", GUILayout.Width(130f), GUILayout.Height(26f)))
                    {
                        WorkbenchConfigStorage.SaveCharacterOverride(_charDraft);
                        EditorUtility.DisplayDialog("提示", $"已保存角色 [{currentWsId}] 的差异化覆盖配置！", "确定");
                        onRequireRefresh?.Invoke();
                    }

                    if (GUILayout.Button("清除覆盖并恢复继承", GUILayout.Width(150f), GUILayout.Height(26f)))
                    {
                        if (EditorUtility.DisplayDialog("确认", $"确定要删除角色 [{currentWsId}] 的全部覆盖配置并恢复继承吗？", "确定", "取消"))
                        {
                            WorkbenchConfigStorage.DeleteCharacterOverride(currentWsId);
                            _charDraft = new CharacterOverrideConfig { WorkspaceId = currentWsId };
                            onRequireRefresh?.Invoke();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
            WorkbenchUIStyles.EndCard();

            // 实时预览卡片
            DrawLivePreviewCard(context);
        }

        private static void DrawLubanBindingField()
        {
            var characters = LubanCharacterSourceAdapter.GetAllCharacters();
            string currentId = _charDraft.LubanCharacterId;

            int selectedIndex = 0;
            string[] displayOptions = new string[characters.Count + 1];
            displayOptions[0] = "-- 未绑定配表 --";

            for (int i = 0; i < characters.Count; i++)
            {
                var c = characters[i];
                displayOptions[i + 1] = $"[{c.CharacterId}] {c.DisplayName} ({c.EnumName})";
                if (!string.IsNullOrEmpty(currentId) &&
                    (string.Equals(c.EnumName, currentId, StringComparison.OrdinalIgnoreCase) ||
                     c.CharacterId.ToString() == currentId))
                {
                    selectedIndex = i + 1;
                }
            }

            var content = new GUIContent("配表角色绑定", "选择关联的 Luban 配表角色实体");
            int newIndex = EditorGUILayout.Popup(content, selectedIndex, displayOptions);
            if (newIndex != selectedIndex)
            {
                _charDraft.LubanCharacterId = newIndex == 0 ? string.Empty : characters[newIndex - 1].EnumName;
            }
        }

        private static void DrawLivePreviewCard(WorkbenchCharacterContext context)
        {
            WorkbenchUIStyles.BeginCard("实时生效路径解析预览");
            {
                // 基于当前草稿实时解析预览
                var previewEffective = EffectiveConfigResolver.Resolve(
                    context.Workspace,
                    WorkbenchConfigStorage.LoadWorkspaceConfig(),
                    _charDraft,
                    null
                );

                WorkbenchUIStyles.DrawReadOnlyField("最终动作目录", previewEffective.ActionConfigDirectory, previewEffective.ActionConfigDirectorySource);
                WorkbenchUIStyles.DrawReadOnlyField("最终时间轴数据目录", previewEffective.TimelineJsonDirectory, previewEffective.TimelineJsonDirectorySource);
                WorkbenchUIStyles.DrawReadOnlyField("最终时间轴资产目录", previewEffective.TimelineAssetDirectory, previewEffective.TimelineAssetDirectorySource);
                WorkbenchUIStyles.DrawReadOnlyField("最终配表数据源", previewEffective.SkillTablePath, previewEffective.SkillTablePathSource);
                WorkbenchUIStyles.DrawReadOnlyField("最终命名模板", previewEffective.NamingTemplate, previewEffective.NamingTemplateSource);
                WorkbenchUIStyles.DrawReadOnlyField("最终冲突策略", previewEffective.ConflictPolicy.ToString(), previewEffective.ConflictPolicySource);
            }
            WorkbenchUIStyles.EndCard();
        }

        private static void DrawWorkspaceManagementPanel(WorkbenchCharacterContext context, Action onRequireRefresh)
        {
            var sharedData = SharedWorkspaceFileIO.Load();
            sharedData.EnsureDefaults();

            // 1. 顶部操作栏
            WorkbenchUIStyles.BeginCard("角色工作区管理与资产目录初始化");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    _showCreateForm = GUILayout.Toggle(_showCreateForm, "➕ 新建角色工作区", EditorStyles.toolbarButton, GUILayout.Width(150f));

                    GUILayout.FlexibleSpace();

                    if (context?.EffectiveConfig != null)
                    {
                        if (GUILayout.Button("一键初始化当前角色资产目录", GUILayout.Width(200f), GUILayout.Height(24f)))
                        {
                            EnsureCharacterDirectoriesExist(context.EffectiveConfig);
                            EditorUtility.DisplayDialog("初始化完成", $"已确保角色 [{context.Workspace.DisplayName}] 的目录结构完整就绪！", "确定");
                            AssetDatabase.Refresh();
                            onRequireRefresh?.Invoke();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6f);

                // 2. 新建工作区表单
                if (_showCreateForm)
                {
                    DrawCreateWorkspaceForm(sharedData, onRequireRefresh);
                    EditorGUILayout.Space(6f);
                }

                // 3. 工作区表格展示与编辑
                DrawWorkspaceListTable(sharedData, onRequireRefresh);
            }
            WorkbenchUIStyles.EndCard();
        }

        private static void DrawCreateWorkspaceForm(SharedWorkspaceData sharedData, Action onRequireRefresh)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("新增角色工作区", EditorStyles.boldLabel);

                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 120f;

                // 1. 分类下拉列表（Popup）
                int selectedCat = EditorGUILayout.Popup(new GUIContent("角色分类", "选择工作区所属的顶层分类"), _newWsCatIndex, CategoryOptions);
                if (selectedCat != _newWsCatIndex)
                {
                    string oldCat = CategoryOptions[_newWsCatIndex];
                    string newCat = CategoryOptions[selectedCat];
                    _newWsCatIndex = selectedCat;

                    // 联动机制：若默认值未被用户修改，自动切换对应分类前缀
                    string oldDefaultId = $"{oldCat}_NewCharacter";
                    string newDefaultId = $"{newCat}_NewCharacter";
                    if (string.IsNullOrEmpty(_newWsId) || _newWsId == oldDefaultId)
                    {
                        _newWsId = newDefaultId;
                    }

                    string oldDefaultFolder = $"{oldCat}/NewCharacter";
                    string newDefaultFolder = $"{newCat}/NewCharacter";
                    if (string.IsNullOrEmpty(_newWsFolderName) || _newWsFolderName == oldDefaultFolder)
                    {
                        _newWsFolderName = newDefaultFolder;
                    }
                }

                // 2. 唯一标识
                _newWsId = EditorGUILayout.TextField(new GUIContent("唯一标识", "工作区全局唯一键，例如 Role_Ellen"), _newWsId);

                // 3. 显示名称
                _newWsDisplayName = EditorGUILayout.TextField(new GUIContent("显示名称", "在界面中呈现的角色名称，例如 艾莲"), _newWsDisplayName);

                // 4. 相对目录（原物理子目录）
                _newWsFolderName = EditorGUILayout.TextField(new GUIContent("相对目录", "角色资产在工程根目录下的相对文件夹路径，例如 Role/Ellen"), _newWsFolderName);

                // 5. 自动初始化
                _autoInitFolderOnCreate = EditorGUILayout.Toggle(new GUIContent("自动创建相对目录", "创建时自动在工程中建立动作与时间轴对应的物理文件夹"), _autoInitFolderOnCreate);

                EditorGUIUtility.labelWidth = oldWidth;

                EditorGUILayout.Space(4f);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("确认创建并保存", GUILayout.Width(130f), GUILayout.Height(24f)))
                    {
                        if (string.IsNullOrWhiteSpace(_newWsId) || string.IsNullOrWhiteSpace(_newWsFolderName))
                        {
                            EditorUtility.DisplayDialog("提示", "工作区唯一标识和相对目录不能为空！", "确定");
                        }
                        else if (sharedData.Workspaces.Any(w => string.Equals(w.Id, _newWsId.Trim(), StringComparison.OrdinalIgnoreCase)))
                        {
                            EditorUtility.DisplayDialog("提示", $"已存在相同唯一标识的工作区: {_newWsId}", "确定");
                        }
                        else
                        {
                            var newDef = new SharedWorkspaceDefinition
                            {
                                Id = _newWsId.Trim(),
                                Category = CategoryOptions[_newWsCatIndex],
                                DisplayName = !string.IsNullOrWhiteSpace(_newWsDisplayName) ? _newWsDisplayName.Trim() : _newWsId.Trim(),
                                FolderName = _newWsFolderName.Trim()
                            };

                            sharedData.Workspaces.Add(newDef);
                            SharedWorkspaceFileIO.Save(sharedData);

                            if (_autoInitFolderOnCreate)
                            {
                                var effective = EffectiveConfigResolver.Resolve(newDef);
                                EnsureCharacterDirectoriesExist(effective);
                                AssetDatabase.Refresh();
                            }

                            EditorUtility.DisplayDialog("成功", $"已成功创建工作区 [{newDef.DisplayName}]！", "确定");
                            _showCreateForm = false;
                            // 重置表单为默认值
                            _newWsCatIndex = 0;
                            _newWsId = "Role_NewCharacter";
                            _newWsDisplayName = "新角色";
                            _newWsFolderName = "Role/NewCharacter";
                            onRequireRefresh?.Invoke();
                        }
                    }

                    if (GUILayout.Button("取消", GUILayout.Width(80f), GUILayout.Height(24f)))
                    {
                        _showCreateForm = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawWorkspaceListTable(SharedWorkspaceData sharedData, Action onRequireRefresh)
        {
            EditorGUILayout.BeginHorizontal(WorkbenchUIStyles.TableHeader);
            {
                GUILayout.Label("#", EditorStyles.miniBoldLabel, GUILayout.Width(WorkbenchUIStyles.ColIndexWidth));
                GUILayout.Label("唯一标识", EditorStyles.miniBoldLabel, GUILayout.Width(170f));
                GUILayout.Label("分类", EditorStyles.miniBoldLabel, GUILayout.Width(75f));
                GUILayout.Label("显示名称", EditorStyles.miniBoldLabel, GUILayout.Width(140f));
                GUILayout.Label("相对目录", EditorStyles.miniBoldLabel, GUILayout.Width(170f));
                GUILayout.Label("目录状态", EditorStyles.miniBoldLabel, GUILayout.Width(95f));
                GUILayout.Label("操作", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < sharedData.Workspaces.Count; i++)
            {
                var ws = sharedData.Workspaces[i];
                var effective = EffectiveConfigResolver.Resolve(ws);
                bool dirExists = Directory.Exists(effective.ActionConfigDirectory);
                bool isEditingThis = (_editingWsId == ws.Id);

                Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
                if (i % 2 == 1)
                {
                    EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.18f, 0.18f, 0.35f));
                }

                {
                    GUILayout.Label((i + 1).ToString(), EditorStyles.miniLabel, GUILayout.Width(WorkbenchUIStyles.ColIndexWidth), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
                    GUILayout.Label(ws.Id, EditorStyles.boldLabel, GUILayout.Width(170f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
                    GUILayout.Label(ws.Category, EditorStyles.miniLabel, GUILayout.Width(75f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
                    GUILayout.Label(ws.DisplayName, EditorStyles.label, GUILayout.Width(140f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));
                    GUILayout.Label(ws.FolderName, EditorStyles.miniLabel, GUILayout.Width(170f), GUILayout.Height(WorkbenchUIStyles.FixedRowHeight));

                    WorkbenchUIStyles.DrawStatusBadge(dirExists, dirExists ? "目录已就绪" : "未建目录", 90f);

                    EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    {
                        if (isEditingThis)
                        {
                            if (GUILayout.Button("收起", EditorStyles.miniButton, GUILayout.Width(50f)))
                            {
                                GUI.FocusControl(null);
                                _editingWsId = null;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("编辑", EditorStyles.miniButton, GUILayout.Width(50f)))
                            {
                                GUI.FocusControl(null);
                                _editingWsId = ws.Id;
                                _editingWsCatIndex = Array.FindIndex(CategoryOptions, c => string.Equals(c, ws.Category, StringComparison.OrdinalIgnoreCase));
                                if (_editingWsCatIndex < 0) _editingWsCatIndex = 0;
                                _editingWsDisplayName = ws.DisplayName;
                                _editingWsFolderName = ws.FolderName;
                            }
                        }

                        if (!dirExists)
                        {
                            if (GUILayout.Button("创建目录", EditorStyles.miniButton, GUILayout.Width(65f)))
                            {
                                EnsureCharacterDirectoriesExist(effective);
                                AssetDatabase.Refresh();
                                onRequireRefresh?.Invoke();
                            }
                        }

                        if (GUILayout.Button("删除", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            if (EditorUtility.DisplayDialog("删除确认", $"确定删除角色工作区 [{ws.DisplayName}] 吗？", "确定", "取消"))
                            {
                                GUI.FocusControl(null);
                                sharedData.Workspaces.RemoveAt(i);
                                SharedWorkspaceFileIO.Save(sharedData);
                                if (_editingWsId == ws.Id) _editingWsId = null;
                                onRequireRefresh?.Invoke();
                                break;
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndHorizontal();

                // 内联编辑表单
                if (isEditingThis)
                {
                    DrawInlineEditWorkspaceForm(ws, sharedData, onRequireRefresh);
                }
            }
        }

        private static void DrawInlineEditWorkspaceForm(SharedWorkspaceDefinition ws, SharedWorkspaceData sharedData, Action onRequireRefresh)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"编辑工作区：{ws.DisplayName} ({ws.Id})", EditorStyles.boldLabel);

                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 110f;

                _editingWsCatIndex = EditorGUILayout.Popup(new GUIContent("角色分类", "更改工作区所属分类"), _editingWsCatIndex, CategoryOptions);

                GUI.SetNextControlName($"ws_edit_name_{ws.Id}");
                _editingWsDisplayName = EditorGUILayout.TextField(new GUIContent("显示名称", "界面的角色显示名"), _editingWsDisplayName);

                GUI.SetNextControlName($"ws_edit_folder_{ws.Id}");
                _editingWsFolderName = EditorGUILayout.TextField(new GUIContent("相对目录", "角色资产在工程中的相对子路径"), _editingWsFolderName);

                EditorGUIUtility.labelWidth = oldWidth;

                EditorGUILayout.Space(4f);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("保存修改", GUILayout.Width(100f), GUILayout.Height(22f)))
                    {
                        if (string.IsNullOrWhiteSpace(_editingWsDisplayName) || string.IsNullOrWhiteSpace(_editingWsFolderName))
                        {
                            EditorUtility.DisplayDialog("提示", "显示名称和相对目录不能为空！", "确定");
                        }
                        else
                        {
                            GUI.FocusControl(null);
                            ws.Category = CategoryOptions[_editingWsCatIndex];
                            ws.DisplayName = _editingWsDisplayName.Trim();
                            ws.FolderName = _editingWsFolderName.Trim();

                            SharedWorkspaceFileIO.Save(sharedData);
                            EditorUtility.DisplayDialog("提示", "工作区修改已成功保存！", "确定");
                            _editingWsId = null;
                            onRequireRefresh?.Invoke();
                        }
                    }

                    if (GUILayout.Button("取消", GUILayout.Width(70f), GUILayout.Height(22f)))
                    {
                        GUI.FocusControl(null);
                        _editingWsId = null;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private static void EnsureCharacterDirectoriesExist(EffectiveWorkbenchConfig cfg)
        {
            if (cfg == null) return;

            if (!string.IsNullOrEmpty(cfg.ActionConfigDirectory) && !Directory.Exists(cfg.ActionConfigDirectory))
            {
                Directory.CreateDirectory(cfg.ActionConfigDirectory);
            }
            if (!string.IsNullOrEmpty(cfg.TimelineJsonDirectory) && !Directory.Exists(cfg.TimelineJsonDirectory))
            {
                Directory.CreateDirectory(cfg.TimelineJsonDirectory);
            }
            if (!string.IsNullOrEmpty(cfg.TimelineAssetDirectory) && !Directory.Exists(cfg.TimelineAssetDirectory))
            {
                Directory.CreateDirectory(cfg.TimelineAssetDirectory);
            }
        }
    }
}
