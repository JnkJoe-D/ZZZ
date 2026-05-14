using System;
using System.Collections.Generic;
using System.Linq;
using Game.Logic.Character;
using Game.Logic.Player;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 目标选择模式。
    /// </summary>
    public enum BehaviorTreeTargetSelectionMode
    {
        LocalPlayerOnly,
        LocalPlayerPreferred,
        ClosestCharacter
    }

    /// <summary>
    /// 阵营筛选方式。
    /// </summary>
    public enum BehaviorTreeTargetFactionFilter
    {
        Any,
        SameFaction,
        DifferentFaction
    }

    /// <summary>
    /// 玩家控制类型筛选方式。
    /// </summary>
    public enum BehaviorTreeTargetControlFilter
    {
        Any,
        PlayerOnly,
        NonPlayerOnly
    }

    [Serializable]
    public struct BehaviorTreeTargetSelectionOptions
    {
        /// <summary>目标选择模式。</summary>
        public BehaviorTreeTargetSelectionMode SelectionMode;
        /// <summary>阵营筛选方式。</summary>
        public BehaviorTreeTargetFactionFilter FactionFilter;
        /// <summary>玩家控制类型筛选方式。</summary>
        public BehaviorTreeTargetControlFilter ControlFilter;
        /// <summary>最小索敌距离。</summary>
        public float MinDistance;
        /// <summary>最大索敌距离。</summary>
        public float MaxDistance;
        /// <summary>视野角，360 表示无方向限制。</summary>
        public float FieldOfViewDegrees;
        /// <summary>是否优先保留当前目标。</summary>
        public bool RetainCurrentTarget;
        /// <summary>保留当前目标时允许的额外距离倍率。</summary>
        public float RetainDistanceMultiplier;

        /// <summary>
        /// 默认的目标选择参数。
        /// </summary>
        public static BehaviorTreeTargetSelectionOptions Default =>
            new BehaviorTreeTargetSelectionOptions
            {
                SelectionMode = BehaviorTreeTargetSelectionMode.LocalPlayerPreferred,
                FactionFilter = BehaviorTreeTargetFactionFilter.Any,
                ControlFilter = BehaviorTreeTargetControlFilter.Any,
                MinDistance = 0f,
                MaxDistance = 20f,
                FieldOfViewDegrees = 360f,
                RetainCurrentTarget = true,
                RetainDistanceMultiplier = 1.25f
            };
    }

    /// <summary>
    /// 目标元数据，记录阵营和是否为玩家控制。
    /// </summary>
    public readonly struct BehaviorTreeTargetMetadata
    {
        /// <summary>
        /// 构造目标元数据。
        /// </summary>
        /// <param name="factionId">角色阵营 ID。</param>
        /// <param name="isPlayerControlled">是否由玩家控制。</param>
        public BehaviorTreeTargetMetadata(int factionId, bool isPlayerControlled)
        {
            FactionId = factionId;
            IsPlayerControlled = isPlayerControlled;
        }

        public int FactionId { get; }
        public bool IsPlayerControlled { get; }
    }

    /// <summary>
    /// 场景中的角色注册表，供目标选择系统遍历候选角色。
    /// </summary>
    public static class BehaviorTreeCharacterRegistry
    {
        private static readonly HashSet<CharacterEntity> characters = new HashSet<CharacterEntity>();
        private static readonly Dictionary<CharacterEntity, BehaviorTreeTargetMetadata> metadataByCharacter =
            new Dictionary<CharacterEntity, BehaviorTreeTargetMetadata>();

        /// <summary>
        /// 当前已注册且有效的角色集合。
        /// </summary>
        public static IEnumerable<CharacterEntity> Characters =>
            characters.Where(character => character != null && character.gameObject.activeInHierarchy);

        /// <summary>
        /// 注册一个角色到索敌系统。
        /// </summary>
        /// <param name="character">要注册的角色。</param>
        public static void Register(CharacterEntity character)
        {
            if (character != null)
            {
                characters.Add(character);
            }
        }

        /// <summary>
        /// 从索敌系统移除一个角色。
        /// </summary>
        /// <param name="character">要移除的角色。</param>
        public static void Unregister(CharacterEntity character)
        {
            if (character != null)
            {
                characters.Remove(character);
                metadataByCharacter.Remove(character);
            }
        }

        /// <summary>
        /// 设置某个角色的目标元数据。
        /// </summary>
        /// <param name="character">目标角色。</param>
        /// <param name="metadata">要写入的元数据。</param>
        public static void SetMetadata(CharacterEntity character, BehaviorTreeTargetMetadata metadata)
        {
            if (character != null)
            {
                metadataByCharacter[character] = metadata;
            }
        }

        /// <summary>
        /// 清理某个角色的元数据。
        /// </summary>
        /// <param name="character">要清理的角色。</param>
        public static void ClearMetadata(CharacterEntity character)
        {
            if (character != null)
            {
                metadataByCharacter.Remove(character);
            }
        }

        /// <summary>
        /// 尝试读取角色元数据。
        /// </summary>
        /// <param name="character">目标角色。</param>
        /// <param name="metadata">输出的元数据。</param>
        /// <returns>是否读取成功。</returns>
        public static bool TryGetMetadata(CharacterEntity character, out BehaviorTreeTargetMetadata metadata)
        {
            if (character != null && metadataByCharacter.TryGetValue(character, out metadata))
            {
                return true;
            }

            metadata = default;
            return false;
        }
    }

    /// <summary>
    /// 从场景注册角色中实时选择目标的目标提供器。
    /// </summary>
    public sealed class BehaviorTreeSceneCharacterTargetProvider : IBehaviorTreeTargetProvider
    {
        private readonly CharacterEntity owner;
        private readonly BehaviorTreeTargetSelectionOptions options;
        private readonly Func<BehaviorTreeBlackboard> blackboardResolver;
        private BehaviorTreeTargetData? currentTarget;

        /// <summary>
        /// 构造场景目标提供器。
        /// </summary>
        /// <param name="owner">发起索敌的角色。</param>
        /// <param name="options">索敌参数。</param>
        public BehaviorTreeSceneCharacterTargetProvider(
            CharacterEntity owner,
            BehaviorTreeTargetSelectionOptions options,
            Func<BehaviorTreeBlackboard> blackboardResolver = null)
        {
            this.owner = owner;
            this.options = options;
            this.blackboardResolver = blackboardResolver;
        }

        /// <summary>
        /// 尝试为当前 owner 选择一个目标。
        /// </summary>
        /// <param name="targetData">输出的目标数据。</param>
        /// <returns>是否选择成功。</returns>
        public bool TryGetTarget(out BehaviorTreeTargetData targetData)
        {
            if (owner == null)
            {
                targetData = default;
                return false;
            }

            BehaviorTreeTargetSelectionOptions resolvedOptions = ResolveRuntimeOptions();
            if (BehaviorTreeTargetSelector.TrySelectTarget(
                    owner.transform.position,
                    owner.transform.forward,
                    GetOwnerFactionId(),
                    resolvedOptions,
                    EnumerateCandidates(),
                    currentTarget,
                    out targetData))
            {
                currentTarget = targetData;
                return true;
            }

            currentTarget = null;
            return false;
        }

        /// <summary>
        /// 解析当前帧真正生效的索敌参数；优先读取黑板，未配置时回退到组件默认值。
        /// </summary>
        /// <returns>当前帧生效的索敌参数。</returns>
        private BehaviorTreeTargetSelectionOptions ResolveRuntimeOptions()
        {
            BehaviorTreeTargetSelectionOptions resolvedOptions = options;
            BehaviorTreeBlackboard blackboard = blackboardResolver?.Invoke();
            if (blackboard == null)
            {
                return resolvedOptions;
            }

            resolvedOptions.MinDistance = blackboard.GetValueOrDefault(
                BehaviorTreeCharacterBlackboardKeys.TargetMinDistance,
                resolvedOptions.MinDistance);
            resolvedOptions.MaxDistance = blackboard.GetValueOrDefault(
                BehaviorTreeCharacterBlackboardKeys.TargetSearchRadius,
                resolvedOptions.MaxDistance);
            resolvedOptions.FieldOfViewDegrees = blackboard.GetValueOrDefault(
                BehaviorTreeCharacterBlackboardKeys.TargetFieldOfView,
                resolvedOptions.FieldOfViewDegrees);
            resolvedOptions.RetainCurrentTarget = blackboard.GetValueOrDefault(
                BehaviorTreeCharacterBlackboardKeys.TargetRetainCurrent,
                resolvedOptions.RetainCurrentTarget);
            resolvedOptions.RetainDistanceMultiplier = blackboard.GetValueOrDefault(
                BehaviorTreeCharacterBlackboardKeys.TargetRetainDistanceMultiplier,
                resolvedOptions.RetainDistanceMultiplier);
            return resolvedOptions;
        }

        /// <summary>
        /// 枚举当前场景中所有有效候选目标。
        /// </summary>
        private IEnumerable<BehaviorTreeTargetData> EnumerateCandidates()
        {
            CharacterEntity localPlayer = ResolveLocalPlayerCharacter();
            foreach (CharacterEntity candidate in BehaviorTreeCharacterRegistry.Characters)
            {
                if (candidate == null || candidate == owner || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                yield return new BehaviorTreeTargetData(
                    candidate.transform.position,
                    candidate.name,
                    candidate.GetInstanceID(),
                    candidate == localPlayer,
                    GetFactionId(candidate, candidate == localPlayer));
            }
        }

        /// <summary>
        /// 获取 owner 的阵营 ID。
        /// </summary>
        private int GetOwnerFactionId()
        {
            CharacterEntity localPlayer = ResolveLocalPlayerCharacter();
            return GetFactionId(owner, owner == localPlayer);
        }

        /// <summary>
        /// 获取指定角色的阵营 ID。
        /// </summary>
        /// <param name="character">要查询的角色。</param>
        /// <param name="isPlayerControlled">是否为玩家控制角色。</param>
        /// <returns>阵营 ID。</returns>
        private static int GetFactionId(CharacterEntity character, bool isPlayerControlled)
        {
            if (BehaviorTreeCharacterRegistry.TryGetMetadata(character, out BehaviorTreeTargetMetadata metadata))
            {
                return metadata.FactionId;
            }

            return isPlayerControlled ? 1 : 0;
        }

        /// <summary>
        /// 兼容两套管理器来解析本地玩家角色。
        /// </summary>
        private static CharacterEntity ResolveLocalPlayerCharacter()
        {
            return PlayerManager.Instance?.LocalCharacter ?? Game.Logic.Character.CharcterManager.Instance?.LocalCharacter;
        }
    }

    /// <summary>
    /// 纯算法目标选择器，不依赖 MonoBehaviour 生命周期。
    /// </summary>
    public static class BehaviorTreeTargetSelector
    {
        /// <summary>
        /// 按给定参数从候选集合中选择一个目标。
        /// </summary>
        /// <param name="ownerPosition">owner 的世界坐标。</param>
        /// <param name="ownerForward">owner 的朝向。</param>
        /// <param name="ownerFactionId">owner 的阵营 ID。</param>
        /// <param name="options">目标选择参数。</param>
        /// <param name="candidates">候选目标集合。</param>
        /// <param name="currentTarget">当前已锁定的目标。</param>
        /// <param name="selectedTarget">输出的目标。</param>
        /// <returns>是否成功选择到目标。</returns>
        public static bool TrySelectTarget(
            Vector3 ownerPosition,
            Vector3 ownerForward,
            int ownerFactionId,
            BehaviorTreeTargetSelectionOptions options,
            IEnumerable<BehaviorTreeTargetData> candidates,
            BehaviorTreeTargetData? currentTarget,
            out BehaviorTreeTargetData selectedTarget)
        {
            List<(BehaviorTreeTargetData target, float sqrDistance)> allCandidates = candidates?
                .Select(target => (target, ComputeHorizontalSqrDistance(ownerPosition, target.Position)))
                .Where(pair => PassesFilters(ownerPosition, ownerForward, ownerFactionId, options, pair.target, pair.Item2))
                .ToList() ?? new List<(BehaviorTreeTargetData target, float sqrDistance)>();

            if (TryRetainCurrentTarget(ownerPosition, options, allCandidates, currentTarget, out selectedTarget))
            {
                return true;
            }

            List<(BehaviorTreeTargetData target, float sqrDistance)> validCandidates = allCandidates
                .Where(pair => IsWithinDistance(pair.Item2, options.MaxDistance))
                .ToList();

            if (validCandidates.Count == 0)
            {
                selectedTarget = default;
                return false;
            }

            selectedTarget = options.SelectionMode switch
            {
                BehaviorTreeTargetSelectionMode.LocalPlayerOnly => SelectLocalPlayer(validCandidates),
                BehaviorTreeTargetSelectionMode.LocalPlayerPreferred => SelectLocalPlayer(validCandidates, fallbackToClosest: true),
                _ => SelectClosest(validCandidates)
            };

            return selectedTarget.InstanceId != 0;
        }

        /// <summary>
        /// 尝试保留当前目标；保留成功时返回候选列表里的最新目标数据，而不是旧快照。
        /// </summary>
        /// <param name="ownerPosition">owner 的世界坐标。</param>
        /// <param name="options">目标选择参数。</param>
        /// <param name="allCandidates">已通过基础筛选的候选集合。</param>
        /// <param name="currentTarget">当前已锁定的目标。</param>
        /// <param name="selectedTarget">输出的目标。</param>
        /// <returns>是否成功保留当前目标。</returns>
        private static bool TryRetainCurrentTarget(
            Vector3 ownerPosition,
            BehaviorTreeTargetSelectionOptions options,
            IReadOnlyList<(BehaviorTreeTargetData target, float sqrDistance)> allCandidates,
            BehaviorTreeTargetData? currentTarget,
            out BehaviorTreeTargetData selectedTarget)
        {
            if (!options.RetainCurrentTarget || !currentTarget.HasValue)
            {
                selectedTarget = default;
                return false;
            }

            BehaviorTreeTargetData retainedTarget = currentTarget.Value;
            if (options.SelectionMode == BehaviorTreeTargetSelectionMode.LocalPlayerOnly && !retainedTarget.IsPlayerControlled)
            {
                selectedTarget = default;
                return false;
            }

            int retainedCandidateIndex = -1;
            for (int index = 0; index < allCandidates.Count; index++)
            {
                if (allCandidates[index].target.InstanceId == retainedTarget.InstanceId)
                {
                    retainedCandidateIndex = index;
                    break;
                }
            }

            if (retainedCandidateIndex < 0)
            {
                selectedTarget = default;
                return false;
            }

            (BehaviorTreeTargetData target, float sqrDistance) liveCandidate = allCandidates[retainedCandidateIndex];

            float retainDistance = options.MaxDistance;
            if (retainDistance > 0f && IsFiniteDistance(retainDistance))
            {
                retainDistance *= Mathf.Max(1f, options.RetainDistanceMultiplier);
            }

            if (!IsWithinDistance(liveCandidate.sqrDistance, retainDistance))
            {
                selectedTarget = default;
                return false;
            }

            selectedTarget = liveCandidate.target;
            return true;
        }

        /// <summary>
        /// 判断目标是否通过统一筛选规则。
        /// </summary>
        /// <param name="ownerPosition">owner 的世界坐标。</param>
        /// <param name="ownerForward">owner 的朝向。</param>
        /// <param name="ownerFactionId">owner 的阵营 ID。</param>
        /// <param name="options">目标选择参数。</param>
        /// <param name="target">待检测目标。</param>
        /// <param name="sqrDistance">预先计算好的水平距离平方。</param>
        /// <returns>是否通过。</returns>
        private static bool PassesFilters(
            Vector3 ownerPosition,
            Vector3 ownerForward,
            int ownerFactionId,
            BehaviorTreeTargetSelectionOptions options,
            BehaviorTreeTargetData target,
            float sqrDistance)
        {
            if (!PassesFactionFilter(ownerFactionId, options.FactionFilter, target.FactionId))
            {
                return false;
            }

            if (!PassesControlFilter(options.ControlFilter, target.IsPlayerControlled))
            {
                return false;
            }

            if (!PassesMinDistance(sqrDistance, options.MinDistance))
            {
                return false;
            }

            if (!PassesFieldOfView(ownerPosition, ownerForward, target.Position, options.FieldOfViewDegrees))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 从候选集合中选出最近目标。
        /// </summary>
        /// <param name="candidates">候选集合。</param>
        /// <returns>最近目标。</returns>
        private static BehaviorTreeTargetData SelectClosest(IEnumerable<(BehaviorTreeTargetData target, float sqrDistance)> candidates)
        {
            return candidates
                .OrderBy(candidate => candidate.sqrDistance)
                .Select(candidate => candidate.target)
                .FirstOrDefault();
        }

        /// <summary>
        /// 从候选集合中优先选择本地玩家。
        /// </summary>
        /// <param name="candidates">候选集合。</param>
        /// <param name="fallbackToClosest">找不到本地玩家时是否退化为最近目标。</param>
        /// <returns>选中的目标。</returns>
        private static BehaviorTreeTargetData SelectLocalPlayer(
            IEnumerable<(BehaviorTreeTargetData target, float sqrDistance)> candidates,
            bool fallbackToClosest = false)
        {
            BehaviorTreeTargetData localPlayerTarget = candidates
                .Where(candidate => candidate.target.IsPlayerControlled)
                .OrderBy(candidate => candidate.sqrDistance)
                .Select(candidate => candidate.target)
                .FirstOrDefault();

            if (localPlayerTarget.InstanceId != 0 || !fallbackToClosest)
            {
                return localPlayerTarget;
            }

            return SelectClosest(candidates);
        }

        /// <summary>
        /// 计算水平平面上的距离平方。
        /// </summary>
        /// <param name="source">起点。</param>
        /// <param name="target">终点。</param>
        /// <returns>水平距离平方。</returns>
        private static float ComputeHorizontalSqrDistance(Vector3 source, Vector3 target)
        {
            Vector2 sourceXZ = new Vector2(source.x, source.z);
            Vector2 targetXZ = new Vector2(target.x, target.z);
            return (targetXZ - sourceXZ).sqrMagnitude;
        }

        /// <summary>
        /// 判断距离是否在最大允许范围内。
        /// </summary>
        /// <param name="sqrDistance">距离平方。</param>
        /// <param name="maxDistance">最大距离。</param>
        /// <returns>是否在范围内。</returns>
        private static bool IsWithinDistance(float sqrDistance, float maxDistance)
        {
            if (!IsFiniteDistance(maxDistance) || maxDistance <= 0f)
            {
                return true;
            }

            return sqrDistance <= maxDistance * maxDistance;
        }

        /// <summary>
        /// 判断一个浮点数是否是可用的有限值。
        /// </summary>
        /// <param name="value">要检测的值。</param>
        /// <returns>是否是有效有限值。</returns>
        private static bool IsFiniteDistance(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// 判断是否满足最小距离限制。
        /// </summary>
        /// <param name="sqrDistance">距离平方。</param>
        /// <param name="minDistance">最小距离。</param>
        /// <returns>是否满足。</returns>
        private static bool PassesMinDistance(float sqrDistance, float minDistance)
        {
            if (!IsFiniteDistance(minDistance) || minDistance <= 0f)
            {
                return true;
            }

            return sqrDistance >= minDistance * minDistance;
        }

        /// <summary>
        /// 判断目标是否位于视野角内；360 或更大视为全向索敌。
        /// </summary>
        /// <param name="ownerPosition">owner 的世界坐标。</param>
        /// <param name="ownerForward">owner 的朝向。</param>
        /// <param name="targetPosition">目标坐标。</param>
        /// <param name="fieldOfViewDegrees">视野角。</param>
        /// <returns>是否通过 FOV 检测。</returns>
        private static bool PassesFieldOfView(
            Vector3 ownerPosition,
            Vector3 ownerForward,
            Vector3 targetPosition,
            float fieldOfViewDegrees)
        {
            if (!IsFiniteDistance(fieldOfViewDegrees) || fieldOfViewDegrees <= 0f || fieldOfViewDegrees >= 360f)
            {
                return true;
            }

            Vector2 forwardXZ = new Vector2(ownerForward.x, ownerForward.z);
            if (forwardXZ.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            Vector2 toTargetXZ = new Vector2(targetPosition.x - ownerPosition.x, targetPosition.z - ownerPosition.z);
            if (toTargetXZ.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            float angle = Vector2.Angle(forwardXZ.normalized, toTargetXZ.normalized);
            return angle <= fieldOfViewDegrees * 0.5f;
        }

        /// <summary>
        /// 判断目标是否通过阵营筛选。
        /// </summary>
        /// <param name="ownerFactionId">owner 阵营。</param>
        /// <param name="filter">阵营筛选方式。</param>
        /// <param name="targetFactionId">目标阵营。</param>
        /// <returns>是否通过。</returns>
        private static bool PassesFactionFilter(
            int ownerFactionId,
            BehaviorTreeTargetFactionFilter filter,
            int targetFactionId)
        {
            return filter switch
            {
                BehaviorTreeTargetFactionFilter.SameFaction => targetFactionId == ownerFactionId,
                BehaviorTreeTargetFactionFilter.DifferentFaction => targetFactionId != ownerFactionId,
                _ => true
            };
        }

        /// <summary>
        /// 判断目标是否通过玩家/非玩家筛选。
        /// </summary>
        /// <param name="filter">控制类型筛选方式。</param>
        /// <param name="isPlayerControlled">目标是否由玩家控制。</param>
        /// <returns>是否通过。</returns>
        private static bool PassesControlFilter(
            BehaviorTreeTargetControlFilter filter,
            bool isPlayerControlled)
        {
            return filter switch
            {
                BehaviorTreeTargetControlFilter.PlayerOnly => isPlayerControlled,
                BehaviorTreeTargetControlFilter.NonPlayerOnly => !isPlayerControlled,
                _ => true
            };
        }
    }
}
