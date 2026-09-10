using UnityEngine;

namespace Game.Logic
{
    public class WaitForGameSeconds : CustomYieldInstruction
    {
        private float _waitTime;
        
        public WaitForGameSeconds(float seconds)
        {
            _waitTime = TimeManager.Instance.UITime + seconds;
        }

        public override bool keepWaiting => TimeManager.Instance.UITime < _waitTime;
    }
}
