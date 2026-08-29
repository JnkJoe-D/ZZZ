using UnityEngine;
using Game.Logic;
using ATEditor;

namespace Game.Logic
{
    /// <summary>
    /// 处理招架与极限支援的跨空间瞬移逻辑
    /// 由 ATEditor 动作时间轴中的 AssistTeleportClip 在特定帧触发
    /// </summary>
    public class ATAssistHandler : IAssistHandler
    {
        private readonly CharacterEntity _entity;

        public ATAssistHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void ExecuteAssistTeleport()
        {
            if (_entity == null) return;
            
            var actionData = _entity.DataModule?.Get<ActionRuntimeData>();
            if (actionData == null) return;

            var marker = actionData.MatchedWarningMarker;
            if (marker == null || marker.Attacker == null) return;

            // 计算朝向攻击者的方向，忽略 Y 轴高度差
            Vector3 dirToAttacker = (marker.Attacker.transform.position - _entity.transform.position).normalized;
            dirToAttacker.y = 0;
            
            // 目标距离 = 怪物半径 + 合理的攻击身位(如 1.5f)
            float targetDistance = marker.Attacker.GetCharcterRadius() + 1.5f; 
            
            Vector3 attackerPos = marker.Attacker.transform.position;
            attackerPos.y = _entity.transform.position.y; // 保持玩家的当前高度
            
            Vector3 teleportPos = attackerPos - dirToAttacker * targetDistance;
            
            // 使用 CharacterMotor 完成跨空间移动和面朝修正
            _entity.CharacterMotor.Move(teleportPos - _entity.transform.position); 
            _entity.CharacterMotor.FaceToTargetImmediately(marker.Attacker.transform);
            
            // 消费掉，防止后续重复读取
            actionData.MatchedWarningMarker = null;
        }
    }
}
