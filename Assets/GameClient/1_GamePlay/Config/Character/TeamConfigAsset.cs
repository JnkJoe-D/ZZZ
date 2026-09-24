using System.Collections.Generic;
using UnityEngine;
using Game.Framework;
namespace Game.GamePlay
{
    [CreateAssetMenu(fileName = "TeamConfigAsset", menuName = "Config/Role/Party Config")]
    public class TeamConfigAsset : GameConfigAsset
    {
        public const int MaxTeamCapacity = 3;

        [Header("Party (Fixed 3 Slots)")]
        [Tooltip("队伍固定为3个槽位。无论配置在哪个位置，运行时都会自动靠左对齐。")]
        public CharacterConfigAsset[] Members = new CharacterConfigAsset[MaxTeamCapacity];

        [Min(0)]
        public int InitialSlotIndex;

        [Header("Team")]
        public GameObject TeamPrefab;

        [Header("Camera")]
        public GameObject CameraPrefab;

        [Header("Switch In Offsets")]
        public List<Vector3> SwitchInOffset = new List<Vector3>();
        public LayerMask blockLayer;
        public float blockRadiusMultipier = 1.5f;

        [Header("Targeting")]
        public RoleTargetFinder.RoleTargetFinderCfg TargetSearchConfig = new RoleTargetFinder.RoleTargetFinderCfg();

        /// <summary>
        /// 获取靠左排序好的只读成员列表契约（长度恒为 3，空位为 null）。
        /// 外部/UI 获取后无需再做排序或移位处理。
        /// </summary>
        public IReadOnlyList<CharacterConfigAsset> OrderedMembers => GetOrderedMembers();

        /// <summary>
        /// 获取固定 3 槽位的靠左排序数组。
        /// 例如配置为 [null, null, RoleC] -> 返回 [RoleC, null, null]
        /// 例如配置为 [RoleA, null, RoleC] -> 返回 [RoleA, RoleC, null]
        /// </summary>
        public CharacterConfigAsset[] GetOrderedMembers()
        {
            CharacterConfigAsset[] results = new CharacterConfigAsset[MaxTeamCapacity];
            if (Members == null)
            {
                return results;
            }

            int writeIndex = 0;
            for (int i = 0; i < Members.Length && writeIndex < MaxTeamCapacity; i++)
            {
                if (Members[i] != null)
                {
                    results[writeIndex++] = Members[i];
                }
            }

            return results;
        }

        /// <summary>
        /// 获取当前队伍中有效配置的角色数量。
        /// </summary>
        public int GetValidMemberCount(int maxCount = MaxTeamCapacity)
        {
            int count = 0;
            if (Members == null)
            {
                return 0;
            }

            for (int i = 0; i < Members.Length && count < maxCount; i++)
            {
                if (Members[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 构建靠左排序紧凑的运行时角色配置列表（过滤掉 null 元素）。
        /// 返回给外部和队伍生成器，确保 1 号位永远为首个有效角色。
        /// </summary>
        public List<CharacterConfigAsset> BuildRuntimeMembers(int maxCount = MaxTeamCapacity)
        {
            CharacterConfigAsset[] ordered = GetOrderedMembers();
            List<CharacterConfigAsset> results = new List<CharacterConfigAsset>(maxCount);

            for (int i = 0; i < ordered.Length && results.Count < maxCount; i++)
            {
                if (ordered[i] != null)
                {
                    results.Add(ordered[i]);
                }
            }

            return results;
        }

        /// <summary>
        /// 上下文菜单：在编辑器中一键将配置的成员物理靠左对齐。
        /// </summary>
        [ContextMenu("Sort Members Left (靠左对齐)")]
        public void SortMembersLeft()
        {
            Members = GetOrderedMembers();
        }

        private void OnValidate()
        {
            // 确保容量始终恒定为 3，防止在 Inspector 中由于快捷操作产生扩容或缩容
            if (Members == null || Members.Length != MaxTeamCapacity)
            {
                CharacterConfigAsset[] newMembers = new CharacterConfigAsset[MaxTeamCapacity];
                if (Members != null)
                {
                    for (int i = 0; i < Mathf.Min(Members.Length, MaxTeamCapacity); i++)
                    {
                        newMembers[i] = Members[i];
                    }
                }
                Members = newMembers;
            }

            int validCount = GetValidMemberCount();
            if (validCount > 0)
            {
                InitialSlotIndex = Mathf.Clamp(InitialSlotIndex, 0, validCount - 1);
            }
            else
            {
                InitialSlotIndex = 0;
            }
        }
    }
}
