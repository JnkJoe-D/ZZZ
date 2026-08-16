using System;
using System.Collections.Generic;

namespace Game.Logic.AI.BehaviorTree
{
    public class Clock
    {
        private class Timer
        {
            public float ScheduledTime;
            public int Repeat;
            public float Delay;
            public float RandomVariance;
            public System.Action Action;
            public bool IsActive = true;
        }

        private readonly List<Timer> _timers = new List<Timer>();
        private readonly List<Timer> _addQueue = new List<Timer>();
        private readonly List<Timer> _removeQueue = new List<Timer>();
        private float _elapsedTime;

        public void AddTimer(float delay, float randomVariance, int repeat, System.Action action)
        {
            float scheduledTime = _elapsedTime + delay + (randomVariance > 0f ? (float)(new Random().NextDouble()) * randomVariance : 0f);
            
            var timer = new Timer
            {
                ScheduledTime = scheduledTime,
                Repeat = repeat,
                Delay = delay,
                RandomVariance = randomVariance,
                Action = action
            };
            _addQueue.Add(timer);
        }

        public void AddTimer(float delay, int repeat, System.Action action)
        {
            AddTimer(delay, 0f, repeat, action);
        }

        public void RemoveTimer(System.Action action)
        {
            foreach (var timer in _timers)
            {
                if (timer.Action == action)
                {
                    timer.IsActive = false;
                    _removeQueue.Add(timer);
                }
            }
            foreach (var timer in _addQueue)
            {
                if (timer.Action == action)
                {
                    timer.IsActive = false;
                }
            }
        }

        public bool HasTimer(System.Action action)
        {
            foreach (var timer in _timers)
            {
                if (timer.Action == action && timer.IsActive) return true;
            }
            foreach (var timer in _addQueue)
            {
                if (timer.Action == action && timer.IsActive) return true;
            }
            return false;
        }

        public void Update(float deltaTime)
        {
            _elapsedTime += deltaTime;

            if (_addQueue.Count > 0)
            {
                _timers.AddRange(_addQueue);
                _addQueue.Clear();
            }

            if (_removeQueue.Count > 0)
            {
                foreach (var r in _removeQueue)
                {
                    _timers.Remove(r);
                }
                _removeQueue.Clear();
            }

            for (int i = 0; i < _timers.Count; i++)
            {
                var timer = _timers[i];
                if (!timer.IsActive) continue;

                if (_elapsedTime >= timer.ScheduledTime)
                {
                    timer.Action?.Invoke();

                    if (timer.IsActive) // Action may have cancelled the timer
                    {
                        if (timer.Repeat > 0)
                        {
                            timer.Repeat--;
                        }
                        
                        if (timer.Repeat == 0)
                        {
                            timer.IsActive = false;
                            _removeQueue.Add(timer);
                        }
                        else
                        {
                            timer.ScheduledTime = _elapsedTime + timer.Delay + (timer.RandomVariance > 0f ? (float)(new Random().NextDouble()) * timer.RandomVariance : 0f);
                        }
                    }
                }
            }
        }
    }
}
