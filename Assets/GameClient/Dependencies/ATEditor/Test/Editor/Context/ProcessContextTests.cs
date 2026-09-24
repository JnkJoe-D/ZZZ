using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ATEditor.Test
{
    public interface IDummyTestService : IService
    {
        string GetMessage();
    }

    public class DummyTestService : IDummyTestService
    {
        public string GetMessage() => "HelloTest";
        public void Initialize() { }
    }

    [TestFixture]
    public class ProcessContextTests
    {
        private GameObject _ownerGo;
        private ProcessContext _context;

        [SetUp]
        public void SetUp()
        {
            _ownerGo = new GameObject("TestOwner_Context");
            _context = new ProcessContext(_ownerGo, PlayMode.Runtime);
        }

        [TearDown]
        public void TearDown()
        {
            if (_ownerGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_ownerGo);
            }
        }

        [Test]
        public void Service_LazyLoading_CachesOnFirstAccess()
        {
            int factoryCallCount = 0;
            Func<Type, GameObject, object> provider = (type, go) =>
            {
                factoryCallCount++;
                if (type == typeof(IDummyTestService))
                {
                    return new DummyTestService();
                }
                return null;
            };

            var ctxWithProvider = new ProcessContext(_ownerGo, PlayMode.Runtime, provider);

            // 1. 首次访问，触发懒加载委托
            var s1 = ctxWithProvider.GetService<IDummyTestService>();
            Assert.IsNotNull(s1);
            Assert.AreEqual("HelloTest", s1.GetMessage());
            Assert.AreEqual(1, factoryCallCount, "首次获取必须触发 serviceProvider 委托");

            // 2. 第二次访问，应直接从 _services 缓存中命中，不重复触发委托
            var s2 = ctxWithProvider.GetService<IDummyTestService>();
            Assert.AreSame(s1, s2, "两次获取的对象必须是同一实例");
            Assert.AreEqual(1, factoryCallCount, "后续获取严禁重复调用 serviceProvider！");
        }

        [Test]
        public void Cleanup_DeduplicationByKey_ExecutesOnlyLatest()
        {
            var executionLog = new List<string>();

            // 注册同一个 key "SharedAudioCleanup" 两次（模拟同类 Process 注册相同的系统清理）
            _context.RegisterCleanup("SharedAudioCleanup", () =>
            {
                executionLog.Add("FirstCallback");
            });

            _context.RegisterCleanup("SharedAudioCleanup", () =>
            {
                executionLog.Add("SecondCallback");
            });

            // 注册一个独立的匿名清理
            _context.RegisterCleanup(() =>
            {
                executionLog.Add("AnonymousCallback");
            });

            // 执行清理
            InvokeInternalMethod(_context, "ExecuteCleanups");

            // 预期：同 key 的 "FirstCallback" 被后来的 "SecondCallback" 覆盖，只执行一次！
            Assert.IsFalse(executionLog.Contains("FirstCallback"), "同 key 的旧回调必须被覆盖去重");
            Assert.IsTrue(executionLog.Contains("SecondCallback"), "同 key 的最新回调必须被执行");
            Assert.IsTrue(executionLog.Contains("AnonymousCallback"));
            Assert.AreEqual(2, executionLog.Count, "总清理执行次数应精确为 2");

            // 再次调用 ExecuteCleanups，应已被清空，不应产生二次调用
            executionLog.Clear();
            InvokeInternalMethod(_context, "ExecuteCleanups");
            Assert.AreEqual(0, executionLog.Count, "清理执行后列表必须已被彻底清空");
        }

        [Test]
        public void Action_StartActions_ExecutedOnceOnly()
        {
            int actionExecutedCount = 0;
            _context.RegisterStartAction("TestAction", () =>
            {
                actionExecutedCount++;
            });

            // 连续执行多次
            InvokeInternalMethod(_context, "ExecuteStartActionsOnce");
            InvokeInternalMethod(_context, "ExecuteStartActionsOnce");
            InvokeInternalMethod(_context, "ExecuteStartActionsOnce");

            Assert.AreEqual(1, actionExecutedCount, "StartAction 具有单次幂等性保证，严格只执行 1 次！");
        }

        private static void InvokeInternalMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            method?.Invoke(target, args);
        }

        [Test]
        public void Speed_PresentationPlaySpeed_EpsilonThreshold()
        {
            var speedChanges = new List<float>();
            _context.OnPresentationSpeedChanged += (newSpeed) =>
            {
                speedChanges.Add(newSpeed);
            };

            // 1. 设置为 0.5f (发生变化，必须触发)
            _context.PresentationPlaySpeed = 0.5f;
            Assert.AreEqual(1, speedChanges.Count);
            Assert.AreEqual(0.5f, speedChanges[0], 0.0001f);

            // 2. 设置为 0.5000001f（浮点极其近似，Mathf.Approximately 判定为无变化）
            _context.PresentationPlaySpeed = 0.5000001f;
            Assert.AreEqual(1, speedChanges.Count, "浮点近似相等时绝不应重复触发速度变更事件");

            // 3. 设置为 1.0f (发生变化，触发)
            _context.PresentationPlaySpeed = 1.0f;
            Assert.AreEqual(2, speedChanges.Count);
            Assert.AreEqual(1.0f, speedChanges[1], 0.0001f);
        }

        [Test]
        public void LayerMask_StackManagement_PushesAndPopsCorrectly()
        {
            var animHandler = new MockAnimationHandler();
            _context.AddService<IAnimationHandler>(animHandler);

            // 构造两个测试 Mask 资产
            var originalMask = new AvatarMask { name = "OriginalMask" };
            var overrideMask1 = new AvatarMask { name = "OverrideMask1" };
            var overrideMask2 = new AvatarMask { name = "OverrideMask2" };

            int layer = 1;
            animHandler.SetLayerMask(layer, originalMask);

            // 1. Push Mask1
            _context.PushLayerMask(layer, overrideMask1);
            Assert.AreEqual(overrideMask1, animHandler.GetLayerMask(layer), "Push 后生效栈顶 Mask1");

            // 2. Push Mask2
            _context.PushLayerMask(layer, overrideMask2);
            Assert.AreEqual(overrideMask2, animHandler.GetLayerMask(layer), "Push 后生效新栈顶 Mask2");

            // 3. Pop Mask2
            _context.PopLayerMask(layer, overrideMask2);
            Assert.AreEqual(overrideMask1, animHandler.GetLayerMask(layer), "Pop 栈顶后回退至前一个 Mask1");

            // 4. Pop Mask1
            _context.PopLayerMask(layer, overrideMask1);
            Assert.AreEqual(originalMask, animHandler.GetLayerMask(layer), "全部 Pop 后必须恢复初始 OriginalMask！");
        }
    }
}
