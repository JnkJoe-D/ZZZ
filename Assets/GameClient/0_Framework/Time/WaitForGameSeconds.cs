using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 基于游戏受缩放时间（UITime）等待指定的物理秒数。
    /// 当游戏处于全局时停或缩放为 0 时会自动挂起等待，避免特效提前回收。
    /// </summary>
    public class WaitForGameSeconds : CustomYieldInstruction
    {
        private readonly float _waitTime;
        
        public WaitForGameSeconds(float seconds)
        {
            _waitTime = FrameworkTimeManager.Instance.UITime + seconds;
        }

        public override bool keepWaiting => FrameworkTimeManager.Instance.UITime < _waitTime;
    }
}
