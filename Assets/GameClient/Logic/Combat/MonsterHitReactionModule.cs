using UnityEngine;
using Game.Framework;

namespace Game.Logic
{
    public class MonsterHitReactionModule : HitReactionModule
    {
        public event System.Action OnHitTimestampChanged;

        protected override void OnInterrupted(HitContext ctx)
        {

            if (_hitData != null)
            {
                _hitData.HitTriggerTimestamp = Time.frameCount;
                OnHitTimestampChanged?.Invoke();
            }
        }
    }
}
