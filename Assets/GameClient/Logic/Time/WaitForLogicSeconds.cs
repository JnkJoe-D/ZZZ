using UnityEngine;

namespace Game.Logic
{
    public class WaitForLogicSeconds : CustomYieldInstruction
    {
        private float _waitTime;
        
        public WaitForLogicSeconds(float seconds)
        {
            _waitTime = TimeManager.Instance.GameplayTime + seconds;
        }

        public override bool keepWaiting => TimeManager.Instance.GameplayTime < _waitTime;
    }
}
