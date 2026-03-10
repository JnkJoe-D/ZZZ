using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 处理起跳瞬间与悬空抛物线下落时的基础状态
    /// （在此状态下不允许使用普通攻击，可能允许特定的 AirDash 冲刺）
    /// </summary>
    public class CharacterAirborneState : CharacterStateBase
    {
        // 假设的滞空时间，暂时代替真实的射线落地检测
        private float _dummyFallTimer;

        public override void OnEnter()
        {
 
        }

        public override void OnUpdate(float deltaTime)
        {
             
        }
    }
}
