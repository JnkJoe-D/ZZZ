using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
[NodeColor("#88f50c")]
public class AdjustDistanceNode : MonsterTaskNode
{
    public MonsterActionConfigAsset WalkForward;
    public MonsterActionConfigAsset WalkBackward;
    public float Tolerance;
    public float OptimalAttackDistance = 1.0f;
    
    // 可视化编辑器暴露字段，默认使用智能拉扯
    public DistanceAdjustStrategy Strategy = DistanceAdjustStrategy.SmartRetreat;
    public float MaxRetreatTime = 1.5f;
    
    private enum MoveState { None, Forward, Backward }
    private MoveState _moveState = MoveState.None;
    private float _retreatStartTime = 0f;
    private float _lastDist = -1f;
    
        public AdjustDistanceNode() {}
        public AdjustDistanceNode(AIContext context, MonsterActionConfigAsset walkForward, MonsterActionConfigAsset walkBackward, float tolerance, float optimalDistance) : base(context)
        {
            WalkForward = walkForward;
            WalkBackward = walkBackward;
            Tolerance = tolerance;
            OptimalAttackDistance = optimalDistance;
        }
    
    protected override void DoStart()
    {
        _moveState = MoveState.None;
        _lastDist = -1f;
        Tree.blackboard.Clock.AddTimer(0.1f, 0, -1, CheckDistance);
        CheckDistance();
    }
    
    private void CheckDistance()
    {
        if (CurrentState != NodeState.Active) return;
        
        if (!Tree.blackboard.Get<bool>("HasTarget"))
        {
            Stopped(false);
            return;
        }
        
        float dist = Tree.blackboard.Get<float>("Distance");
        float optimal = OptimalAttackDistance;
        
        bool shouldAttack = false;
        
        // 判定是否可以直接攻击
        if (Strategy == DistanceAdjustStrategy.InsideAllowed && dist <= optimal + Tolerance)
        {
            shouldAttack = true; // 圆内或圆周，直接攻击
        }
        else if (Mathf.Abs(dist - optimal) <= Tolerance)
        {
            shouldAttack = true; // 严格在圆周上
        }
        else if (_lastDist > 0)
        {
            // 防止因为移动速度过快、Tolerance过小导致一步跨过最佳距离产生的“前后抽搐”
            if (_moveState == MoveState.Forward && _lastDist >= optimal && dist <= optimal)
                shouldAttack = true;
            else if (_moveState == MoveState.Backward && _lastDist <= optimal && dist >= optimal)
                shouldAttack = true;
        }

        if (Strategy == DistanceAdjustStrategy.SmartRetreat && _moveState == MoveState.Backward && !shouldAttack)
        {
            if (Time.time - _retreatStartTime >= MaxRetreatTime)
            {
                shouldAttack = true; // 后退超时，强制攻击
            }
        }
        
        _lastDist = dist;

        if (shouldAttack)
        {
            Debug.Log($"[BT Adjust] Distance condition met (dist:{dist}, optimal:{optimal}, strategy:{Strategy}). Proceeding to attack.");
            
            // 必须在主动完成时清理定时器和动作
            Tree.blackboard.Clock.RemoveTimer(CheckDistance);
            if (_moveState != MoveState.None && Context.Owner != null && Context.Owner.ActionPlayer != null)
            {
                Context.Owner.ActionPlayer.StopAction();
            }
            _moveState = MoveState.None;
            
            Stopped(true);
            return;
        }
        
        // 如果不能攻击，则动态决定该前进还是后退 (通用于所有策略的动态打断和切换)
        bool needsForward = dist > optimal + Tolerance;
        
        if (needsForward)
        {
            if (_moveState != MoveState.Forward)
            {
                Debug.Log($"[BT Adjust] Switching to Forward Walk. (dist:{dist} > optimal:{optimal})");
                PlayMovement(WalkForward);
                _moveState = MoveState.Forward;
            }
        }
        else
        {
            if (_moveState != MoveState.Backward)
            {
                Debug.Log($"[BT Adjust] Switching to Backward Walk. (dist:{dist} < optimal:{optimal})");
                PlayMovement(WalkBackward);
                _moveState = MoveState.Backward;
                _retreatStartTime = Time.time;
            }
        }
    }
    
    private void PlayMovement(MonsterActionConfigAsset action)
    {
        if (Context.Owner != null && Context.Owner.ActionPlayer != null)
        {
            if (_moveState != MoveState.None)
            {
                Context.Owner.ActionPlayer.StopAction();
            }
            if (action != null)
            {
                Context.Owner.ActionPlayer.PlayAction(action);
            }
        }
    }
    
    protected override void DoStop()
    {
        Tree.blackboard.Clock.RemoveTimer(CheckDistance);
        if (_moveState != MoveState.None && Context.Owner != null && Context.Owner.ActionPlayer != null)
        {
            Context.Owner.ActionPlayer.StopAction();
        }
        _moveState = MoveState.None;
        Stopped(false);
    }
}

        public enum DistanceAdjustStrategy
        {
            Strict,         // 严格圆周
            InsideAllowed,  // 圆内直接攻击
            SmartRetreat    // 智能拉扯
        }
}
