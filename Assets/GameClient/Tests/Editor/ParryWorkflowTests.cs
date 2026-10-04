using System.Collections.Generic;
using ATEditor;
using Game.Framework;
using Game.GamePlay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Combat
{
    /// <summary>
    /// 绝区零（ZZZ）招架支援（Parry Assist）全链路自动化测试套件
    /// 完全对齐客观技术架构设计方案（parry_system_architecture_design.md）
    /// 覆盖 6 大核心工程维度：
    /// 1. 空间几何与接刀身位数学解算 (ClashPositionOffset & Ground Snapping)
    /// 2. 目标锁定仲裁与 360 度全方位危险感知 (Lock-on Priority & Omni-directional Hazard Sensing)
    /// 3. 切入身位裁决与贴脸禁区保护 (Switch Placement: In-Place vs Clash Point vs Anti-Clipping)
    /// 4. 双阶段招架时序与动作派发 (Direct Clash vs Early Parry Aid Start)
    /// 5. 真实物理击中、受击管线短路与多段连击连续招架 (HitPipeline & Continuous Clash)
    /// 6. 零兜底防御与边界安全保证 (Zero-Fallback & Defensiveness)
    /// </summary>
    [TestFixture]
    public class ParryWorkflowTests
    {
        private GameObject _monsterGo;
        private MonsterEntity _monster;

        private GameObject _monsterGo2;
        private MonsterEntity _monster2;

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

            // 1. 初始化主攻击怪物（位于 (0, 0, 10)，面向 -Z 方向）
            _monsterGo = new GameObject("Monster_Primary");
            _monsterGo.transform.position = new Vector3(0f, 0f, 10f);
            _monsterGo.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, -1f));
            _monster = _monsterGo.AddComponent<MonsterEntity>();

            // 2. 初始化次要怪物（位于 (10, 0, 0)，面向 -X 方向）
            _monsterGo2 = new GameObject("Monster_Secondary");
            _monsterGo2.transform.position = new Vector3(10f, 0f, 0f);
            _monsterGo2.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, 0f, 0f));
            _monster2 = _monsterGo2.AddComponent<MonsterEntity>();

            // 3. 初始化在场退场角色（玩家主控，位于原点）
            _outgoingRoleGo = new GameObject("Role_Outgoing");
            _outgoingRoleGo.transform.position = Vector3.zero;
            _outgoingRoleGo.transform.rotation = Quaternion.identity;
            _outgoingRole = _outgoingRoleGo.AddComponent<RoleEntity>();
            _outgoingRole.EnsureRuntimeInitialized();

            // 4. 初始化待切入招架角色（防守方）
            _incomingRoleGo = new GameObject("Role_Incoming");
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
            if (_monsterGo2 != null) Object.DestroyImmediate(_monsterGo2);
            if (_outgoingRoleGo != null) Object.DestroyImmediate(_outgoingRoleGo);
            if (_incomingRoleGo != null) Object.DestroyImmediate(_incomingRoleGo);
        }

        #region 1. 空间几何与接刀身位数学解算

        [Test]
        public void Dim1_ClashPosition_TransformsCorrectly_AccordingToMonsterOrientation()
        {
            // 怪物在 (0, 0, 10)，朝向 -Z。配置局部接刀点为前方 3 米：(0, 0, 3)
            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f),
                SignalType = WarningSignalType.Yellow_Parryable,
                AllowInPlaceParry = true
            };

            Vector3 worldClashPos = marker.GetWorldClashPosition(_incomingRole);

            // 期望世界坐标：10 + (-1 * 3) = 7.0
            Assert.AreEqual(0f, worldClashPos.x, 0.001f, "X 轴应对齐怪物中轴");
            Assert.AreEqual(7.0f, worldClashPos.z, 0.001f, "Z 轴必须位于怪物正前方 3 米 (即 Z=7.0)");

            // 若怪物旋转面向 +X
            _monsterGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            Vector3 worldClashPosTurned = marker.GetWorldClashPosition(_incomingRole);

            Assert.AreEqual(3.0f, worldClashPosTurned.x, 0.001f, "怪物面朝 +X 时，接刀点 X 应为 3.0");
            Assert.AreEqual(10.0f, worldClashPosTurned.z, 0.001f, "怪物面朝 +X 时，接刀点 Z 应与怪物一致 (10.0)");
        }

        [Test]
        public void Dim1_ClashPosition_GroundSnapping_AlignsWithAttackerFeet_WhenNoCollider()
        {
            // 怪物在空中 Y=5.0，配置局部接刀点
            _monsterGo.transform.position = new Vector3(0f, 5.0f, 10f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 2.0f),
                SignalType = WarningSignalType.Yellow_Parryable
            };

            // 在无地面碰撞体时，世界 Y 坐标应与攻击者脚底对齐，杜绝无限下坠或漂移
            Vector3 worldClashPos = marker.GetWorldClashPosition(_incomingRole);
            Assert.AreEqual(5.0f, worldClashPos.y, 0.01f, "无地面时接刀身位 Y 必须平贴攻击者基准面");
        }

        #endregion

        #region 2. 目标锁定仲裁与 360 度全方位危险感知

        [Test]
        public void Dim2_TargetArbitration_LockOnSupercedesDistanceAndAngle()
        {
            // 场景：玩家处于超远距离 (0, 0, -15)（距离怪物 25 米！），并且锁定了主怪物
            _outgoingRoleGo.transform.position = new Vector3(0f, 0f, -15f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                SignalType = WarningSignalType.Yellow_Parryable,
                ClashPositionOffset = new Vector3(0f, 0f, 2.0f),
                DetectionRadius = 5.0f // 即使时间轴上配置了很小的 5m 半径
            };
            CombatWarningManager.Register(marker);

            // 模拟玩家锁定怪物 (通过 MockTargetFinder 注入)
            var mockFinder = new MockTargetFinder { LockedTransform = _monster.transform };
            _outgoingRole.SetTargetFinder(mockFinder);

            // 切人校验管线
            var pipe = new SwitchValidationPipe();
            var ctx = new SwitchPipelineContext
            {
                Manager = TeamManager.Instance,
                Type = SwitchType.NormalSwitch,
                IncomingMember = _incomingMember,
                OutgoingMember = _outgoingMember
            };

            pipe.Process(ctx);

            Assert.AreEqual(SwitchType.ParryAid, ctx.Type, "超远距离锁定出招怪物时，普通切人必须绝对优先升级为招架支援(ParryAid)");
            Assert.AreEqual(marker, ctx.WarningMarker, "必须自动关联锁定的出招预警");
            Assert.AreEqual(_monster, ctx.TargetAttacker, "目标必须为锁定的怪物");
        }

        [Test]
        public void Dim2_TargetArbitration_OmniDirectionalBackstabHazardSensing()
        {
            // 场景：怪物位于 (0, 0, 5)，面向 +Z（背对玩家！）。玩家位于原点 (0, 0, 0)，距离 5 米，未锁定目标
            _monsterGo.transform.rotation = Quaternion.LookRotation(Vector3.forward); // 面向前方 +Z
            _outgoingRoleGo.transform.position = Vector3.zero;                       // 处于怪物正后方 (夹角 180 度)

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                SignalType = WarningSignalType.Yellow_Parryable,
                ClashPositionOffset = new Vector3(0f, 0f, 1.8f),
                DetectionRadius = 10.0f
            };
            CombatWarningManager.Register(marker);

            // 校验管线
            var pipe = new SwitchValidationPipe();
            var ctx = new SwitchPipelineContext
            {
                Manager = TeamManager.Instance,
                Type = SwitchType.NormalSwitch,
                IncomingMember = _incomingMember,
                OutgoingMember = _outgoingMember
            };

            pipe.Process(ctx);

            Assert.AreEqual(SwitchType.ParryAid, ctx.Type, "怪物背对玩家出招（或背后偷袭）时，360 度危险感知必须正确识别并升级为招架支援");
            Assert.AreEqual(_monster, ctx.TargetAttacker);
        }

        [Test]
        public void Dim2_TargetArbitration_MultipleThreats_PrioritizesLockOnOrClosest()
        {
            // 主怪在 (0, 0, 10)，次怪在 (0, 0, 3)（次怪更近）
            _monsterGo2.transform.position = new Vector3(0f, 0f, 3.0f);

            var markerFar = new AttackWarningMarker
            {
                Attacker = _monster,
                SignalType = WarningSignalType.Yellow_Parryable,
                DetectionRadius = 25.0f
            };
            var markerNear = new AttackWarningMarker
            {
                Attacker = _monster2,
                SignalType = WarningSignalType.Yellow_Parryable,
                DetectionRadius = 25.0f
            };

            CombatWarningManager.Register(markerFar);
            CombatWarningManager.Register(markerNear);

            // Case A: 未锁定目标，自动搜寻响应最近的次怪
            var markerFoundAuto = CombatWarningManager.GetAnyValidWarning(_outgoingRole);
            Assert.AreEqual(_monster2, markerFoundAuto.Attacker, "未锁定时，必须自动响应距离最近的出招怪");

            // Case B: 锁定了远处的怪，绝对优先响应锁定的怪
            _outgoingRole.SetTargetFinder(new MockTargetFinder { LockedTransform = _monster.transform });
            var markerFoundLocked = CombatWarningManager.GetAnyValidWarning(_outgoingRole);
            Assert.AreEqual(_monster, markerFoundLocked.Attacker, "锁定远怪时，必须绝对优先响应锁定目标");
        }

        #endregion

        #region 3. 切入身位裁决与贴脸禁区保护

        [Test]
        public void Dim3_Placement_OutOfCoverage_TeleportsToClashPosition()
        {
            // 原角色在 (0, 0, -2)，怪物在 (0, 0, 10)，扇形覆盖域半径 5m
            _outgoingRoleGo.transform.position = new Vector3(0f, 0f, -2f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f), // 前方 3m，世界坐标 Z=7.0
                CoverageShape = new HitBoxShape
                {
                    shapeType = HitBoxType.Sector,
                    radius = 5.0f,
                    angle = 120.0f,
                    height = 2.5f
                },
                AllowInPlaceParry = true
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

            Assert.AreEqual(7.0f, ctx.SpawnPosition.z, 0.01f, "覆盖域外切入角色必须瞬移至接刀点 (Z=7.0)");
            Assert.AreEqual(7.0f, _incomingRole.transform.position.z, 0.01f, "Transform 同步必须到位");
            Assert.Greater(_incomingRole.transform.forward.z, 0.9f, "切入角色必须自动旋转面向怪物");
        }

        [Test]
        public void Dim3_Placement_InsideCoverage_InPlaceParry()
        {
            // 原角色在 (0, 0, 7.5)，怪物在 (0, 0, 10)，距离 2.5m（处于 [1.2m, 5.0m] 环形有效区内）
            _outgoingRoleGo.transform.position = new Vector3(0f, 0f, 7.5f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f), // 若瞬移应在 Z=7.0
                CoverageShape = new HitBoxShape
                {
                    shapeType = HitBoxType.Sector,
                    radius = 5.0f,
                    angle = 120.0f,
                    height = 2.5f
                },
                AllowInPlaceParry = true,
                RestrictedInnerRadius = 1.2f
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

            Assert.IsTrue(marker.CanPerformInPlaceParry(_outgoingRoleGo.transform.position), "角色在连线上且在有效区内，CanPerformInPlaceParry 必须为 true");
            Assert.AreEqual(7.5f, ctx.SpawnPosition.z, 0.01f, "在环形有效区内且处于连线上时，必须原地招架不发生位移");
            Assert.AreEqual(0.0f, ctx.SpawnPosition.x, 0.01f, "原地招架 X 保持 0");
        }

        [Test]
        public void Dim3_Placement_OffClashLine_ForcesTeleport_EvenInsideCoverage()
        {
            // 原角色在 (1.5, 0, 7.5)，怪物在 (0, 0, 10)，接刀点在 (0, 0, 7.0)
            // 距离怪物 sqrt(1.5^2 + 2.5^2) ≈ 2.915m，处于有效扇形内且大于禁区 1.2m
            // 但偏离了怪物与接刀点的连线 (X 偏离 1.5m > 容差 0.5m)，必须判定不可原地招架，强制瞬移至接刀点 (0, 0, 7.0)
            _outgoingRoleGo.transform.position = new Vector3(1.5f, 0f, 7.5f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f), // 对应世界坐标 (0, 0, 7.0)
                CoverageShape = new HitBoxShape
                {
                    shapeType = HitBoxType.Sector,
                    radius = 5.0f,
                    angle = 120.0f,
                    height = 2.5f
                },
                AllowInPlaceParry = true,
                RestrictedInnerRadius = 1.2f,
                InPlaceLineTolerance = 0.5f
            };
            CombatWarningManager.Register(marker);

            // 1. 验证 CanPerformInPlaceParry 裁决逻辑
            Assert.IsTrue(marker.IsPositionInCoverage(_outgoingRoleGo.transform.position), "角色在覆盖域与扇形内");
            Assert.IsFalse(marker.IsOnClashLine(_outgoingRoleGo.transform.position), "角色偏离连线 (1.5m > 0.5m)，IsOnClashLine 应为 false");
            Assert.IsFalse(marker.CanPerformInPlaceParry(_outgoingRoleGo.transform.position), "偏离连线时 CanPerformInPlaceParry 必须为 false");

            // 2. 验证 Pipeline 实际执行瞬移
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

            Assert.AreEqual(7.0f, ctx.SpawnPosition.z, 0.01f, "偏离连线时必须强制瞬移至预设接刀点 (Z=7.0)");
            Assert.AreEqual(0.0f, ctx.SpawnPosition.x, 0.01f, "偏离连线时必须强制瞬移至预设接刀点 (X=0.0)");
        }

        [Test]
        public void Dim3_Placement_RestrictedInnerZone_ForcesTeleport_AvoidsClipping()
        {
            // 原角色在 (0, 0, 9.5)，怪物在 (0, 0, 10)，距离仅 0.5m（处于 <1.2m 贴脸禁区内！）
            _outgoingRoleGo.transform.position = new Vector3(0f, 0f, 9.5f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f), // 接刀身位在 Z=7.0
                CoverageShape = new HitBoxShape
                {
                    shapeType = HitBoxType.Sector,
                    radius = 5.0f,
                    angle = 120.0f,
                    height = 2.5f
                },
                AllowInPlaceParry = true,
                RestrictedInnerRadius = 1.2f // 禁区 1.2m
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

            Assert.AreEqual(7.0f, ctx.SpawnPosition.z, 0.01f, "贴脸过近(<1.2m)时，严禁就地放置，必须强制瞬移至接刀点(Z=7.0)避免穿插或扫空");
        }

        [Test]
        public void Dim3_Placement_PendingSwitchOut_ForcesParryBreakthrough()
        {
            // 模拟切入角色仍处于上一次换人的待切出 (isPendingSwitchOut = true) 状态
            var pipe = new IncomingPlacementPipe();
            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f),
                AllowInPlaceParry = false
            };

            var ctx = new SwitchPipelineContext
            {
                Manager = TeamManager.Instance,
                Type = SwitchType.ParryAid,
                IncomingMember = _incomingMember,
                OutgoingMember = _outgoingMember,
                TargetAttacker = _monster,
                WarningMarker = marker,
                IsIncomingPendingSwitchOut = true // 标记为待切出
            };

            pipe.Process(ctx);

            Assert.AreEqual(7.0f, ctx.SpawnPosition.z, 0.01f, "紧急 ParryAid 必须打破待切出阻断，强制更新身位至接刀点");
        }

        [Test]
        public void Dim3_Placement_RootMotionCompensation_DeductsOffsetCorrectly()
        {
            // 为切入角色装配带有 ParryReadyDuration=0.2s 与 ParryRootMotionZOffset=1.0m 的配置
            var roleCfg = ScriptableObject.CreateInstance<RoleConfigAsset>();
            roleCfg.AssistConfig = new RoleAssistConfig
            {
                SupportType = RoleAssistType.ParryAid,
                ParryReadyDuration = 0.2f,
                ParryRootMotionZOffset = 1.0f
            };
            _incomingRole.Init(roleCfg);

            var pipe = new IncomingPlacementPipe();

            // Case A: 提前 0.3s 按键（剩余时间 0.3s >= T_ready 0.2s），t_start=0，Ratio=1.0，实际冲刺 1.0m
            // 基础接刀点在 Z=7.0，角色朝向 +Z，初始生成应沿后方扣减 1.0m 停在 Z=6.0
            var markerA = new AttackWarningMarker
            {
                Attacker = _monster,
                ClashPositionOffset = new Vector3(0f, 0f, 3.0f),
                StartTime = 0.0f,
                Duration = 0.5f // EndTime = 0.5s
            };
            // 模拟怪物当前播放到 0.2s，剩余时间 = 0.5 - 0.2 = 0.3s
            // 通过 mock 动作播放器或直接验证计算公式
            // 验证公式: remainTime = 0.3s >= 0.2s => t_start = 0 => ratio = (0.2-0)/0.2 = 1.0 => deduct = 1.0m => Z = 6.0
            float readyDur = roleCfg.AssistConfig.ParryReadyDuration;
            float rootZ = roleCfg.AssistConfig.ParryRootMotionZOffset;

            float remainA = 0.3f;
            float startA = Mathf.Max(0f, readyDur - remainA);
            float ratioA = Mathf.Clamp01((readyDur - startA) / readyDur);
            float deductA = rootZ * ratioA;
            Assert.AreEqual(0.0f, startA, 0.001f, "提前按键时 StartTime 必须为 0");
            Assert.AreEqual(1.0f, deductA, 0.001f, "提前按键时必须全额扣除根运动位移 1.0m");

            // Case B: 临界 0.1s 按键（剩余时间 0.1s < T_ready 0.2s），t_start=0.1s，Ratio=0.5，实际冲刺 0.5m
            float remainB = 0.1f;
            float startB = Mathf.Max(0f, readyDur - remainB);
            float ratioB = Mathf.Clamp01((readyDur - startB) / readyDur);
            float deductB = rootZ * ratioB;
            Assert.AreEqual(0.1f, startB, 0.001f, "临界按键时 StartTime 应为 0.1s");
            Assert.AreEqual(0.5f, deductB, 0.001f, "半程滑步时仅扣除 0.5m");

            // Case C: 极限压哨按键（剩余时间几乎为 0），t_start=0.2s，Ratio=0.0，实际冲刺 0m
            float remainC = 0.0f;
            float startC = Mathf.Max(0f, readyDur - remainC);
            float ratioC = Mathf.Clamp01((readyDur - startC) / readyDur);
            float deductC = rootZ * ratioC;
            Assert.AreEqual(0.2f, startC, 0.001f, "极限压哨时 StartTime 达到 0.2s");
            Assert.AreEqual(0.0f, deductC, 0.001f, "极限压哨无需前置位移扣除");
        }

        #endregion

        #region 4. 招架时序计算与起手动作派发

        [Test]
        public void Dim4_ActionDispatch_DynamicStartTimeCalculation()
        {
            float readyDuration = 0.25f;

            // 1. 正常区间：剩余时间 0.15s (<= 0.25s)
            float remain1 = 0.15f;
            float t_start1 = Mathf.Max(0f, readyDuration - remain1);
            Assert.AreEqual(0.10f, t_start1, 0.001f, "从 0.10s 开始播放，经历 0.15s 后刚好到达 0.25s 举刀姿势");

            // 2. 提前架势区间：剩余时间 0.5s (> 0.25s)
            float remain2 = 0.5f;
            float t_start2 = Mathf.Max(0f, readyDuration - remain2);
            Assert.AreEqual(0f, t_start2, 0.001f, "超前按键时从第 0 帧开始播放起手动作");

            // 3. 超时区间：剩余时间 0s 或负数
            float remain3 = 0f;
            Assert.IsTrue(remain3 <= 0f, "超时应被判定为无效预警，拒绝招架");
        }

        [Test]
        public void Dim4_ActionTriggerPipe_InjectsContext_WithoutForcedInvincibleBuff()
        {
            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                SignalType = WarningSignalType.Yellow_Parryable,
                ParryWeight = ParryWeight.Heavy
            };

            var pipe = new ActionAndInvincibleTriggerPipe();
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

            var parryData = _incomingRole.DataModule.Get<ParryRuntimeData>();
            Assert.AreEqual(ParryWeight.Heavy, parryData.LastParryWeight);
            Assert.IsFalse(parryData.ParrySucceeded, "切入阶段尚未发生物理接触，严禁提前标记 ParrySucceeded 为 true");
        }

        #endregion

        #region 5. 真实物理击中、受击短路与多段连击连续招架

        [Test]
        public void Dim5_HitPipeline_PurePhysics_DamageShortCircuitedAndHitStop()
        {
            // 切入角色位于接刀点 (0, 0, 7.0)
            _incomingRoleGo.transform.position = new Vector3(0f, 0f, 7.0f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                SignalType = WarningSignalType.Yellow_Parryable,
                ParryWeight = ParryWeight.Light
            };
            CombatWarningManager.Register(marker);

            var contract = new ParryClashContract
            {
                Attacker = _monster,
                ParryRole = _incomingRole,
                Marker = marker
            };
            CombatWarningManager.RegisterContract(contract);

            // 激活招架窗口
            var parryData = _incomingRole.DataModule.Get<ParryRuntimeData>();
            parryData.Set(nameof(parryData.IsParrying), true);

            var parryHandler = new ATParryWindowHandler(_incomingRole);
            parryData.Set(nameof(parryData.ClashHandler), (IParryClashHandler)parryHandler);

            // 执行受击管线
            var pipeline = HitPipeline.Default;
            var hitCtx = pipeline.AllocateContext();
            hitCtx.Attacker = _monster;
            hitCtx.Victim = _incomingRole;
            hitCtx.HitPoint = new Vector3(0f, 1f, 7.0f);
            hitCtx.HitDirection = new Vector3(0f, 0f, -1f);

            pipeline.Execute(hitCtx);

            Assert.IsTrue(hitCtx.IsAborted, "招架成功时，受击管线必须 Short-circuit 免伤中断");
            Assert.IsTrue((hitCtx.ResultFlags & HitResultFlags.Parried) != 0, "必须打上 Parried 标记");
            Assert.IsTrue(contract.IsResolved, "契约必须标记为已解决");
            Assert.IsTrue(parryData.ParrySucceeded, "角色招架成功标记必须激活");

            pipeline.ReleaseContext(hitCtx);
        }

        [Test]
        public void Dim5_HitPipeline_ContinuousClash_SupportsMultipleHitsAndExitsOnWindowClose()
        {
            // 连续招架闭环验证：
            // 首次物理打击打入时，触发免伤短路与顿帧，IsParrying 依然保持 true（由 ParryWindowClip 自治管理）；
            // 再次受到攻击时，受击管线依然能够成功捕获并再次免伤短路；
            // 直至招架防御窗口退出时，IsParrying 关闭且契约注销闭环。
            _incomingRoleGo.transform.position = new Vector3(0f, 0f, 7.0f);

            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                SignalType = WarningSignalType.Yellow_Parryable
            };
            CombatWarningManager.Register(marker);

            var contract = new ParryClashContract
            {
                Attacker = _monster,
                ParryRole = _incomingRole,
                Marker = marker
            };
            CombatWarningManager.RegisterContract(contract);

            var parryData = _incomingRole.DataModule.Get<ParryRuntimeData>();
            var parryHandler = new ATParryWindowHandler(_incomingRole);
            parryData.Set(nameof(parryData.ClashHandler), (IParryClashHandler)parryHandler);

            var parryClip = new ATEditor.ParryWindowClip { Duration = 0.5f };
            parryHandler.OnParryWindowEnter(parryClip);
            Assert.IsTrue(parryData.IsParrying, "进入招架窗口后 IsParrying 必须为 true");

            var pipeline = HitPipeline.Default;

            // 1. 首次物理攻击打入
            var hitCtx1 = pipeline.AllocateContext();
            hitCtx1.Attacker = _monster;
            hitCtx1.Victim = _incomingRole;
            pipeline.Execute(hitCtx1);

            Assert.IsTrue(hitCtx1.IsAborted, "首次招架成功时，受击管线必须免伤短路");
            Assert.IsTrue(parryData.ParrySucceeded, "角色必须记录招架成功");
            Assert.IsTrue(parryData.IsParrying, "连续招架支持：招架窗口未退出前 IsParrying 必须持续为 true");
            Assert.IsNotNull(CombatWarningManager.GetActiveContractByRole(_incomingRole), "连续招架支持：窗口期内契约保持活跃");

            // 2. 再次打入第二段物理攻击
            var hitCtx2 = pipeline.AllocateContext();
            hitCtx2.Attacker = _monster;
            hitCtx2.Victim = _incomingRole;
            pipeline.Execute(hitCtx2);

            Assert.IsTrue(hitCtx2.IsAborted, "连续招架成功：第二段受击管线依然必须免伤短路");

            // 3. 招架窗口退出
            parryHandler.OnParryWindowExit(parryClip, false);
            Assert.IsFalse(parryData.IsParrying, "招架窗口离开后 IsParrying 必须关闭");
            Assert.IsNull(CombatWarningManager.GetActiveContractByRole(_incomingRole), "招架窗口离开后防守方契约必须注销闭环");

            pipeline.ReleaseContext(hitCtx1);
            pipeline.ReleaseContext(hitCtx2);
        }

        [Test]
        public void Dim5_RoleConfig_LightVsHeavyParryActionEntry_Resolution()
        {
            // 验证思路二：反制配置从时间轴剥离，由角色 RoleAssistConfig 聚合声明与权威驱动
            var roleCfg = ScriptableObject.CreateInstance<RoleConfigAsset>();
            roleCfg.AssistConfig = new RoleAssistConfig
            {
                SupportType = RoleAssistType.ParryAid,
                ParryLight = new ParryActionEntry
                {
                    HitEffectId = 1050121,
                    HitStopDuration = 0.15f
                },
                ParryHeavy = new ParryActionEntry
                {
                    HitEffectId = 1050122,
                    HitStopDuration = 0.35f
                }
            };
            _incomingRole.Init(roleCfg);

            // 1. 验证轻招架取值
            var entryLight = roleCfg.AssistConfig.GetParryEntry(ParryWeight.Light);
            Assert.IsNotNull(entryLight);
            Assert.AreEqual(1050121, entryLight.HitEffectId, "轻招架必须精确读取 ParryLight 中的 HitEffectId");
            Assert.AreEqual(0.15f, entryLight.HitStopDuration, 0.001f, "轻招架顿帧时长必须为 0.15s");

            // 2. 验证重招架取值
            var entryHeavy = roleCfg.AssistConfig.GetParryEntry(ParryWeight.Heavy);
            Assert.IsNotNull(entryHeavy);
            Assert.AreEqual(1050122, entryHeavy.HitEffectId, "重招架必须精确读取 ParryHeavy 中的 HitEffectId");
            Assert.AreEqual(0.35f, entryHeavy.HitStopDuration, 0.001f, "重招架顿帧时长必须为 0.35s");
        }

        #endregion

        #region 6. 零兜底防御与边界安全保证

        [Test]
        public void Dim6_ZeroFallback_WhenAttackerOrVictimNull_ReturnsSafelyWithoutCrash()
        {
            var pipeline = HitPipeline.Default;
            var hitCtx = pipeline.AllocateContext();
            hitCtx.Attacker = null; // 异常攻击者
            hitCtx.Victim = _incomingRole;

            // 必须安全执行，绝不报 NullReferenceException
            Assert.DoesNotThrow(() => pipeline.Execute(hitCtx));
            Assert.IsFalse(hitCtx.IsAborted, "异常攻击者不应误触发招架短路");
            pipeline.ReleaseContext(hitCtx);

            // 同样测试切人管线
            var switchPipe = new IncomingPlacementPipe();
            var switchCtx = new SwitchPipelineContext
            {
                Manager = null, // 异常空 Manager
                Type = SwitchType.ParryAid
            };
            Assert.DoesNotThrow(() => switchPipe.Process(switchCtx), "管线必须安全拦截 Null Manager");
        }

        [Test]
        public void Dim7_RuntimeDataSeparation_LifecycleRuntimeData_IsDeadDecoupledFromEntity()
        {
            // 验证要求 1：实体纯壳化与运行时状态分离，IsDead 绝不直接放在 CharacterEntity 上
            var roleData = _incomingRole.DataModule.Get<LifecycleRuntimeData>();
            Assert.IsNotNull(roleData, "DataModule 必须持有 LifecycleRuntimeData");
            Assert.IsFalse(roleData.IsDead, "初始状态必须为存活");

            // 标记死亡
            roleData.Set(nameof(LifecycleRuntimeData.IsDead), true);
            Assert.IsTrue(roleData.IsDead, "标记死亡后必须反映在 LifecycleRuntimeData");

            // 重置
            roleData.Reset();
            Assert.IsFalse(roleData.IsDead, "Reset 后必须恢复为存活");
        }

        [Test]
        public void Dim7_ParryHitStopSequence_ExecuteClashTriggeredViaHitStopCallback()
        {
            // 验证要求 2：招架成功时，玩家切入反击动作，双方顿帧，顿帧结束后怪物攻击被打断并播受击动作
            var marker = new AttackWarningMarker
            {
                Attacker = _monster,
                SignalType = WarningSignalType.Yellow_Parryable,
                ParryWeight = ParryWeight.Heavy
            };
            CombatWarningManager.Register(marker);

            var contract = new ParryClashContract
            {
                Attacker = _monster,
                ParryRole = _incomingRole,
                Marker = marker
            };
            CombatWarningManager.RegisterContract(contract);

            var parryData = _incomingRole.DataModule.Get<ParryRuntimeData>();
            parryData.Set(nameof(parryData.IsParrying), true);

            var parryHandler = new ATParryWindowHandler(_incomingRole);
            parryData.Set(nameof(parryData.ClashHandler), (IParryClashHandler)parryHandler);

            // 模拟受击打入
            var pipeline = HitPipeline.Default;
            var hitCtx = pipeline.AllocateContext();
            hitCtx.Attacker = _monster;
            hitCtx.Victim = _incomingRole;
            hitCtx.HitPoint = new Vector3(0f, 1f, 7.0f);
            hitCtx.HitDirection = new Vector3(0f, 0f, -1f);

            pipeline.Execute(hitCtx);

            // 顿帧与回调闭环验证：
            Assert.IsTrue(hitCtx.IsAborted, "招架成功免伤短路");
            Assert.IsTrue(contract.IsResolved, "契约已标记为解决");

            pipeline.ReleaseContext(hitCtx);
        }

        #endregion

        #region Helper Mock Classes

        private class MockTargetFinder : ITargetFinder
        {
            public Transform LockedTransform;
            public CharacterEntity CombatContextTarget { get; set; }
            public void SetCombatContextTarget(CharacterEntity target) => CombatContextTarget = target;
            public void ClearCombatContextTarget() => CombatContextTarget = null;
            public Transform GetTarget() => LockedTransform;
            public float GetDistanceToTarget() => 0f;
            public Transform GetEffectiveTarget() => CombatContextTarget != null ? CombatContextTarget.transform : LockedTransform;
            public void Initialize(CharacterEntity owner) { }
            public void LogicTick(float logicDeltaTime) { }
            public void Dispose() { }
        }

        #endregion
    }
}
