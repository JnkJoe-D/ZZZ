using Game.Framework;
using Game.GamePlay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Targeting
{
    [TestFixture]
    public class TargetLockerTests
    {
        private GameObject _playerGo;
        private RoleEntity _playerEntity;

        private GameObject _monsterGoA;
        private MonsterEntity _monsterA;

        private GameObject _monsterGoB;
        private MonsterEntity _monsterB;

        private TargetLockerConfig _config;
        private TargetLocker _locker;
        private RoleTargetFinder _finder;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("Player_Test");
            _playerEntity = _playerGo.AddComponent<RoleEntity>();
            _playerGo.transform.position = Vector3.zero;
            _playerGo.transform.rotation = Quaternion.identity;

            _monsterGoA = new GameObject("Monster_A");
            _monsterA = _monsterGoA.AddComponent<MonsterEntity>();
            _monsterGoA.transform.position = new Vector3(0, 0, 10f); // 距离 10 米

            _monsterGoB = new GameObject("Monster_B");
            _monsterB = _monsterGoB.AddComponent<MonsterEntity>();
            _monsterGoB.transform.position = new Vector3(0, 0, 6f); // 距离 6 米（更近）

            MonsterManager.Instance.RegisterActiveMonsterForTest(_monsterA);
            MonsterManager.Instance.RegisterActiveMonsterForTest(_monsterB);

            _config = new TargetLockerConfig
            {
                LockRadius = 14f,
                LoseRadius = 18f,
                ScanInterval = 0.08f,
                ForwardAngleWeight = 0f, // 纯距离测试，避免角度扰动
                ViewFovHalfAngle = 180f
            };

            _locker = new TargetLocker(_config);
            _locker.Initialize(_playerEntity);

            _finder = new RoleTargetFinder(new RoleTargetFinder.RoleTargetFinderCfg { SearchRadius = 15f }, _locker);
            _finder.Initialize(_playerEntity);
        }

        [TearDown]
        public void TearDown()
        {
            _finder?.Dispose();
            _locker?.Dispose();

            if (_monsterA != null) MonsterManager.Instance.UnregisterActiveMonsterForTest(_monsterA);
            if (_monsterB != null) MonsterManager.Instance.UnregisterActiveMonsterForTest(_monsterB);

            if (_playerGo != null) Object.DestroyImmediate(_playerGo);
            if (_monsterGoA != null) Object.DestroyImmediate(_monsterGoA);
            if (_monsterGoB != null) Object.DestroyImmediate(_monsterGoB);
        }

        [Test]
        public void TargetLocker_InitialScan_LocksClosestTarget()
        {
            // 推进一次 Tick 触发扫描
            _locker.LogicTick(0.1f);

            Assert.IsTrue(_locker.HasTarget);
            Assert.AreEqual(_monsterB, _locker.CurrentLockedMonster, "初始应优先锁定距离玩家最近的怪物 B (6m)");
            Assert.AreEqual(_monsterB.transform, _finder.GetTarget());
        }

        [Test]
        public void TargetLocker_Hysteresis_RemainsLockedWithinLoseRadius()
        {
            // 1. 初始锁定 B
            _locker.LogicTick(0.1f);
            Assert.AreEqual(_monsterB, _locker.CurrentLockedMonster);

            // 2. 将 B 移动至 16m (超出 LockRadius=14m，但在 LoseRadius=18m 以内)
            // 此时 A 仍在 10m（比 B 更近）
            _monsterGoB.transform.position = new Vector3(0, 0, 16f);

            _locker.LogicTick(0.1f);

            // 迟滞检验：虽然 A 比 B 更近，且 B 超过了进入半径 14m，但未超出 18m 脱锁半径，必须维持锁定 B！
            Assert.AreEqual(_monsterB, _locker.CurrentLockedMonster, "迟滞机制应维持对已锁定目标 B 的锁定，防止高频抖动切锁");
        }

        [Test]
        public void TargetLocker_LosesTarget_WhenExceedingLoseRadius()
        {
            // 1. 初始锁定 B
            _locker.LogicTick(0.1f);
            Assert.AreEqual(_monsterB, _locker.CurrentLockedMonster);

            // 2. 将 B 移出脱锁范围至 20m (> LoseRadius 18m)
            _monsterGoB.transform.position = new Vector3(0, 0, 20f);

            _locker.LogicTick(0.1f);

            // 脱锁检验：B 超出脱离范围，丢失 B 并重新锁定当前唯一在范围内的 A
            Assert.AreEqual(_monsterA, _locker.CurrentLockedMonster, "超出 LoseRadius 后应脱锁并自动锁定新的最近目标 A");
        }

        [Test]
        public void TargetLocker_OnEntityDied_InstantlySwitchesToNextTarget()
        {
            // 1. 初始锁定 B
            _locker.LogicTick(0.1f);
            Assert.AreEqual(_monsterB, _locker.CurrentLockedMonster);

            // 2. 模拟怪物 B 死亡并失活
            _monsterGoB.SetActive(false);
            EventCenter.Publish(new EntityDiedEvent { Victim = _monsterB, Attacker = _playerEntity });

            // 死亡响应检验：零延迟立即转锁至怪物 A
            Assert.AreEqual(_monsterA, _locker.CurrentLockedMonster, "当前锁定目标死亡时，应瞬时转锁至下一个有效目标 A");
        }

        [Test]
        public void RoleTargetFinder_CombatContextTarget_HasPrecedenceOverLockerTarget()
        {
            _locker.LogicTick(0.1f);
            Assert.AreEqual(_monsterB.transform, _finder.GetTarget());

            // 设定动作交互上下文临时目标为 A (如反击/招架)
            _finder.SetCombatContextTarget(_monsterA);

            Assert.AreEqual(_monsterA.transform, _finder.GetEffectiveTarget(), "GetEffectiveTarget 必须优先返回 CombatContextTarget");
            Assert.AreEqual(_monsterB.transform, _finder.GetTarget(), "GetTarget 仍应返回常规锁定目标");

            // 清空上下文目标
            _finder.ClearCombatContextTarget();
            Assert.AreEqual(_monsterB.transform, _finder.GetEffectiveTarget(), "清空上下文后应自动回退至常规锁定目标");
        }

        [Test]
        public void RoleTargetFinder_CombatContextTarget_AutoClears_WhenExceedingLoseRadius()
        {
            // 设定上下文目标为 A (10m 在范围以内)
            _finder.SetCombatContextTarget(_monsterA);
            Assert.AreEqual(_monsterA.transform, _finder.GetEffectiveTarget());

            // 将怪物 A 移动至 30 米远处 (超出 LoseRadius=18m)
            _monsterGoA.transform.position = new Vector3(0, 0, 30f);

            // 当读取 GetEffectiveTarget 时，应自动自愈检测到超出距离并清理上下文目标，回退至 Locker 锁定的 B
            _locker.LogicTick(0.1f);
            Assert.AreEqual(_monsterB.transform, _finder.GetEffectiveTarget(), "超出脱锁范围的 CombatContextTarget 应被自动清理并回退");
            Assert.IsNull(_finder.CombatContextTarget, "CombatContextTarget 属性应被置空");
        }

        [Test]
        public void TargetLocker_ChasingMonsterBehind_CanBeLocked_WhenNoTargetInFront()
        {
            // 清理场景前方的怪物
            _monsterGoA.SetActive(false);
            _monsterGoB.SetActive(false);

            // 创建一只在玩家正背后 5 米处追击的怪物
            var chasingGo = new GameObject("Chasing_Monster");
            var chasingMonster = chasingGo.AddComponent<MonsterEntity>();
            chasingGo.transform.position = new Vector3(0, 0, -5f); // 玩家背后 180 度偏角
            MonsterManager.Instance.RegisterActiveMonsterForTest(chasingMonster);

            try
            {
                _locker.Unlock();
                _locker.LogicTick(0.1f);

                // 检验：即使追击怪物处于玩家背后 180 度偏角，在正面无怪时仍能作为兜底目标被锁定
                Assert.AreEqual(chasingMonster, _locker.CurrentLockedMonster, "玩家背后近距离追击的怪物在正面无怪时应能被锁定");
            }
            finally
            {
                MonsterManager.Instance.UnregisterActiveMonsterForTest(chasingMonster);
                Object.DestroyImmediate(chasingGo);
            }
        }
    }
}
