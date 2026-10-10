using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Game.Editor.Workspace;
using ATEditor.Editor;

namespace GameClient.Tests.Editor
{
    /// <summary>
    /// Phase 0 落地验收测试：公共工作区数据模型、JSON 序列化、分类与命名统一、ATEditor 双向感知与资产保护
    /// </summary>
    public class SharedWorkspacePhase0Tests
    {
        [Test]
        public void FixedCategories_AreStrictlyRoleMonsterCommon()
        {
            // 验证分类固定为 Role, Monster, Common
            var valid = SharedWorkspaceDefinition.ValidCategories;
            Assert.AreEqual(3, valid.Length);
            Assert.Contains("Role", valid);
            Assert.Contains("Monster", valid);
            Assert.Contains("Common", valid);

            // 验证非法分类被拦截
            var invalidDef = new SharedWorkspaceDefinition("Test_Invalid", "UnknownCategory", "测试", "Test/Invalid");
            bool ok = invalidDef.Validate(out string error);
            Assert.IsFalse(ok);
            Assert.IsTrue(error.Contains("分类"));
        }

        [Test]
        public void SharedWorkspaceFileIO_LoadsAndParsesStandardJson()
        {
            var data = SharedWorkspaceFileIO.Load();
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.Workspaces);
            Assert.IsTrue(data.Workspaces.Count >= 6, "至少应包含 Ellen, Anby, Unagi, QingYi, TyrfingInfested, Common");

            // 验证命名示例
            var ellen = data.FindById("Role_Ellen");
            Assert.IsNotNull(ellen, "应存在统一命名后的 Role_Ellen");
            Assert.AreEqual("Role", ellen.Category);
            Assert.AreEqual("Role/Ellen", ellen.FolderName);

            var tyrfing = data.FindById("Monster_TyrfingInfested");
            Assert.IsNotNull(tyrfing, "应存在统一命名后的 Monster_TyrfingInfested");
            Assert.AreEqual("Monster", tyrfing.Category);

            var common = data.FindById("Common_Shared");
            Assert.IsNotNull(common, "应存在统一命名后的 Common_Shared");
            Assert.AreEqual("Common", common.Category);
        }

        [Test]
        public void ATEditorWorkspaceDatabase_PreservesPreviewPrefabAssets()
        {
            var db = ATEditorWorkspaceDatabase.Instance;
            Assert.IsNotNull(db);

            // 验证分类已固定为 Role, Monster, Common
            var cats = db.GetCategories();
            Assert.AreEqual(3, cats.Count);
            Assert.Contains("Role", cats);
            Assert.Contains("Monster", cats);
            Assert.Contains("Common", cats);

            // 验证 Role_Ellen 及其 Prefab 资产完好无损
            var ellenWs = db.GetWorkspaceById("Role_Ellen");
            Assert.IsNotNull(ellenWs);
            Assert.AreEqual("Role", ellenWs.Category);
            Assert.IsNotNull(ellenWs.PreviewPrefab, "现有资产保护：艾莲的 PreviewPrefab 必须保持有效引用，不可丢失！");

            // 验证怪物与其它角色 Prefab 完好
            var anbyWs = db.GetWorkspaceById("Role_Anby");
            Assert.IsNotNull(anbyWs);
            Assert.IsNotNull(anbyWs.PreviewPrefab, "安比 PreviewPrefab 资产受保护");

            var monsterWs = db.GetWorkspaceById("Monster_TyrfingInfested");
            Assert.IsNotNull(monsterWs);
            Assert.IsNotNull(monsterWs.PreviewPrefab, "怪物 PreviewPrefab 资产受保护");
        }

        [Test]
        public void SharedWorkspaceWatcher_FiresNotification()
        {
            bool eventFired = false;
            Action handler = () => { eventFired = true; };

            SharedWorkspaceWatcher.OnWorkspacesFileChanged += handler;
            try
            {
                SharedWorkspaceWatcher.NotifyChanged();
                Assert.IsTrue(eventFired, "NotifyChanged 应成功派发变动通知给观察者");
            }
            finally
            {
                SharedWorkspaceWatcher.OnWorkspacesFileChanged -= handler;
            }
        }

        [Test]
        public void WorkspaceDirectoryPrefix_ComposesActualDirectoriesAndEliminatesHardcoding()
        {
            var db = ATEditorWorkspaceDatabase.Instance;
            Assert.IsNotNull(db);

            // 1. 验证全局目录前缀默认值
            Assert.AreEqual("Assets/Resources/Serializations/JSON/ActionTimelines", db.JsonRootDirectory);
            Assert.AreEqual("Assets/Resources/Serializations/ScriptableObjects/ActionTimelines", db.SoRootDirectory);
            Assert.AreEqual("Assets/Editor/Settings/Workspace/SharedWorkspaces.json", db.SharedWorkspaceJsonPath);

            // 2. 验证实际合成目录：前缀 + FolderName
            var ellen = db.GetWorkspaceById("Role_Ellen");
            Assert.IsNotNull(ellen);
            Assert.AreEqual("Role/Ellen", ellen.FolderName);

            string expectedJson = "Assets/Resources/Serializations/JSON/ActionTimelines/Role/Ellen";
            string expectedSo = "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines/Role/Ellen";

            Assert.AreEqual(expectedJson, db.GetWorkspaceJsonDirectory(ellen));
            Assert.AreEqual(expectedSo, db.GetWorkspaceAssetDirectory(ellen));

            // 3. 验证 ATEditorState 状态机已与 WorkspaceDatabase 统一绑定，消灭孤立硬编码
            var state = new ATEditorState();
            Assert.AreEqual(db.JsonRootDirectory, state.DefaultJsonDirectory);
            Assert.AreEqual(db.SoRootDirectory, state.DefaultAssetDirectory);
        }
    }
}
