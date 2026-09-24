using NUnit.Framework;

namespace ATEditor.Test
{
    [TestFixture]
    public class ProcessFactoryPoolTests
    {
        [SetUp]
        public void SetUp()
        {
            ProcessFactory.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ProcessFactory.Reset();
        }

        [Test]
        public void Factory_BindingAttribute_DiscoversRegisteredProcesses()
        {
            // 测试 MockClip 能否通过 [ProcessBinding(typeof(MockClip), PlayMode.Runtime)] 被自动发现并创建
            var clip = new MockClip(0f, 1f, "FactoryClip");
            var process = ProcessFactory.Create(clip, PlayMode.Runtime);

            Assert.IsNotNull(process, "ProcessFactory 必须通过特性反射正确识别 MockClip 并创建实例");
            Assert.IsInstanceOf<MockProcess>(process);
        }

        [Test]
        public void Pool_ReturnAndBorrow_CallsReset_ClearsPollution()
        {
            var clip = new MockClip(0f, 1f, "PoolClip");

            // 1. 首次借出
            var proc1 = ProcessFactory.Create(clip, PlayMode.Runtime) as MockProcess;
            Assert.IsNotNull(proc1);

            // 2. 模拟运行中产生的“脏数据”
            proc1.CustomOnEnterAction = (p) => { };
            proc1.OnUpdate(0.5f, 0.02f);
            Assert.AreEqual(0.5f, proc1.LastUpdateTime);
            Assert.IsNotNull(proc1.CustomOnEnterAction);

            // 3. 归还对象池
            ProcessFactory.Return(proc1);

            // 4. 二次借出，期望从池中取出同一实例，并在借出前自动调用 Reset() 洗净！
            var proc2 = ProcessFactory.Create(clip, PlayMode.Runtime) as MockProcess;
            Assert.IsNotNull(proc2);
            Assert.AreSame(proc1, proc2, "池中有可用实例时必须优先复用");

            // 5. 验证是否调用了 Reset() 并彻底清除了脏数据
            Assert.GreaterOrEqual(proc2.ResetCount, 1, "借出复用实例前必须自动调用 Reset()！");
            Assert.IsNull(proc2.CustomOnEnterAction, "委托引用必须在 Reset 中被清空");
            Assert.AreEqual(0f, proc2.LastUpdateTime, "内部运行态时间必须在 Reset 中被清零");
        }

        [Test]
        public void Pool_ClearPools_DiscardsCachedInstances()
        {
            var clip = new MockClip(0f, 1f, "PoolClip");

            var proc1 = ProcessFactory.Create(clip, PlayMode.Runtime);
            ProcessFactory.Return(proc1);

            // 清空池
            ProcessFactory.ClearPools();

            // 再次借出，池已被清空，应创建全新实例
            var proc2 = ProcessFactory.Create(clip, PlayMode.Runtime);
            Assert.IsNotNull(proc2);
            Assert.AreNotSame(proc1, proc2, "ClearPools 后必须实例化全新对象，不再返回已被废弃的旧实例");
        }
    }
}
