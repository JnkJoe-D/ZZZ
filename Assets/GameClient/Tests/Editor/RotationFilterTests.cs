using ATEditor;
using Game.GamePlay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Movement
{
    public class RotationFilterTests
    {
        private GameObject _go;
        private MovementComponent _movement;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestEntity");
            _go.transform.position = Vector3.zero;
            _go.transform.rotation = Quaternion.identity;
            _movement = _go.AddComponent<MovementComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void RotationFilter_AxisMask_FiltersPitchAndRoll()
        {
            var clip = new RotationFilterClip
            {
                axisMask = new RotationAxisMask { FilterX = true, FilterY = false, FilterZ = true },
                enableMaxAngleLimit = false,
                lockToSingleDirection = false,
                exitAlignMode = RotationExitAlignMode.None
            };

            _movement.SetRotationFilter(clip);

            // 模拟带俯仰(30°)、偏航(45°)、翻滚(15°)的脏旋转增量
            Quaternion delta = Quaternion.Euler(30f, 45f, 15f);
            
            // 借助反射调用私有 ApplyRootRotation
            var method = typeof(MovementComponent).GetMethod("ApplyRootRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method);
            method.Invoke(_movement, new object[] { delta });

            Vector3 euler = _go.transform.rotation.eulerAngles;
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, euler.x), 0.01f, "Pitch 俯仰应被完全过滤置零");
            Assert.AreEqual(45f, Mathf.DeltaAngle(0f, euler.y), 0.01f, "Yaw 偏航应正常应用 45 度");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, euler.z), 0.01f, "Roll 翻滚应被完全过滤置零");
        }

        [Test]
        public void RotationFilter_MaxAngleLimit_ClampsAt180Degrees()
        {
            var clip = new RotationFilterClip
            {
                axisMask = new RotationAxisMask { FilterX = true, FilterY = false, FilterZ = true },
                enableMaxAngleLimit = true,
                maxAccumulatedAngle = 180f,
                lockToSingleDirection = false,
                exitAlignMode = RotationExitAlignMode.None
            };

            _movement.SetRotationFilter(clip);
            var method = typeof(MovementComponent).GetMethod("ApplyRootRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 分步旋转：100° -> 60° (累计 160°) -> 40° (本应累计 200°，但应截断至 180°)
            method.Invoke(_movement, new object[] { Quaternion.Euler(0f, 100f, 0f) });
            method.Invoke(_movement, new object[] { Quaternion.Euler(0f, 60f, 0f) });
            method.Invoke(_movement, new object[] { Quaternion.Euler(0f, 40f, 0f) });

            float finalYaw = Mathf.DeltaAngle(0f, _go.transform.rotation.eulerAngles.y);
            Assert.AreEqual(180f, finalYaw, 0.01f, "累计旋转角应被截断在最大 180 度");

            // 后续再有跑动晃动增量 (+10°)，应被彻底丢弃
            method.Invoke(_movement, new object[] { Quaternion.Euler(0f, 10f, 0f) });
            finalYaw = Mathf.DeltaAngle(0f, _go.transform.rotation.eulerAngles.y);
            Assert.AreEqual(180f, finalYaw, 0.01f, "超过 180 度后的晃动旋转应全部丢弃");
        }

        [Test]
        public void RotationFilter_UnidirectionalLock_DiscardsReverseJitter()
        {
            var clip = new RotationFilterClip
            {
                axisMask = new RotationAxisMask { FilterX = true, FilterY = false, FilterZ = true },
                enableMaxAngleLimit = false,
                lockToSingleDirection = true,
                exitAlignMode = RotationExitAlignMode.None
            };

            _movement.SetRotationFilter(clip);
            var method = typeof(MovementComponent).GetMethod("ApplyRootRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 主方向向正向旋转 60°
            method.Invoke(_movement, new object[] { Quaternion.Euler(0f, 60f, 0f) });
            
            // 出现反向晃动 -15° (晃荡回弹)，单向锁定下应被丢弃
            method.Invoke(_movement, new object[] { Quaternion.Euler(0f, -15f, 0f) });

            float finalYaw = Mathf.DeltaAngle(0f, _go.transform.rotation.eulerAngles.y);
            Assert.AreEqual(60f, finalYaw, 0.01f, "反向晃动增量应被单向锁定直接过滤");
        }

        [Test]
        public void RotationFilter_ExitAlign_SnapsTo180Degrees()
        {
            var clip = new RotationFilterClip
            {
                axisMask = new RotationAxisMask { FilterX = true, FilterY = false, FilterZ = true },
                enableMaxAngleLimit = false,
                lockToSingleDirection = false,
                exitAlignMode = RotationExitAlignMode.SnapToRelativeTarget,
                targetRelativeYaw = 180f
            };

            _movement.SetRotationFilter(clip);
            var method = typeof(MovementComponent).GetMethod("ApplyRootRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 动画旋转只转了 165°
            method.Invoke(_movement, new object[] { Quaternion.Euler(0f, 165f, 0f) });

            // 退出窗口，触发对齐
            _movement.ClearRotationFilter();

            float finalYaw = Mathf.DeltaAngle(0f, _go.transform.rotation.eulerAngles.y);
            Assert.AreEqual(180f, finalYaw, 0.01f, "退出时刻应强制对齐到 180 度");
        }

        [Test]
        public void RotationFilter_AllAxesFiltered_SuppressesExitAlignment()
        {
            var clip = new RotationFilterClip
            {
                axisMask = new RotationAxisMask { FilterX = true, FilterY = true, FilterZ = true }, // 全过滤
                enableMaxAngleLimit = true,
                maxAccumulatedAngle = 180f,
                lockToSingleDirection = true,
                exitAlignMode = RotationExitAlignMode.SnapToRelativeTarget,
                targetRelativeYaw = 180f
            };

            _movement.SetRotationFilter(clip);
            var method = typeof(MovementComponent).GetMethod("ApplyRootRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 动画发生旋转，但因为全过滤被完全阻断
            method.Invoke(_movement, new object[] { Quaternion.Euler(0f, 90f, 0f) });
            float midYaw = Mathf.DeltaAngle(0f, _go.transform.rotation.eulerAngles.y);
            Assert.AreEqual(0f, midYaw, 0.01f, "全过滤开启时，动画旋转应被完全阻断");

            // 退出窗口：全过滤状态下严禁发生 180 度吸附对齐！
            _movement.ClearRotationFilter();

            float finalYaw = Mathf.DeltaAngle(0f, _go.transform.rotation.eulerAngles.y);
            Assert.AreEqual(0f, finalYaw, 0.01f, "全过滤状态下严禁触发退出对齐吸附，必须保持原朝向不变");
        }
    }
}
