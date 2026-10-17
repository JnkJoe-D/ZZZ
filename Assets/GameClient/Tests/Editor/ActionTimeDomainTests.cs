using Game.Framework;
using Game.GamePlay;
using NUnit.Framework;

namespace GameClient.Tests.Editor
{
    public sealed class ActionTimeDomainTests
    {
        private TimeManager _manager;
        [TearDown] public void Cleanup() { _manager?.Shutdown(); _manager = null; }
        [TestCase(1f)] [TestCase(0.5f)] [TestCase(0.1f)] [TestCase(0f)]
        public void OneRealSecondAppliesGameplayAndRoleAndLeafScalesOnce(float gameplayScale)
        {
            _manager = new TimeManager(); _manager.GameplayTimeScale = gameplayScale; _manager.RoleTimeScale = 0.5f;
            var leaf = new TimeClock("entity", _manager.RoleClock) { LocalScale = 0.5f };
            float advanced = 0f;
            _manager.OnGameplayLogicTick += dt => advanced += leaf.AdvanceFrom(dt, _manager.GameplayClock);
            for (int i = 0; i < 120; i++) _manager.TickFrame(1f / 120f);
            Assert.That(advanced, Is.EqualTo(gameplayScale * 0.25f).Within(0.005f));
            Assert.AreEqual(advanced, leaf.Time);
        }
        [Test]
        public void RealTimeOriginIncludesAllAncestorsAndRejectsUnrelatedOrigin()
        {
            var parent = new TimeClock("parent") { LocalScale = 0.1f };
            var leaf = new TimeClock("leaf", parent) { LocalScale = 0.5f };
            Assert.That(leaf.AdvanceFrom(1f, null), Is.EqualTo(0.05f).Within(0.0001f));
            Assert.Throws<System.ArgumentException>(() => leaf.AdvanceFrom(1f, new TimeClock("other")));
            Assert.That(leaf.Time, Is.EqualTo(0.05f).Within(0.0001f));
        }
        [Test]
        public void OverlappingHitStopsKeepStrongestScaleAndRestoreOriginalUnderSlowParent()
        {
            _manager = new TimeManager(); _manager.RoleTimeScale = 0.1f;
            var leaf = new TimeClock("entity", _manager.RoleClock) { LocalScale = 0.5f };
            _manager.RegisterHitStop(leaf, 0.1f, 0f);
            _manager.RegisterHitStop(leaf, 0.3f, 0.2f);
            Assert.AreEqual(0f, leaf.LocalScale);
            _manager.ManualTick(0.11f); Assert.AreEqual(0.2f, leaf.LocalScale);
            _manager.ManualTick(0.21f); Assert.AreEqual(0.5f, leaf.LocalScale);
            Assert.That(leaf.EffectiveScale, Is.EqualTo(0.05f).Within(0.0001f));
        }
        [Test]
        public void HitStopExpiresDuringGameplayPauseAndClearRestoresBase()
        {
            _manager = new TimeManager(); _manager.GameplayTimeScale = 0f;
            var leaf = new TimeClock("entity", _manager.RoleClock) { LocalScale = 0.7f };
            _manager.RegisterHitStop(leaf, 0.1f); _manager.TickFrame(0.2f);
            Assert.AreEqual(0.7f, leaf.LocalScale); Assert.AreEqual(0f, leaf.EffectiveScale);
            _manager.RegisterHitStop(leaf, 1f); _manager.ClearAllHitStops();
            Assert.AreEqual(0.7f, leaf.LocalScale);
        }
    }
}
