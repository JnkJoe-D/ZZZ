using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Game.Logic.Character;

namespace Game.Logic.Action.Combo
{
    public enum ComboTriggerMode
    {
        Buffered,
        InstantOnly,
        BufferedAndInstant
    }
    [Serializable]
    public class ComboTransition
    {
        [Header("触发指令")]
        [Tooltip("触发该派生所必须缓存的输入指令")]
        public BufferedInputType RequiredCommand;
        [Header("跳转目标")]
        [Tooltip("验证通过后，将要播放的下一个行为")]
        [FormerlySerializedAs("NextSkill")]
        public Config.ActionConfigSO NextAction;

        [Header("业务前置条件 (选填)")]
        [SerializeReference] 
        public List<ITransitionCondition> ExtraConditions = new List<ITransitionCondition>();

        [Header("触发区间标签")]
        [Tooltip("触发必须处于该 Timeline 标签窗口内 (配合 ComboWindowTrack 使用)")]
        public string RequiredWindowTag = "Normal";

        [Tooltip("触发模式：\n1. BufferedAndInstant：吃预输入（默认）\n2. InstantOnly：不吃预输入，必须在窗口内部现按")]
        public ComboTriggerMode TriggerMode = ComboTriggerMode.Buffered;

        /// <summary>
        /// 评估连段条件是否成立
        /// </summary>
        public bool Evaluate(BufferedInputType currentInput, string tag, bool isBuffered = false)
        {
            // 第一层：校验指令指纹是否对得上
            if (currentInput == BufferedInputType.None || currentInput != RequiredCommand)
            {
                return false;
            }

            // 拦截预输入
            if (isBuffered && TriggerMode == ComboTriggerMode.InstantOnly)
            {
                return false;
            }

            // 第二层：校验区间标签限制（若配置了专属区间的话）
            if (!string.IsNullOrEmpty(RequiredWindowTag))
            {
                if (tag!=RequiredWindowTag)
                {
                    return false; // 当前没有处于对应的连段窗口内
                }
            }

            // 第三层：校验业务特异化条件 (如大招是否充满、能量是否够砍强化重击)
            if (ExtraConditions != null)
            {
                foreach (var condition in ExtraConditions)
                {
                    // if (condition != null && !condition.Check(actor)) 
                    // {
                    //     return false;
                    // }
                }
            }

            return true;
        }
    }
}
