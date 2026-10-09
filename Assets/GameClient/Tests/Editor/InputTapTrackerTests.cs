using Game.GamePlay;
using NUnit.Framework;

namespace Game.Tests.InputSystem
{
    public class InputTapTrackerTests
    {
        [Test]
        public void Test01_RingBuffer_BasicRecordAndCount()
        {
            var buffer = new TapTimestampRingBuffer();
            Assert.AreEqual(0, buffer.CountSince(0f));

            buffer.Record(1.0f);
            buffer.Record(1.2f);
            buffer.Record(1.5f);

            Assert.AreEqual(3, buffer.CountSince(0.5f));
            Assert.AreEqual(2, buffer.CountSince(1.1f));
            Assert.AreEqual(1, buffer.CountSince(1.3f));
            Assert.AreEqual(0, buffer.CountSince(1.6f));
        }

        [Test]
        public void Test02_RingBuffer_OverflowBeyondCapacity()
        {
            var buffer = new TapTimestampRingBuffer();

            // 写入 10 次 (容量为 8)
            for (int i = 1; i <= 10; i++)
            {
                buffer.Record(i * 1.0f); // 1.0, 2.0, ..., 10.0
            }

            // 最早的 1.0 和 2.0 已被环形覆盖，保留 3.0 ~ 10.0 (共 8 个)
            Assert.AreEqual(8, buffer.CountSince(0f));
            Assert.AreEqual(8, buffer.CountSince(3.0f));
            Assert.AreEqual(7, buffer.CountSince(3.5f));
            Assert.AreEqual(1, buffer.CountSince(10.0f));
        }

        [Test]
        public void Test03_RingBuffer_Clear()
        {
            var buffer = new TapTimestampRingBuffer();
            buffer.Record(1.0f);
            buffer.Record(2.0f);
            buffer.Clear();

            Assert.AreEqual(0, buffer.CountSince(0f));
        }

        [Test]
        public void Test04_InputTapTracker_MultipleKeysIsolation()
        {
            var tracker = new InputTapTracker();
            float now = 10.0f;

            tracker.RecordTap(HardwareInputType.BasicAttack, 9.7f);
            tracker.RecordTap(HardwareInputType.BasicAttack, 9.9f);
            tracker.RecordTap(HardwareInputType.Evade, 9.8f);

            // 普攻在最近 0.3s (9.7 ~ 10.0) 之间有 2 次
            Assert.AreEqual(2, tracker.GetTapCountInWindow(HardwareInputType.BasicAttack, 0.35f, now));
            // 闪避在最近 0.3s (9.7 ~ 10.0) 之间有 1 次
            Assert.AreEqual(1, tracker.GetTapCountInWindow(HardwareInputType.Evade, 0.35f, now));
            // 特殊攻击未按过，为 0 次
            Assert.AreEqual(0, tracker.GetTapCountInWindow(HardwareInputType.SpecialAttack, 0.35f, now));

            // 基于起始时间戳
            Assert.AreEqual(2, tracker.GetTapCountSince(HardwareInputType.BasicAttack, 9.5f));
            Assert.AreEqual(1, tracker.GetTapCountSince(HardwareInputType.BasicAttack, 9.8f));

            // 清理单键
            tracker.Clear(HardwareInputType.BasicAttack);
            Assert.AreEqual(0, tracker.GetTapCountInWindow(HardwareInputType.BasicAttack, 1.0f, now));
            Assert.AreEqual(1, tracker.GetTapCountInWindow(HardwareInputType.Evade, 1.0f, now));
        }
    }
}
