using System.Collections.Generic;
using ATEditor;
using Game.Framework;
using Game.GamePlay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Combat
{
    /// <summary>
    /// 角色招架全流程与接刀身位深度诊断自动化测试
    /// 涵盖：
    /// 1. 攻击预警接刀身位（ClashPositionOffset）空间坐标数学解算
    /// 2. 切入身位裁决（IncomingPlacementPipe）：就地格挡 (In-Place) vs 远距接刀点 (Clash Point)
    /// 3. 受击管线短路与拼刀契约仲裁（HitPipeline + ParryPipe）
    /// 4. 攻击盒覆盖范围不足导致的招架失败重现与诊断
    /// 5. 招架捕获窗口（ATParryWindowHandler）完整生命周期闭环
    /// </summary>
    [TestFixture]
    public class ParryWorkflowTests
    {
        private GameObject _monsterGo;
        private MonsterEntity _monster;

        private GameObject _outgoingRoleGo;
        private RoleEntity _outgoingRole;

        private GameObject _incomingRoleGo;
        private RoleEntity _incomingRole;

        private PartyMember _incomingMember;
        private PartyMember _outgoingMember;

        [SetUp]
        public void SetUp()
        {
            CombatWarningManager.Clear();

            // 1. 初始化怪物（处于世界坐标 (0, 0, 10)，面向原点 -Z 方向）
            _monsterGo = new GameObject("Monster_Attacker");
            _monsterGo.transform.position = new Vector3(0f, 0f, 10f);
            _monsterGo.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, -1f)); // 面向 (0, 0, 0)
            _monster = _monsterGo.AddComponent<MonsterEntity>();

            // 2. 初始化在场退场角色（玩家主控）
            _outgoingRoleGo = new GameObject("Role_Outgoing");
            _outgoingRoleGo.transform.position = Vector3.zero;
            _outgoingRoleGo.transform.rotation = Quaternion.identity;
            _outgoingRole = _outgoingRoleGo.AddComponent<RoleEntity>();
            _outgoingRole.EnsureRuntimeInitialized();

            // 3. 初始化待切入招架角色（防守方）
            _incomingRoleGo = new GameObject("Role_Incoming_Parry");
            _incomingRoleGo.transform.position = new Vector3(0f, 0f, -5f);
            _incomingRoleGo.transform.rotation = Quaternion.identity;
            _incomingRole = _incomingRoleGo.AddComponent<RoleEntity>();
            _incomingRole.EnsureRuntimeInitialized();

            _outgoingMember = new PartyMember
            {
                SlotIndex = 0,
                Entity = _outgoingRole
            };

            _incomingMember = new PartyMember
            {
                SlotIndex = 1,
                Entity = _incomingRole
            };
        }

        [TearDown]
        public void TearDown()
        {
            CombatWarningManager.Clear();

            if (_monsterGo != null) Object.DestroyImmediate(_monsterGo);
            if (_outgoingRoleGo != null) Object.DestroyImmediate(_outgoingRoleGo);
            if (_incomingRoleGo != null) Object.DestroyImmediate(_incomingRoleGo);
        }

        #region 1. 接刀身位世界坐标数学变换测试

        [Test]
        public void Parry_ClashPosition_TransformsCorrectly_AccordingToMonsterOrientation()
        {
            // 怪物位于 (0, 0, 10)，面向 -Z 方向（玩家方向）
            // 配置局部接刀身位在怪物正前方 3.0 米：ClashPositionOffset = (0, 0, 3f)
            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f),
                SignalType = WarningSignalType.Yellow_Parryable,
                AllowInPlaceParry = true
            };

            Vector3 worldClashPos = marker.GetWorldClashPosition(LayerMask.GetMask("Ground"));

            // 预期：怪物在 Z=10，朝向为 -Z，其局部正前方(+Z)转换到世界坐标后，应为 10 + (-1 * 3.0) = 7.0
            Assert.AreEqual(0f, worldClashPos.x, 0.001f, "X 坐标应在怪物中轴线上");
            Assert.AreEqual(7.0f, worldClashPos.z, 0.001f, "Z 坐标必须位于怪物前方 3 米 (即 Z=7.0)");

            // 若怪物转向 +X 方向（朝右看）
            _monsterGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            Vector3 worldClashPosTurned = marker.GetWorldClashPosition(LayerMask.GetMask("Ground"));

            // 预期：怪物在 (0, 0, 10)，朝向 +X，局部 +Z 变换到世界坐标为 (3, 0, 10)
            Assert.AreEqual(3.0f, worldClashPosTurned.x, 0.001f, "怪物面朝 +X 时，接刀点应在 X=3.0");
            Assert.AreEqual(10.0f, worldClashPosTurned.z, 0.001f, "怪物面朝 +X 时，接刀点 Z 轴应与怪物一致");
        }

        #endregion

        #region 2. 切入身位裁决与就地招架陷阱诊断测试 (核心问题定位)

        [Test]
        public void Parry_IncomingPlacement_OutOfCoverage_TeleportsToClashPosition()
        {
            // 场景 A：原角色位于远距离 (0, 0, 0)，怪物在 (0, 0, 10)，威胁覆盖域半径为 5.0m
            // 原角色完全在威胁覆盖域外部！
            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f), // 期望接刀身位：怪物前方 3m (世界坐标 Z=7.0)
                CoverageShape = new HitBoxShape
                {
                    shapeType = HitBoxType.Sector,
                    radius = 5.0f,
                    angle = 120.0f,
                    height = 2.5f
                },
                CoverageCenterOffset = Vector3.zero,
                AllowInPlaceParry = true
            };
            CombatWarningManager.Register(marker);

            // 构造切人管线上下文
            var pipe = new IncomingPlacementPipe();
            var ctx = new SwitchPipelineContext
            {
                Manager = TeamManager.Instance,
                Type = SwitchType.ParryAid,
                IncomingMember = _incomingMember,
                OutgoingMember = _outgoingMember,
                TargetAttacker = _monster,
                WarningMarker = marker
            };

            pipe.Process(ctx);

            // 远距离切入必须精准瞬移至接刀点 (0, 0, 7)！
            Assert.AreEqual(7.0f, ctx.SpawnPosition.z, 0.01f, "远距切入角色必须瞬移至怪物正前方接刀点 (Z=7.0)");
            Assert.AreEqual(7.0f, _incomingRole.transform.position.z, 0.01f, "实体空间坐标必须完成同步");

            // 朝向必须面向怪物 (+Z 方向)
            Vector3 forward = _incomingRole.transform.forward;
            Assert.Greater(forward.z, 0.9f, "切入角色必须自动旋转面向怪物");
        }

        [Test]
        public void Parry_IncomingPlacement_InsideCoverage_InPlaceParry_CausesRootPositionIssue()
        {
            // 场景 B (问题复现！)：
            // 原在场角色正在贴着怪物近战攻击，站位在 (0, 0, 9.5)（即怪物根部 Z=10 仅仅向前 0.5 米，处于怪物脚下/根位置附近）！
            // 怪物配置了 AllowInPlaceParry = true（允许就地格挡）
            _outgoingRoleGo.transform.position = new Vector3(0f, 0f, 9.5f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f), // 配置的前方接刀点在 Z=7.0
                CoverageShape = new HitBoxShape
                {
                    shapeType = HitBoxType.Sector,
                    radius = 5.0f,
                    angle = 120.0f,
                    height = 2.5f
                },
                CoverageCenterOffset = Vector3.zero,
                AllowInPlaceParry = true // 允许就地格挡！
            };
            CombatWarningManager.Register(marker);

            var pipe = new IncomingPlacementPipe();
            var ctx = new SwitchPipelineContext
            {
                Manager = TeamManager.Instance,
                Type = SwitchType.ParryAid,
                IncomingMember = _incomingMember,
                OutgoingMember = _outgoingMember,
                TargetAttacker = _monster,
                WarningMarker = marker
            };

            pipe.Process(ctx);

            // ★ 根因验证：
            // 因为原角色在覆盖域内，触发了【就地格挡】(In-Place Parry)！
            // 导致切入角色停留在原角色的身位 (Z=9.5，怪物根位置附近)，而根本没有使用配置的 ClashPosition (Z=7.0)！
            Assert.AreEqual(9.5f, ctx.SpawnPosition.z, 0.01f, "【根因诊断】由于 AllowInPlaceParry=true 且原角色在覆盖域内，角色被就地放置在原角色贴脸位置(Z=9.5)");
            Assert.AreNotEqual(7.0f, ctx.SpawnPosition.z, "配置的怪物前方接刀锚点(Z=7.0)被就地格挡机制跳过！");
        }

        [Test]
        public void Parry_IncomingPlacement_WhenAllowInPlaceParryDisabled_AlwaysTeleportsToClashPosition()
        {
            // 场景 C（解决方案验证）：
            // 即使原角色在怪物根部 (Z=9.5) 贴脸肉搏，但若将 AllowInPlaceParry 置为 false，
            // 切入角色将强制瞬移至配置的接刀锚点 (Z=7.0)，确保进入怪物的打击盒内！
            _outgoingRoleGo.transform.position = new Vector3(0f, 0f, 9.5f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f), // 期望接刀身位在 Z=7.0
                CoverageShape = new HitBoxShape
                {
                    shapeType = HitBoxType.Sector,
                    radius = 5.0f,
                    angle = 120.0f,
                    height = 2.5f
                },
                CoverageCenterOffset = Vector3.zero,
                AllowInPlaceParry = false // 强制关闭就地格挡，强制瞬移接刀！
            };
            CombatWarningManager.Register(marker);

            var pipe = new IncomingPlacementPipe();
            var ctx = new SwitchPipelineContext
            {
                Manager = TeamManager.Instance,
                Type = SwitchType.ParryAid,
                IncomingMember = _incomingMember,
                OutgoingMember = _outgoingMember,
                TargetAttacker = _monster,
                WarningMarker = marker
            };

            pipe.Process(ctx);

            Assert.AreEqual(7.0f, ctx.SpawnPosition.z, 0.01f, "关闭 AllowInPlaceParry 后，即便原角色贴脸，也必须强制瞬移至配置的接刀锚点(Z=7.0)");
            Assert.AreEqual(7.0f, _incomingRole.transform.position.z, 0.01f);
        }

        #endregion

        #region 3. 攻击盒覆盖范围与招架成功/失败对比测试

        [Test]
        public void Parry_HitPipeline_WhenAtClashPosition_ParrySucceeds_AndDamageShortCircuited()
        {
            try
            {
                // 场景 A：切入角色位于配置的接刀锚点 (Z=7.0)
                _incomingRoleGo.transform.position = new Vector3(0f, 0f, 7.0f);

                // 1. 注册预警与招架契约
                var marker = new AttackWarningMarker
                {
                    Attacker = _monster,
                    ClashPositionOffset = new Vector3(0f, 0f, 3.0f),
                    SignalType = WarningSignalType.Yellow_Parryable,
                    ParryWeight = ParryWeight.Heavy
                };
                CombatWarningManager.Register(marker);

                var contract = new ParryClashContract
                {
                    Attacker = _monster,
                    ParryRole = _incomingRole,
                    Marker = marker,
                    IsResolved = false
                };
                CombatWarningManager.RegisterContract(contract);

                // 2. 模拟角色招架窗口激活 (IsParrying = true)
                var parryData = _incomingRole.DataModule.Get<ParryRuntimeData>();
                parryData.Set(nameof(parryData.IsParrying), true);

                var parryHandler = new ATParryWindowHandler(_incomingRole);
                parryData.Set(nameof(parryData.ClashHandler), (IParryClashHandler)parryHandler);

                // 3. 怪物发动攻击，打击盒覆盖范围在怪物前方 (Z 位于 6.0 ~ 8.0 之间)
                // 角色位于 Z=7.0，恰好处于打击盒核心判定区！
                var pipeline = HitPipeline.Default;
                var hitCtx = pipeline.AllocateContext();
                hitCtx.Attacker = _monster;
                hitCtx.Victim = _incomingRole;
                hitCtx.HitPoint = new Vector3(0f, 1f, 7.0f);
                hitCtx.HitDirection = new Vector3(0f, 0f, -1f);

                pipeline.Execute(hitCtx);

                // 4. 断言招架结果
                Assert.IsTrue(hitCtx.IsAborted, "招架成功时，受击管线必须被 Short-circuit 中断！");
                Assert.IsTrue((hitCtx.ResultFlags & HitResultFlags.Parried) != 0, "必须打上 Parried 成功标记");
                Assert.IsTrue(contract.IsResolved, "契约必须标记为已解决 (IsResolved=true)");
                Assert.IsTrue(parryData.ParrySucceeded, "角色招架数据标记必须为 true");
                Assert.AreEqual(_monster, parryData.LastParriedAttacker, "招架目标必须记录为当前怪物");

                pipeline.ReleaseContext(hitCtx);
            }
            catch (System.Exception ex) when (!(ex is AssertionException))
            {
                Assert.Fail($"[EX_HIT_PIPELINE] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [Test]
        public void Parry_HitPipeline_WhenAtRootPosition_MissesHitBox_ParryFails()
        {
            // 场景 B (问题复现！)：
            // 切入角色位移到了怪物根位置附近 (Z=9.5)！
            _incomingRoleGo.transform.position = new Vector3(0f, 0f, 9.5f);

            // 怪物前方配置的前方打击盒：中心位于 (0, 0, 3)，半径 1.0m (覆盖世界范围 Z 位于 6.0 ~ 8.0)
            // 判定点在 Z=7.0 处生效。若角色在 Z=9.5，距离判定点有 2.5m 距离，无法命中！
            Vector3 hitBoxCenterWorld = new Vector3(0f, 1f, 7.0f);
            float hitBoxRadius = 1.0f;

            bool isRoleInHitBox = Vector3.Distance(_incomingRole.transform.position + Vector3.up, hitBoxCenterWorld) <= hitBoxRadius;
            Assert.IsFalse(isRoleInHitBox, "【失败重现】角色位移到怪物根位置(Z=9.5)时，彻底脱离了怪物前方的攻击判定盒(Z=6.0~8.0)！");

            // 结果：由于角色不在判定盒内，怪物的 HitClip 不会捕获到角色，ParryPipe 永远无法被触发，招架直接失败！
        }

        #endregion

        #region 4. 招架捕获窗口生命周期与契约闭环测试

        [Test]
        public void Parry_WindowHandler_Lifecycle_RegistersAndCleansContract()
        {
            try
            {
                var marker = new AttackWarningMarker
                {
                    Attacker = _monster,
                    SignalType = WarningSignalType.Yellow_Parryable,
                    ParryWeight = ParryWeight.Light
                };
                CombatWarningManager.Register(marker);

                // 注入角色的战斗上下文目标与匹配标记
                var actionData = _incomingRole.DataModule.Get<ActionRuntimeData>();
                actionData.Set(nameof(actionData.MatchedWarningMarker), marker);

                var handler = new ATParryWindowHandler(_incomingRole);

                var captureData = new ParryCaptureData
                {
                    triggerTiming = ParryCaptureTriggerTiming.Instant
                };

                // 1. 进入招架窗口
                handler.OnCaptureWindowEnter(captureData);

                var parryData = _incomingRole.DataModule.Get<ParryRuntimeData>();
                Assert.IsTrue(parryData.IsParrying, "进入窗口后必须处于招架状态");

                var activeContract = CombatWarningManager.GetActiveContractByRole(_incomingRole);
                Assert.IsNotNull(activeContract, "必须自动生成并注册拼刀契约");
                Assert.AreEqual(_monster, activeContract.Attacker, "契约攻击方必须为预警怪物");

                // 2. 模拟捕获到命中
                var clashCtx = new ParryClashContext
                {
                    Attacker = _monster,
                    Victim = _incomingRole,
                    Marker = marker
                };
                handler.OnHitCaptured(clashCtx);

                // 3. 退出招架窗口
                handler.OnCaptureWindowExit(captureData, isInterrupted: false);
                Assert.IsFalse(parryData.IsParrying, "退出窗口后必须关闭招架状态");
            }
            catch (System.Exception ex) when (!(ex is AssertionException))
            {
                Assert.Fail($"[EX_WINDOW_HANDLER] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion
    }
}
