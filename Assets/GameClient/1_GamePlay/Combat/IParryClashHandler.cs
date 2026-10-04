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
        public bool IsConsumed;
        public ParryPrecomputedData PrecomputedData;
    }

    public interface IParryClashHandler
    {
        void OnHitCaptured(ParryClashContext ctx);
    }
}
