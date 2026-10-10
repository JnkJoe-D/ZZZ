using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Game.Editor.Workspace;

namespace GameClient.Tests.Editor
{
    /// <summary>
    /// Phase 1: WorkbenchPathResolver 统一路径解析服务单元测试
    /// </summary>
    public class WorkbenchPathResolverTests
    {
        [TearDown]
        public void TearDown()
        {
            WorkbenchPathResolver.ResetCustomRootProviders();
        }

        [Test]
        public void Test01_NormalizePath_CleansSlashesAndTrailing()
        {
            // 混合反斜杠与连续多斜杠
            string dirtyPath = "Assets\\\\Resources//Serializations///JSON//ActionTimelines/";
            string normalized = WorkbenchPathResolver.NormalizePath(dirtyPath);

            Assert.AreEqual("Assets/Resources/Serializations/JSON/ActionTimelines", normalized);
            Assert.IsFalse(normalized.EndsWith("/"));
            Assert.IsFalse(normalized.Contains("\\"));
            Assert.IsFalse(normalized.Contains("//"));
        }

        [Test]
        public void Test02_PathSecurity_InterceptsDirectoryTraversal()
        {
            string traversalPath1 = "Assets/Resources/../../../Windows/System32";
            string traversalPath2 = "../SecretData.json";
            string validPath = "Assets/Resources/Serializations/JSON/ActionTimelines/Role/Ellen";

            Assert.IsTrue(WorkbenchPathResolver.IsPathTraversing(traversalPath1));
            Assert.IsTrue(WorkbenchPathResolver.IsPathTraversing(traversalPath2));
            Assert.IsFalse(WorkbenchPathResolver.IsPathTraversing(validPath));

            Assert.IsFalse(WorkbenchPathResolver.ValidateSafePath(traversalPath1, out string err1));
            Assert.IsTrue(err1.Contains(".."));

            Assert.IsTrue(WorkbenchPathResolver.ValidateSafePath(validPath, out string err2));
            Assert.IsTrue(string.IsNullOrEmpty(err2));
        }

        [Test]
        public void Test03_AssetPathConversion_RelativeAndAbsolute()
        {
            string assetRel = "Assets/Editor/Settings/Workspace/SharedWorkspaces.json";
            Assert.IsTrue(WorkbenchPathResolver.IsProjectAssetPath(assetRel));

            string absPath = WorkbenchPathResolver.ToAbsoluteSystemPath(assetRel);
            Assert.IsTrue(Path.IsPathRooted(absPath));
            Assert.IsTrue(absPath.Contains("SharedWorkspaces.json"));

            string convertedBack = WorkbenchPathResolver.ToProjectAssetPath(absPath);
            Assert.AreEqual(assetRel, convertedBack);
        }

        [Test]
        public void Test04_RoleDirectoryResolution_MatchesConventions()
        {
            var ws = new SharedWorkspaceDefinition
            {
                Id = "Role_Ellen",
                Category = "Role",
                DisplayName = "Ellen (艾莲)",
                FolderName = "Role/Ellen"
            };

            string jsonDir = WorkbenchPathResolver.ResolveTimelineJsonDirectory(ws);
            string soDir = WorkbenchPathResolver.ResolveTimelineSoDirectory(ws);
            string actionDir = WorkbenchPathResolver.ResolveActionConfigDirectory(ws);

            Assert.AreEqual("Assets/Resources/Serializations/JSON/ActionTimelines/Role/Ellen", jsonDir);
            Assert.AreEqual("Assets/Resources/Serializations/ScriptableObjects/ActionTimelines/Role/Ellen", soDir);
            Assert.AreEqual("Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/Ellen/Action", actionDir);
        }

        [Test]
        public void Test05_DetectAssetKind_RecognizesKindsCorrectly()
        {
            Assert.AreEqual(
                WorkbenchAssetKind.TimelineJson,
                WorkbenchPathResolver.DetectAssetKind("Assets/Resources/Serializations/JSON/ActionTimelines/Role/Ellen/Atk_01.json")
            );

            Assert.AreEqual(
                WorkbenchAssetKind.TimelineSo,
                WorkbenchPathResolver.DetectAssetKind("Assets/Resources/Serializations/ScriptableObjects/ActionTimelines/Role/Ellen/Atk_01.asset")
            );

            Assert.AreEqual(
                WorkbenchAssetKind.ActionConfigAsset,
                WorkbenchPathResolver.DetectAssetKind("Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/Ellen/Action/Skill_01.asset")
            );

            Assert.AreEqual(
                WorkbenchAssetKind.Unknown,
                WorkbenchPathResolver.DetectAssetKind("Assets/Art/Textures/Icon.png")
            );
        }

        [Test]
        public void Test06_CounterpartResolution_EliminatesBrittleStringReplace()
        {
            // 场景 A: 给定 JSON 相对路径，推导对偶 SO 路径
            string sourceJson = "Assets/Resources/Serializations/JSON/ActionTimelines/Role/Ellen/Atk_01.json";
            bool ok1 = WorkbenchPathResolver.TryResolveCounterpartTimelinePath(
                sourceJson,
                WorkbenchAssetKind.TimelineSo,
                out string resolvedSo
            );

            Assert.IsTrue(ok1);
            Assert.AreEqual("Assets/Resources/Serializations/ScriptableObjects/ActionTimelines/Role/Ellen/Atk_01.asset", resolvedSo);

            // 场景 B: 给定 SO 相对路径，推导对偶 JSON 路径
            string sourceSo = "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines/Monster/TyrfingInfested/Die.asset";
            bool ok2 = WorkbenchPathResolver.TryResolveCounterpartTimelinePath(
                sourceSo,
                WorkbenchAssetKind.TimelineJson,
                out string resolvedJson
            );

            Assert.IsTrue(ok2);
            Assert.AreEqual("Assets/Resources/Serializations/JSON/ActionTimelines/Monster/TyrfingInfested/Die.json", resolvedJson);

            // 场景 C: 显式提供 workspaceFolderName
            bool ok3 = WorkbenchPathResolver.TryResolveCounterpartTimelinePath(
                "CustomDir/MyAction.json",
                WorkbenchAssetKind.TimelineSo,
                out string explicitSo,
                "Role/Anby"
            );

            Assert.IsTrue(ok3);
            Assert.AreEqual("Assets/Resources/Serializations/ScriptableObjects/ActionTimelines/Role/Anby/MyAction.asset", explicitSo);
        }

        [Test]
        public void Test07_CustomRootProviders_AllowsDynamicRedirection()
        {
            // 注入测试专用的临时根目录
            WorkbenchPathResolver.SetCustomRootProviders(
                charConfigProvider: () => "Assets/CustomGame/Actions",
                jsonRootProvider: () => "Assets/CustomGame/Timeline/Json",
                soRootProvider: () => "Assets/CustomGame/Timeline/Assets"
            );

            Assert.AreEqual("Assets/CustomGame/Actions", WorkbenchPathResolver.CharacterConfigRoot);
            Assert.AreEqual("Assets/CustomGame/Timeline/Json", WorkbenchPathResolver.TimelineJsonRoot);
            Assert.AreEqual("Assets/CustomGame/Timeline/Assets", WorkbenchPathResolver.TimelineSoRoot);

            string customJsonDir = WorkbenchPathResolver.ResolveTimelineJsonDirectory("Role/Ellen");
            Assert.AreEqual("Assets/CustomGame/Timeline/Json/Role/Ellen", customJsonDir);

            // 重置后应恢复默认
            WorkbenchPathResolver.ResetCustomRootProviders();
            Assert.AreEqual(WorkbenchPathResolver.DEFAULT_TIMELINE_JSON_ROOT, WorkbenchPathResolver.TimelineJsonRoot);
        }
    }
}
