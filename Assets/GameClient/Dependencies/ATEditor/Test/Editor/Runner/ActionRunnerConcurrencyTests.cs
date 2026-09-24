using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ATEditor.Test
{
    [TestFixture]
    public class ActionRunnerConcurrencyTests
    {
        private GameObject _ownerGo;
        private ProcessContext _context;
        private ActionRunner _runner;

        [SetUp]
        public void SetUp()
        {
            ProcessFactory.Reset();
            _ownerGo = new GameObject("TestOwner_Concurrency");
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
        public void Loop_OvershootCompensation_MaintainsTemporalContinuity()
        {
            // 时间轴时长 1.0s，开启 isLoop = true
            // 片段 A: [0.0, 0.4]（开头片段）
            // 片段 B: [0.7, 1.0]（末尾片段）
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: true);
            var clipA = timeline.AddMockClip(0.0f, 0.4f, "ClipA_Start");
            var clipB = timeline.AddMockClip(0.7f, 0.3f, "ClipB_End");

            bool loopFired = false;
            _runner.OnLoopComplete += () => { loopFired = true; };

            _runner.Play(timeline, _context, startTime: 0f);
            var pA = GetProcessForClip(clipA);
            var pB = GetProcessForClip(clipB);

            // 推进到 0.8s：此时 A 已退出 (Exit=1)，B 正在激活中 (Enter=1, Exit=0)
            _runner.Tick(0.8f);
            Assert.AreEqual(1, pA.OnExitCount);
            Assert.AreEqual(1, pB.OnEnterCount);
            Assert.AreEqual(0, pB.OnExitCount);

            // 此时再推进 0.3s -> 总时间达到 1.1s (越界 0.1s)
            // 预期：
            // 1. 触发 OnLoopComplete
            // 2. OvershootTime == 0.1s
            // 3. 旧轮次中激活的 B 触发 OnExit (Exit=1)
            // 4. 新一轮 CurrentTime = 0.1s
            // 5. 新一轮中跨入 [0.0, 0.4] 的片段 A 重新进入 OnEnter (Enter=2)！
            _runner.Tick(0.3f);

            Assert.IsTrue(loopFired, "循环完成必须触发 OnLoopComplete");
            Assert.AreEqual(0.1f, _runner.OvershootTime, 0.0001f, "残差时间补偿必须精确等于 0.1s");
            Assert.AreEqual(0.1f, _runner.CurrentTime, 0.0001f, "新一轮时间必须设置为 OvershootTime(0.1s)");
            Assert.AreEqual(1, pB.OnExitCount, "末尾活跃的片段 B 跨轮时必须触发 OnExit");
            Assert.AreEqual(2, pA.OnEnterCount, "开头片段 A 在新一轮必须重新触发 OnEnter 参与播放！");
        }

        [Test]
        public void Reentrancy_PlayNewActionInsideOnEnter_SafeBreak()
        {
            // 测试在 Process 的 OnEnter 回调中直接调用 runner.Play(newTimeline) 切换技能
            var timeline1 = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clip1 = timeline1.AddMockClip(0.2f, 0.6f, "Clip_WillReenter");

            var timeline2 = TestTimelineBuilder.Create(2.0f, isLoop: false);
            var clip2 = timeline2.AddMockClip(0.0f, 1.0f, "Clip_NewAction");

            _runner.Play(timeline1, _context, startTime: 0f);
            var p1 = GetProcessForClip(clip1);

            // 注入切招行为
            bool reenterExecuted = false;
            p1.CustomOnEnterAction = (proc) =>
            {
                reenterExecuted = true;
                // 在 OnEnter 中直接抢占切招
                _runner.Play(timeline2, _context, startTime: 0f);
            };

            // 推进到 0.3s，触发 clip1 的 OnEnter 并立即切招
            Assert.DoesNotThrow(() =>
            {
                _runner.Tick(0.3f);
            }, "在 OnEnter 回调中切招绝不应抛出异常（如集合修改或空引用）");

            Assert.IsTrue(reenterExecuted, "切招回调必须被执行");
            Assert.AreEqual(timeline2, _runner.Timeline, "Runner 的当前时间轴必须已被切换为新技能");
            Assert.AreEqual(ActionRunner.State.Playing, _runner.CurrentState);

            var p2 = GetProcessForClip(clip2);
            Assert.IsNotNull(p2, "新技能的 Process 必须成功运行");
            Assert.AreEqual(1, p2.OnEnterCount, "新技能开始时间为 0 的片段必须已被激活");
        }

        [Test]
        public void Reentrancy_StopInsideOnUpdate_SafeBreak()
        {
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            var clip = timeline.AddMockClip(0.0f, 1.0f, "Clip_StopInsideUpdate");

            _runner.Play(timeline, _context, startTime: 0f);
            var proc = GetProcessForClip(clip);

            bool stopCalled = false;
            proc.CustomOnUpdateAction = (p, time, dt) =>
            {
                if (time >= 0.2f && !stopCalled)
                {
                    stopCalled = true;
                    _runner.Stop();
                }
            };

            Assert.DoesNotThrow(() =>
            {
                _runner.Tick(0.1f); // t = 0.1s
                _runner.Tick(0.2f); // t = 0.3s -> 触发 Stop
            });

            Assert.IsTrue(stopCalled);
            Assert.AreEqual(1, proc.OnStopCount, "Stop 必须触发 OnStop");
            Assert.AreEqual(0, proc.OnDisableCount, "Stop 严禁触发 OnDisable");
            Assert.AreEqual(0, proc.OnExitCount, "Stop 严禁触发 OnExit");
        }

        [Test]
        public void Seek_TriggeredInsideTick_PendingUntilTickEnd()
        {
            // 在 OnTick 回调中调用 Seek，验证延迟 Seek 机制
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            timeline.AddMockClip(0.0f, 0.4f, "ClipA");
            timeline.AddMockClip(0.6f, 0.4f, "ClipB");

            _runner.Play(timeline, _context, startTime: 0f);

            bool seekRequested = false;
            _runner.OnTick += (currentTime) =>
            {
                if (currentTime >= 0.2f && !seekRequested)
                {
                    seekRequested = true;
                    // 在 Tick 遍历执行流中调用 Seek 到 0.8s
                    _runner.Seek(0.8f, 0.02f);
                }
            };

            // 驱动 Tick 从 0 步进 0.25s
            _runner.Tick(0.25f);

            Assert.IsTrue(seekRequested);
            // 验证 Seek 是否在 Tick 结束后被延迟执行并生效
            Assert.AreEqual(0.8f, _runner.CurrentTime, 0.0001f, "延迟 Seek 必须在 Tick 结束时精确生效到达 0.8s");
        }

        [Test]
        public void Event_DispatchOrder_StartTickComplete()
        {
            var timeline = TestTimelineBuilder.Create(1.0f, isLoop: false);
            timeline.AddMockClip(0.0f, 1.0f, "Clip_All");

            var eventOrder = new List<string>();
            _runner.OnStart += () => { eventOrder.Add("OnStart"); };
            _runner.OnTick += (t) => { eventOrder.Add($"OnTick:{t:F2}"); };
            _runner.OnComplete += () => { eventOrder.Add("OnComplete"); };

            _runner.Play(timeline, _context, startTime: 0f);
            _runner.Tick(0.5f);
            _runner.Tick(0.5f); // 到达 1.0s 自然结束

            Assert.IsTrue(eventOrder.Count >= 3);
            Assert.AreEqual("OnStart", eventOrder[0], "首个分发的事件必须是 OnStart");
            Assert.AreEqual("OnComplete", eventOrder[eventOrder.Count - 1], "最后一个分发的事件必须是 OnComplete");
        }
    }
}
