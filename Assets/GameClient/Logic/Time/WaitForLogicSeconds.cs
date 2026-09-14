using UnityEngine;

namespace Game.Logic
{
    public class WaitForLogicSeconds : CustomYieldInstruction
    {
        private float _waitTime;
        
        public WaitForLogicSeconds(float seconds)
        {
            float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GameplayTime : Time.time;
            _waitTime = currentTime + seconds;
        }

        public override bool keepWaiting
        {
            get
            {
                float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GameplayTime : Time.time;
                return currentTime < _waitTime;
            }
        }
    }
}
