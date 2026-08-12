using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    [CreateAssetMenu(fileName = "TeamConfigAsset", menuName = "Config/Role/Party Config")]
    public class TeamConfigAsset : GameConfigAsset
    {
        [Header("Party")]
        public List<CharacterConfigAsset> Members = new List<CharacterConfigAsset>(3);

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

        public int GetValidMemberCount(int maxCount = 3)
        {
            int count = 0;
            if (Members == null)
            {
                return 0;
            }

            for (int i = 0; i < Members.Count && count < maxCount; i++)
            {
                if (Members[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        public List<CharacterConfigAsset> BuildRuntimeMembers(int maxCount = 3)
        {
            List<CharacterConfigAsset> results = new List<CharacterConfigAsset>(maxCount);
            if (Members == null)
            {
                return results;
            }

            for (int i = 0; i < Members.Count && results.Count < maxCount; i++)
            {
                if (Members[i] != null)
                {
                    results.Add(Members[i]);
                }
            }

            return results;
        }
    }
}
