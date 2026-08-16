using System;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class BlackboardCondition : DecoratorNode
    {
        [HideInInspector] public string _key;
        [HideInInspector] public Operator _op;
        [HideInInspector] public object _value;
        [HideInInspector] public Stops _stops;
        protected bool _isConditionMet;

        public BlackboardCondition() {}

        public BlackboardCondition(string key, Operator op, object value, Stops stops, Node decoratee) : base(decoratee)
        {
            _key = key;
            _op = op;
            _value = value;
            _stops = stops;
        }

        public override void SetTree(BehaviorTreeAsset tree)
        {
            base.SetTree(tree);
            if (_stops != Stops.None)
            {
                Tree.blackboard.AddObserver(_key, OnBlackboardChanged);
            }
        }

        protected override void DoStart()
        {
            _isConditionMet = Evaluate();
            if (_isConditionMet)
            {
                base.DoStart();
            }
            else
            {
                Stopped(false);
            }
        }

        protected override void DoStop()
        {
            base.DoStop();
        }

        private void OnBlackboardChanged(Blackboard.Type type, object newValue)
        {
            bool wasMet = _isConditionMet;
            _isConditionMet = Evaluate();

            if (!wasMet && _isConditionMet && 
                (_stops == Stops.LowerPriority || _stops == Stops.Both))
            {
                // This means this condition was false, we weren't running decoratee, 
                // but now it's true and we want to interrupt lower priority branches.
                Node current = this;
                Container parent = ParentNode;
                while (parent != null)
                {
                    if (parent is Selector selector && selector.CurrentState == NodeState.Active)
                    {
                        selector.StopLowerPriorityChildrenForChild(current);
                        break;
                    }
                    current = parent;
                    parent = parent.ParentNode;
                }
            }
            else if (CurrentState == NodeState.Active && wasMet && !_isConditionMet && 
                     (_stops == Stops.Self || _stops == Stops.Both))
            {
                // We are active, but condition is no longer met. Abort self.
                Decoratee?.Stop();
            }
        }

        private bool Evaluate()
        {
            object bbValue = Tree.blackboard.Get(_key);
            
            if (_op == Operator.AlwaysTrue) return true;
            
            if (_op == Operator.IsEqual)
            {
                return (bbValue == null && _value == null) || (bbValue != null && bbValue.Equals(_value));
            }
            
            if (_op == Operator.IsNotEqual)
            {
                return !((bbValue == null && _value == null) || (bbValue != null && bbValue.Equals(_value)));
            }

            // Implement greater/smaller logic for numbers if needed...
            return false;
        }
    }
}
