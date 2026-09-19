using Game.Framework;
using NUnit.Framework;

namespace Game.Tests.TimeSystem
{
    public class HierarchicalClockTreeTests
    {
        [Test]
        public void ClockTree_CascadingScale_CalculatesCorrectly()
        {
            // Root (0.5) -> Gameplay (0.8) -> Monster (0.1) -> Entity (1.0)
            var root = new TimeClock("Root") { LocalScale = 0.5f };
            var gameplay = new TimeClock("Gameplay", root) { LocalScale = 0.8f };
            var monster = new TimeClock("Monster", gameplay) { LocalScale = 0.1f };
            var entity = new TimeClock("Entity", monster);

            // 0.5 * 0.8 * 0.1 * 1.0 = 0.04
            Assert.AreEqual(0.04f, entity.EffectiveScale, 0.0001f);
        }

        [Test]
        public void ClockTree_HitStopInBulletTime_HarmonizesMathematically()
        {
            var root = new TimeClock("Root");
            var gameplay = new TimeClock("Gameplay", root);
            var monster = new TimeClock("Monster", gameplay);
            var entity = new TimeClock("Entity", monster);

            int eventCount = 0;
            float lastScale = 1.0f;
            entity.OnEffectiveScaleChanged += scale =>
            {
                eventCount++;
                lastScale = scale;
            };

            // 1. 触发子弹时间 (0.1x)
            monster.LocalScale = 0.1f;
            Assert.AreEqual(0.1f, entity.EffectiveScale, 0.0001f);
            Assert.AreEqual(0.1f, lastScale, 0.0001f);

            // 2. 怪物受击顿帧 (LocalScale = 0)
            entity.LocalScale = 0f;
            Assert.AreEqual(0f, entity.EffectiveScale, 0.0001f);
            Assert.AreEqual(0f, lastScale, 0.0001f);

            // 3. 顿帧结束 (LocalScale = 1.0)
            entity.LocalScale = 1.0f;
            // 自动回到 0.1x，零特殊分支！
            Assert.AreEqual(0.1f, entity.EffectiveScale, 0.0001f);
            Assert.AreEqual(0.1f, lastScale, 0.0001f);

            // 4. 子弹时间结束 (LocalScale = 1.0)
            monster.LocalScale = 1.0f;
            Assert.AreEqual(1.0f, entity.EffectiveScale, 0.0001f);
            Assert.AreEqual(1.0f, lastScale, 0.0001f);
        }

        [Test]
        public void ClockTree_PauseGameplay_CascadesToZeroWithoutAffectingUI()
        {
            var root = new TimeClock("Root");
            var gameplay = new TimeClock("Gameplay", root);
            var ui = new TimeClock("UI", root);
            var monster = new TimeClock("Monster", gameplay) { LocalScale = 0.1f };
            var entity = new TimeClock("Entity", monster);

            // 暂停战斗
            gameplay.LocalScale = 0f;

            // UI 保持全速
            Assert.AreEqual(1.0f, ui.EffectiveScale, 0.0001f);

            // 怪物及其下属实体全部瞬间归零
            Assert.AreEqual(0f, monster.EffectiveScale, 0.0001f);
            Assert.AreEqual(0f, entity.EffectiveScale, 0.0001f);

            // 恢复战斗
            gameplay.LocalScale = 1.0f;

            // 怪物时钟无缝级联回到 0.1x
            Assert.AreEqual(0.1f, entity.EffectiveScale, 0.0001f);
        }

        [Test]
        public void ClockTree_Detach_RemovesParentLinkCleanly()
        {
            var root = new TimeClock("Root");
            var leaf = new TimeClock("Leaf", root);

            Assert.AreEqual(root, leaf.Parent);
            Assert.Contains(leaf, (System.Collections.ICollection)root.Children);

            leaf.Detach();

            Assert.IsNull(leaf.Parent);
            Assert.IsEmpty((System.Collections.ICollection)root.Children);
        }

        [Test]
        public void ClockTree_WakeUpOnHit_RestoresMonsterToNormalSpeed()
        {
            var root = new TimeClock("Root");
            var gameplay = new TimeClock("Gameplay", root);
            var monster = new TimeClock("Monster", gameplay);
            var victimEntity = new TimeClock("VictimMonster", monster);

            // 1. 触发子弹时间 (0.1x)
            monster.LocalScale = 0.1f;
            Assert.AreEqual(0.1f, victimEntity.EffectiveScale, 0.0001f);

            // 2. 玩家命中怪物并造成动作打断，触发打醒 (Wake-up on hit)，恢复 MonsterClock 为 1.0f
            monster.LocalScale = 1.0f;
            Assert.AreEqual(1.0f, victimEntity.EffectiveScale, 0.0001f);

            // 3. 受击顿帧介入 (0.05s 定格，LocalScale = 0)
            victimEntity.LocalScale = 0f;
            Assert.AreEqual(0f, victimEntity.EffectiveScale, 0.0001f);

            // 4. 顿帧自然结束 (LocalScale = 1.0f)
            victimEntity.LocalScale = 1.0f;

            // 5. 验证受击动画以 1.0x 正常速度流畅播放，而非处于 0.1x 慢动作
            Assert.AreEqual(1.0f, victimEntity.EffectiveScale, 0.0001f);
        }

        [Test]
        public void ClockTree_EventDriven_WakeUpAndHitStopFlow_Passes()
        {
            var tm = Game.GamePlay.TimeManager.Instance;
            tm.ResetToNormal();

            var victimClock = new TimeClock("VictimClock");
            tm.RegisterMonsterClock(victimClock);

            // 1. 广播极限闪避事实事件 -> TimeManager 自发响应进入子弹时间
            EventCenter.Publish(new Game.GamePlay.PerfectEvadeTriggeredEvent(
                null, null, 0.1f, 5.0f, false));

            Assert.AreEqual(0.1f, tm.MonsterClock.LocalScale, 0.0001f);
            Assert.AreEqual(0.1f, victimClock.EffectiveScale, 0.0001f);

            // 2. 广播受击打断事实事件 -> TimeManager 自发响应打醒并解除子弹时间
            // 构造包含 victimClock 的打断事件
            var go = new UnityEngine.GameObject("TestVictim");
            try
            {
                var victimEntity = go.AddComponent<Game.GamePlay.MonsterEntity>();
                victimEntity.AttachToClock(tm.MonsterClock);

                EventCenter.Publish(new Game.GamePlay.EntityHitInterruptedEvent(
                    null, victimEntity, cfg.ZZZ.HitReactionType.Light));

                // 验证已被打醒
                Assert.AreEqual(1.0f, tm.MonsterClock.LocalScale, 0.0001f);
                Assert.AreEqual(1.0f, victimClock.EffectiveScale, 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // 3. 广播顿帧请求事件 -> TimeManager 自发执行卡肉顿帧
            EventCenter.Publish(new Game.GamePlay.HitStopRequestEvent(
                null, victimClock, 0.05f, 0f));

            Assert.AreEqual(0f, victimClock.LocalScale, 0.0001f);
            Assert.AreEqual(0f, victimClock.EffectiveScale, 0.0001f);

            // 4. 重置并清理
            tm.ResetToNormal();
            Assert.AreEqual(1.0f, victimClock.LocalScale, 0.0001f);
            victimClock.Detach();
        }
    }
}
