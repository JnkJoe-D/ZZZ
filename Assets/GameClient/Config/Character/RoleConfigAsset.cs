using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    [CreateAssetMenu(fileName = "RoleConfigAsset", menuName = "Config/Role/Role Config")]
    public class RoleConfigAsset : CharacterConfigAsset
    {
        [Header("UI Config")]
        public CharacterUIConfigAsset UIConfig;

        [Header("Ground Jog (Player)")]
        public float JogShortInputThreshold = 0.2f;

        [Header("Evade (Player)")]
        public int evadeLimitedTimes = 2;
        public float evadeCoolDown = 1f;

    }
}
