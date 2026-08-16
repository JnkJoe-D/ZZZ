using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#d6f50c")]
    public class TraceCondition : LeafNode
{
    private string _name;
    private System.Func<bool> _conditionFunc;
    
    public TraceCondition(string name, System.Func<bool> conditionFunc) 
    { 
        _name = name; 
        _conditionFunc = conditionFunc; 
    }
    
    protected override void DoStart() 
    { 
        bool result = _conditionFunc != null && _conditionFunc();
        // 为了防止日志刷屏，每60帧采样打印一次（约1秒1次）
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[BT Trace] Evaluate '{_name}': {result}");
        }
        Stopped(result); 
    }
    
    protected override void DoStop() { }
}
}
