using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ATEditor.Test
{
    [TestFixture]
    public class ProcessLifecycleTests
    {
        private GameObject _ownerGo;
        private ProcessContext _context;
        private ActionRunner _runner;

        [SetUp]
        public void SetUp()
        {
            ProcessFactory.Reset();
            _ownerGo = new GameObject("TestOwner_Lifecycle");
            _context = new ProcessContext(_ownerGo, PlayMode.Runtime);
            _runner = new ActionRunner(PlayMode.Runtime);
        }

        [TearDown]
        public void TearDown()
        {
            if (_runner != null && _runner.CurrentState != ActionRunner.State.None)
            {
                _runner.Stop();
            }
            if (_ownerGo != null)
            {
                Object.DestroyImmediate(_ownerGo);
            }
            ProcessFactory.Reset();
        }

        private MockProcess GetProcessForClip(ClipBase clip)
        {
            foreach (var inst in _runner.ActiveProcesses)
            {
                if (ReferenceEquals(inst.clip, clip))
                {
                    return inst.process as MockProcess;
                }
            }
            return null;
        }

        [Test]
        public void Lifecycle_NaturalPlayback_ExitOnly_NeverStop()
        {
            // 构造片段 [0.2s, 0.8s]，时间轴时长 1.0s
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clip = timeline.AddMockClip(0.2f, 0.6f, "Clip_Natural");

            _runner.Play(timeline, _context, startTime: 0f);

            var process = GetProcessForClip(clip);
            Assert.IsNotNull(process, "Process 必须成功通过 ProcessFactory 实例化绑定");

            // 1. 验证启动期
            Assert.AreEqual(1, process.InitializeCount, "Initialize 必须在启动期首个被调用");
            Assert.AreEqual(1, process.OnEnableCount, "OnEnable 必须在启动期被调用");
            Assert.AreEqual(0, process.OnEnterCount, "尚未到达 0.2s，不应调用 OnEnter");

            // 2. 推进到 0.1s（尚未进入）
            _runner.Tick(0.1f);
            Assert.AreEqual(0, process.OnEnterCount, "t=0.1s 仍未进入区间");

            // 3. 推进到 0.3s（跨入区间，进入激活）
            _runner.Tick(0.2f); // CurrentTime = 0.3s
            Assert.AreEqual(1, process.OnEnterCount, "t=0.3s 已进入区间，必须触发 OnEnter");
            Assert.GreaterOrEqual(process.OnUpdateCount, 1, "区间内必须触发 OnUpdate");

            // 4. 推进到 0.7s（区间内保持激活）
            _runner.Tick(0.4f); // CurrentTime = 0.7s
            Assert.AreEqual(1, process.OnEnterCount);
            Assert.AreEqual(0, process.OnExitCount, "仍在区间内，不应触发 OnExit");
            Assert.AreEqual(0, process.OnStopCount, "正常播放严禁触发 OnStop");
            Assert.AreEqual(0, process.OnDisableCount, "正常播放严禁触发 OnDisable");

            // 5. 推进到 0.9s（离开区间 [0.2, 0.8]）
            _runner.Tick(0.2f); // CurrentTime = 0.9s
            Assert.AreEqual(1, process.OnExitCount, "t=0.9s 已脱离区间，必须触发 OnExit");
            Assert.AreEqual(0, process.OnStopCount, "离开区间正常退出，严禁触发 OnStop");
            Assert.AreEqual(0, process.OnDisableCount, "离开区间正常退出，严禁触发 OnDisable");

            // 6. 推进到 1.0s（时间轴播放完成）
            _runner.Tick(0.1f); // CurrentTime = 1.0s, 触发 OnComplete
            Assert.AreEqual(ActionRunner.State.None, _runner.CurrentState, "播放完毕状态应归于 None");

            // 7. 严格核验业务契约：自然播完触发 Exit，绝不触发 OnStop 与 OnDisable
            Assert.AreEqual(1, process.OnExitCount, "自然播完必须已调用 OnExit");
            Assert.AreEqual(0, process.OnStopCount, "【业务契约核心断言】自然播完链路严格禁止触发 OnStop！");
            Assert.AreEqual(0, process.OnDisableCount, "【业务契约核心断言】自然播完链路严格禁止触发 OnDisable！");
            Assert.GreaterOrEqual(process.ResetCount, 1, "归还对象池时必须调用 Reset 清洗");
        }

        [Test]
        public void Lifecycle_Interrupt_StopOnly_NeverExit()
        {
            // 构造片段 [0.0s, 1.0s]，播放中途进行打断
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clip = timeline.AddMockClip(0.0f, 1.0f, "Clip_ToInterrupt");

            _runner.Play(timeline, _context, startTime: 0f);
            var process = GetProcessForClip(clip);
            Assert.IsNotNull(process);

            _runner.Tick(0.5f); // 播放到 0.5s，处于激活中
            Assert.AreEqual(1, process.OnEnterCount);
            Assert.AreEqual(0, process.OnExitCount);
            Assert.AreEqual(0, process.OnStopCount);
            Assert.AreEqual(0, process.OnDisableCount);

            // 注入新 Timeline 抢占打断
            var newTimeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            bool interruptEventFired = false;
            _runner.OnInterrupt += () => { interruptEventFired = true; };

            _runner.Play(newTimeline, _context, startTime: 0f);

            // 验证旧片段被打断的生命周期
            Assert.IsTrue(interruptEventFired, "被打断时必须触发 OnInterrupt 事件");
            Assert.AreEqual(0, process.OnExitCount, "【业务契约核心断言】被打断时绝对不应触发 OnExit！");
            Assert.AreEqual(1, process.OnStopCount, "被打断时必须且仅触发 1 次 OnStop 进行硬清理");
            Assert.AreEqual(0, process.OnDisableCount, "被打断时严禁触发物理销毁 OnDisable！");
            Assert.IsTrue(process.History.Exists(r => r.MethodName == nameof(process.OnStop) && r.ContextIsInterrupted), 
                "在 OnStop 执行时，context.IsInterrupted 必须为 true");
        }

        [Test]
        public void Lifecycle_Stop_StopOnly_NeverExit()
        {
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clip = timeline.AddMockClip(0.0f, 1.0f, "Clip_ToStop");

            _runner.Play(timeline, _context, startTime: 0f);
            var process = GetProcessForClip(clip);
            Assert.IsNotNull(process);

            _runner.Tick(0.5f);
            Assert.AreEqual(1, process.OnEnterCount);

            // 外部主动强制调用 Stop
            _runner.Stop();

            Assert.AreEqual(ActionRunner.State.None, _runner.CurrentState);
            Assert.AreEqual(0, process.OnExitCount, "【业务契约核心断言】外部 Stop 时绝对不应触发 OnExit！");
            Assert.AreEqual(1, process.OnStopCount, "外部 Stop 时必须触发 OnStop");
            Assert.AreEqual(0, process.OnDisableCount, "外部 Stop 严禁触发物理销毁 OnDisable！");
            Assert.GreaterOrEqual(process.ResetCount, 1, "Stop 后归还对象池必须调用 Reset");
        }

        [Test]
        public void Lifecycle_InactiveClip_InterruptedBeforeEnter()
        {
            // 片段 A [0.0, 0.4]，片段 B [0.6, 1.0]
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clipA = timeline.AddMockClip(0.0f, 0.4f, "ClipA");
            var clipB = timeline.AddMockClip(0.6f, 0.4f, "ClipB");

            _runner.Play(timeline, _context, startTime: 0f);
            var processA = GetProcessForClip(clipA);
            var processB = GetProcessForClip(clipB);

            _runner.Tick(0.2f); // 此时 A 激活，B 尚未激活

            Assert.AreEqual(1, processA.OnEnterCount);
            Assert.AreEqual(0, processB.OnEnterCount, "B 尚未到达开始时间，不能 Enter");

            // 在 B 尚未激活时打断
            _runner.Stop();

            // 验证未激活片段 B 的行为
            Assert.AreEqual(1, processB.OnEnableCount, "Play 时全部 Process 都调用了 OnEnable");
            Assert.AreEqual(0, processB.OnEnterCount, "B 全程未 Enter");
            Assert.AreEqual(0, processB.OnUpdateCount, "B 全程未 Update");
            Assert.AreEqual(0, processB.OnExitCount, "B 全程未 Exit");
            Assert.AreEqual(1, processB.OnStopCount, "未激活的片段在打断时也应收到 OnStop 释放预加载资源");
            Assert.AreEqual(0, processB.OnDisableCount, "打断未激活片段严禁触发 OnDisable");
        }

        [Test]
        public void Lifecycle_PauseResume_OnlyActiveProcesses()
        {
            // 片段 A [0.0, 0.4]，片段 B [0.6, 1.0]
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clipA = timeline.AddMockClip(0.0f, 0.4f, "ClipA");
            var clipB = timeline.AddMockClip(0.6f, 0.4f, "ClipB");

            _runner.Play(timeline, _context, startTime: 0f);
            var processA = GetProcessForClip(clipA);
            var processB = GetProcessForClip(clipB);

            _runner.Tick(0.2f); // A 激活，B 未激活

            // 1. 暂停
            _runner.Pause();
            Assert.AreEqual(ActionRunner.State.Paused, _runner.CurrentState);
            Assert.AreEqual(1, processA.OnPauseCount, "激活中的 A 必须收到 OnPause");
            Assert.AreEqual(0, processB.OnPauseCount, "未激活的 B 严禁收到 OnPause");

            // 2. 暂停状态下驱动 Tick，时间不应推进，不应触发 Update
            int aUpdateCountBefore = processA.OnUpdateCount;
            _runner.Tick(0.1f);
            Assert.AreEqual(0.2f, _runner.CurrentTime, 0.0001f, "Paused 状态下 Tick 不推进时间");
            Assert.AreEqual(aUpdateCountBefore, processA.OnUpdateCount, "Paused 状态下不触发 Update");

            // 3. 恢复
            _runner.Resume();
            Assert.AreEqual(ActionRunner.State.Playing, _runner.CurrentState);
            Assert.AreEqual(1, processA.OnResumeCount, "恢复时 A 必须收到 OnResume");
            Assert.AreEqual(0, processB.OnResumeCount, "未激活的 B 严禁收到 OnResume");
        }

        [Test]
        public void Lifecycle_Seek_FourWayStateTransitions()
        {
            // A: [0.0, 0.4]
            // B: [0.3, 0.7] (跨越 0.2 到 0.5)
            // C: [0.5, 0.9]
            // D: [0.8, 1.0]
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clipA = timeline.AddMockClip(0.0f, 0.4f, "ClipA");
            var clipB = timeline.AddMockClip(0.3f, 0.4f, "ClipB");
            var clipC = timeline.AddMockClip(0.5f, 0.4f, "ClipC");
            var clipD = timeline.AddMockClip(0.8f, 0.2f, "ClipD");

            _runner.Play(timeline, _context, startTime: 0f);
            var pA = GetProcessForClip(clipA);
            var pB = GetProcessForClip(clipB);
            var pC = GetProcessForClip(clipC);
            var pD = GetProcessForClip(clipD);

            // 推进到 0.35s：此时 A[0,0.4] 激活，B[0.3,0.7] 激活，C[0.5,0.9] 未激活，D[0.8,1.0] 未激活
            _runner.Tick(0.35f);
            Assert.AreEqual(1, pA.OnEnterCount);
            Assert.AreEqual(1, pB.OnEnterCount);
            Assert.AreEqual(0, pC.OnEnterCount);
            Assert.AreEqual(0, pD.OnEnterCount);

            // 从 0.35s 直接 Seek 到 0.6s：
            // A[0, 0.4]：Active -> Inactive (必须触发 OnExit)
            // B[0.3, 0.7]：Active -> Active (必须仅触发 OnSeek(0.6f)，不调 Exit/Enter)
            // C[0.5, 0.9]：Inactive -> Active (必须触发 OnEnter)
            // D[0.8, 1.0]：Inactive -> Inactive (无任何操作)
            _runner.Seek(0.6f, 0.02f);

            // 断言 A: 脱离
            Assert.AreEqual(1, pA.OnExitCount, "A 脱离区间必须触发 OnExit");

            // 断言 B: 保持激活
            Assert.AreEqual(1, pB.OnSeekCount, "B 保持激活必须触发 OnSeek");
            Assert.AreEqual(0.6f, pB.LastSeekTime, 0.0001f);
            Assert.AreEqual(1, pB.OnEnterCount, "B 不应再次调用 OnEnter");
            Assert.AreEqual(0, pB.OnExitCount, "B 不应调用 OnExit");

            // 断言 C: 新进入
            Assert.AreEqual(1, pC.OnEnterCount, "C 新进入区间必须触发 OnEnter");
            Assert.AreEqual(0, pC.OnExitCount);

            // 断言 D: 全程无关
            Assert.AreEqual(0, pD.OnEnterCount);
            Assert.AreEqual(0, pD.OnExitCount);
            Assert.AreEqual(0, pD.OnSeekCount);

            // 验证 Seek 刷新了当帧画面（B 和 C 都执行了 OnUpdate(0.6f, 0.02f)）
            Assert.AreEqual(0.6f, pB.LastUpdateTime, 0.0001f);
            Assert.AreEqual(0.6f, pC.LastUpdateTime, 0.0001f);
        }

        [Test]
        public void Lifecycle_PhysicalDisable_ExecutesCleanups()
        {
            // 验证全新定义的物理注销生命周期契约 OnDisable()
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clip = timeline.AddMockClip(0.0f, 1.0f, "Clip_PhysicalDisable");

            _runner.Play(timeline, _context, startTime: 0f);
            var process = GetProcessForClip(clip);
            Assert.IsNotNull(process);

            bool disableCustomFired = false;
            process.CustomOnDisableAction = (p) => { disableCustomFired = true; };

            // 模拟宿主组件卸载/销毁时广播的 OnDisable
            process.OnDisable();

            Assert.AreEqual(1, process.OnDisableCount, "OnDisable 必须被准确触发 1 次");
            Assert.IsTrue(disableCustomFired, "OnDisable 内挂载的物理资源解绑回调必须被执行");
        }
    }
}

