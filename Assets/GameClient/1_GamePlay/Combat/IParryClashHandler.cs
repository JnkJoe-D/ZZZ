using UnityEngine;
using ATEditor;

namespace Game.GamePlay
{
    public class ParryClashContext
    {
        public CharacterEntity Attacker;
        public CharacterEntity Victim;
        public AttackWarningMarker Marker;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public int ParryHitEffectId;
        public float HitStopDuration;
        public bool IsConsumed;
    }

    public interface IParryClashHandler
    {
        void OnHitCaptured(ParryClashContext ctx);
    }
}
