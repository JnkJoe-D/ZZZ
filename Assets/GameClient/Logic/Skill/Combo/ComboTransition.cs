using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Game.Logic.Character;

namespace Game.Logic.Action.Combo
{
    [Serializable]
    public class ComboTransition
    {
        [Header("触发指令")]
        [Tooltip("触发该派生所必须缓存的预输入指令")]
        public BufferedInputType RequiredCommand;

        [Header("跳转目标")]
        [Tooltip("验证通过后，将要播放的下一个行为")]
        [FormerlySerializedAs("NextSkill")]
        public Config.ActionConfigSO NextAction;

        [Header("业务前置条件 (选填)")]
        [SerializeReference] 
        public List<ITransitionCondition> ExtraConditions = new List<ITransitionCondition>();

        /// <summary>
        /// 执行核心校验逻辑：由事件窗口回调触发
        /// </summary>
        public bool Evaluate(BufferedInputType bufferedCommand, CharacterEntity actor)
        {
            // 第一层：校验指令指纹是否对得上
            if (bufferedCommand != RequiredCommand)
            {
                return false;
            }

            // 第二层：校验业务特异化条件 (如大招是否充满、能量是否够砍强化重击)
            if (ExtraConditions != null)
            {
                foreach (var condition in ExtraConditions)
                {
                    if (condition != null && !condition.Check(actor)) 
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
