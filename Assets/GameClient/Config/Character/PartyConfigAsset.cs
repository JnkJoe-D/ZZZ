using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.Character.Config
{
    [CreateAssetMenu(fileName = "PartyConfigAsset", menuName = "Config/Role/Party Config")]
    public class PartyConfigAsset : ScriptableObject
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
        public Vector3 RightBackOffset = new Vector3(1.5f, 0f, -1.25f);
        public Vector3 BackOffset = new Vector3(0f, 0f, -1.6f);
        public Vector3 LeftBackOffset = new Vector3(-1.5f, 0f, -1.25f);
        public Vector3 FallbackOffset = Vector3.zero;

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

        public Vector3 GetSwitchOffset(int priorityIndex)
        {
            int index = Mathf.Abs(priorityIndex) % 4;
            return index switch
            {
                0 => RightBackOffset,
                1 => BackOffset,
                2 => LeftBackOffset,
                _ => FallbackOffset
            };
        }
    }
}
