using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Presentation
{
    public class VisualRotationOffsetTests
    {
        private GameObject _rootGo;
        private GameObject _visualGo;
        private CharacterVisualOffsetComponent _visualOffsetComp;

        [SetUp]
        public void SetUp()
        {
            _rootGo = new GameObject("TestCharacterRoot");
            _rootGo.transform.position = Vector3.zero;
            _rootGo.transform.rotation = Quaternion.identity;

            _visualGo = new GameObject("Visual");
            _visualGo.tag = "CharacterVisual";
            _visualGo.transform.SetParent(_rootGo.transform, false);
            _visualGo.transform.localPosition = Vector3.zero;
            _visualGo.transform.localRotation = Quaternion.identity;

            _visualOffsetComp = _rootGo.AddComponent<CharacterVisualOffsetComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_rootGo != null)
            {
                Object.DestroyImmediate(_rootGo);
            }
        }

        [Test]
        public void VisualRotationOffset_ApplyOffset_SetsLocalYawCorrectly()
        {
            _visualOffsetComp.SetVisualRotationOffset(-180f);

            float localYaw = Mathf.DeltaAngle(0f, _visualGo.transform.localRotation.eulerAngles.y);
            Assert.AreEqual(-180f, localYaw, 0.01f, "模型局部偏航角应被设置为 -180 度");
        }

        [Test]
        public void VisualRotationOffset_CounterRotation_CancelsWorldTurnPop()
        {
            // 模拟 TurnBack 动作 OnEnter：
            // 1. 父节点 GameObject 瞬间旋转 180 度（对齐目标输入方向）
            _rootGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // 2. 视觉模型同时注入 -180 度反向补偿
            _visualOffsetComp.SetVisualRotationOffset(-180f);

            // 3. 验证世界空间旋转：父节点(+180°) + 模型补偿(-180°) = 0°
            float worldYaw = Mathf.DeltaAngle(0f, _visualGo.transform.rotation.eulerAngles.y);
            Assert.AreEqual(0f, worldYaw, 0.01f, "世界空间下模型朝向应纹丝不动（0度），绝不发生反向跳变！");
        }

        [Test]
        public void VisualRotationOffset_Reset_ReturnsToIdentity()
        {
            _visualOffsetComp.SetVisualRotationOffset(-180f);
            _visualOffsetComp.ResetVisualRotationOffset();

            Assert.AreEqual(Quaternion.identity, _visualGo.transform.localRotation, "重置后局部旋转应恢复为单位四元数");
        }
    }
}
