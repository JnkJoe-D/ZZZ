using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Game.GamePlay;

namespace GameClient.Tests.Editor
{
    [TestFixture]
    [Category("Editor")]
    public class ActionConfigAssetEditorTests
    {
        private RoleActionConfigAsset _dummyAsset;
        private RoleActionConfigAsset _targetAsset;

        [SetUp]
        public void SetUp()
        {
            _dummyAsset = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            _dummyAsset.name = "Dummy_Action_A";
            _targetAsset = ScriptableObject.CreateInstance<RoleActionConfigAsset>();
            _targetAsset.name = "Dummy_Action_B";
        }

        [TearDown]
        public void TearDown()
        {
            if (_dummyAsset != null) UnityEngine.Object.DestroyImmediate(_dummyAsset);
            if (_targetAsset != null) UnityEngine.Object.DestroyImmediate(_targetAsset);
        }

        [Test]
        public void Test01_SerializedProperty_NonContiguousEnum_ResolvesTransitToAction()
        {
            _dummyAsset.CompleteMode = ActionCompleteMode.TransitToAction;
            _dummyAsset.CompleteAction = _targetAsset;

            var so = new SerializedObject(_dummyAsset);
            var modeProp = so.FindProperty("CompleteMode");
            var actionProp = so.FindProperty("CompleteAction");

            Assert.IsNotNull(modeProp, "CompleteMode 属性必须存在");
            Assert.IsNotNull(actionProp, "CompleteAction 属性必须存在");

            // 验证核心 Bug 根因：enumValueIndex 是下标（2），直接强转 (ActionCompleteMode)2 != TransitToAction (20)
            Assert.AreEqual(2, modeProp.enumValueIndex, "TransitToAction 在枚举数组中的下标应为 2");
            Assert.AreNotEqual(ActionCompleteMode.TransitToAction, (ActionCompleteMode)modeProp.enumValueIndex, 
                "证明历史 Bug：由于枚举值设定为 20，直接强转下标会得到错误的数值 (ActionCompleteMode)2");

            // 验证修复方案：通过 enumNames 进行安全解析
            string enumName = modeProp.enumNames[modeProp.enumValueIndex];
            bool success = Enum.TryParse<ActionCompleteMode>(enumName, out var resolvedMode);
            Assert.IsTrue(success, "通过 enumNames 解析应成功");
            Assert.AreEqual(ActionCompleteMode.TransitToAction, resolvedMode, "安全解析结果必须等于 TransitToAction");

            // 验证 intValue
            Assert.AreEqual(20, modeProp.intValue, "底层 intValue 必须正确对应 20");
            Assert.AreEqual(ActionCompleteMode.TransitToAction, (ActionCompleteMode)modeProp.intValue, "强转 intValue 必须为 TransitToAction");

            // 验证 CompleteAction 引用序列化
            Assert.AreEqual(_targetAsset, actionProp.objectReferenceValue, "CompleteAction 必须能够正确引用目标动作资产");
        }

        [Test]
        public void Test02_SerializedProperty_NonContiguousEnum_ResolvesStayAndDefault()
        {
            _dummyAsset.CompleteMode = ActionCompleteMode.Stay;
            var soStay = new SerializedObject(_dummyAsset);
            var modePropStay = soStay.FindProperty("CompleteMode");
            Assert.AreEqual(1, modePropStay.enumValueIndex);
            Assert.AreEqual(10, modePropStay.intValue);
            Assert.AreEqual("Stay", modePropStay.enumNames[modePropStay.enumValueIndex]);

            _dummyAsset.CompleteMode = ActionCompleteMode.Default;
            var soDefault = new SerializedObject(_dummyAsset);
            var modePropDefault = soDefault.FindProperty("CompleteMode");
            Assert.AreEqual(0, modePropDefault.enumValueIndex);
            Assert.AreEqual(0, modePropDefault.intValue);
            Assert.AreEqual("Default", modePropDefault.enumNames[modePropDefault.enumValueIndex]);
        }
    }
}
