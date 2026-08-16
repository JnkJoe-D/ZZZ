using System;

namespace Game.Logic.AI.BehaviorTree
{
    public class Wait : LeafNode
    {
        private float _delay;
        private float _randomVariance;

        public Wait(float delay, float randomVariance = 0f)
        {
            _delay = delay;
            _randomVariance = randomVariance;
        }

        protected override void DoStart()
        {
            Tree.blackboard.Clock.AddTimer(_delay, _randomVariance, 0, OnTimer);
        }

        protected override void DoStop()
        {
            Tree.blackboard.Clock.RemoveTimer(OnTimer);
            Stopped(false);
        }

        private void OnTimer()
        {
            Stopped(true);
        }
    }
}
