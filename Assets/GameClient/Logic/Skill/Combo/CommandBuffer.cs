using System.Collections.Generic;
using UnityEngine;
using Game.Logic.Action.Config;
using Game.Logic.Character; // Assuming BufferedInputType is here or in its own file

namespace Game.Logic.Action.Combo
{
    public class CharacterCommand
    {
        public BufferedInputType InputType;
        public float Timestamp; //时间戳
        public bool IsConsumed; //是否已核销
    }

    /// <summary>
    /// 指令缓冲池：纯粹的数据中立黑盒，用于按时间戳记录玩家的所有输入
    /// </summary>
    public class CommandBuffer
    {
        private List<CharacterCommand> _commands = new List<CharacterCommand>();
        private const float _expirationTime = 0.3f;

        public void Push(BufferedInputType inputType)
        {
            _commands.Add(new CharacterCommand
            {
                InputType = inputType,
                Timestamp = Time.time,
                IsConsumed = false
            });
        }

        public void Tick()
        {
            float currentTime = Time.time;
            // 自动清理过期指令
            _commands.RemoveAll(cmd => currentTime - cmd.Timestamp > _expirationTime || cmd.IsConsumed);
        }

        /// <summary>
        /// 提供所有未消费的有效指令迭代
        /// </summary>
        public IEnumerable<CharacterCommand> GetUnconsumedCommands()
        {
            foreach (var cmd in _commands)
            {
                if (!cmd.IsConsumed)
                {
                    yield return cmd;
                }
            }
        }

        public void Clear()
        {
            _commands.Clear();
        }
        
        public bool HasUnconsumedCommand()
        {
            foreach (var cmd in _commands)
            {
                if (!cmd.IsConsumed) return true;
            }
            return false;
        }
    }
}
