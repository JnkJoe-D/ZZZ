using System.Collections.Generic;
using Game.FSM;
using Game.Logic.Character;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Action.Combo
{
    public class ComboController:ISkillComboWindowHandler
    {
        public struct ExecutionRecord
        {
            public BufferedInputType Input;
            public int ActionId;
            public float Timestamp;
        }
        public class ComboWindowData
        {
            public string Tag;
            public ComboWindowType Type;
        }
        private CharacterEntity _entity;
        public ComboController(CharacterEntity entity)
        {
            _entity = entity;
        }

        // 临界保护标志：防止在状态切换期间（触发 OnExit/StopAction）二次验算引发死循环和乱跳连段
        private bool _isTransitioning = false;
        
        private List<ComboWindowData> activeComboWindows  = new List<ComboWindowData>();

        public List<ExecutionRecord> ExecutionHistory { get; private set; } = new List<ExecutionRecord>();

        private void RecordExecution(BufferedInputType input, object action)
        {
            int actionId = action != null ? (action as Config.ActionConfigAsset).ID : -1;
            ExecutionHistory.Insert(0, new ExecutionRecord { Input = input, ActionId = actionId, Timestamp = Time.time });
            if (ExecutionHistory.Count > 10) ExecutionHistory.RemoveAt(10);
        }

        public void Update(float deltaTime)
        {
            // Fallback 期间的打断逻辑现在移到了 CharacterActionBackswingState 内部处理，
            // 保持这里的 Update 纯粹。
            if (_entity.CommandBuffer != null)
            {
                _entity.CommandBuffer.Tick();
            }
        }

        /// <summary>
        /// 统一的接收录入入口。所有状态（特别是技能期）只需调用此方法推入按键。
        /// </summary>
        public void OnInput(BufferedInputType inputType)
        {
            if (_entity.CommandBuffer == null) return;
            
            _entity.CommandBuffer.Push(inputType);

            // 如果当前在放技能、闪避或后摇期，立刻进行一次评估
            if (_entity.Machine.CurrentState is CharacterSkillState || 
                _entity.Machine.CurrentState is CharacterEvadeState ||
                _entity.Machine.CurrentState is CharacterActionBackswingState)
            {
                EvaluateCurrentState();
            }
        }

        // === 处理技能时间轴上的窗口生命周期 ===
        private void OnWindowEnter(string comboTag, ComboWindowType windowType)
        {
            activeComboWindows.Add(new ComboWindowData { Tag = comboTag, Type = windowType });
            if (windowType == ComboWindowType.Execute)
            {
                EvaluateTransitionsAgainst(comboTag);
            }
            else if (windowType == ComboWindowType.Buffer)
            {
                // 手感优化：在进入 Buffer预输入窗口的一瞬间，强制清空之前残留的陈旧指令。
                // 确保只有在这个 Buffer窗口开启【之后】按下的按键，才会被承认作为下一次的预输入。
                // 彻底杜绝“玩家在很早之前乱按的键，到现在仍未过期并被这扇窗户错误捕获”的极大延迟感。
                _entity.CommandBuffer.Clear();
            }
            else if (windowType == ComboWindowType.Fallback)
            {
                // 进入后摇窗口，切入空白等待态
                _entity.Machine.ChangeState<CharacterActionBackswingState>();
            }
        }

        private void OnWindowExit(string comboTag, ComboWindowType windowType)
        {
            activeComboWindows.RemoveAll(x => x.Tag == comboTag && x.Type == windowType);
            if (windowType == ComboWindowType.Buffer)
            {
                // Buffer期结束时，作为一个重要的“结算点”，以该 Buffer 自身的 Tag 作为条件校验一次连段
                EvaluateTransitionsAgainst(comboTag);
            }
            else if (windowType == ComboWindowType.Fallback)
            {
                // 后摇窗口自然结束，回归待机
                if (_entity.Machine.CurrentState is CharacterActionBackswingState)
                {
                    if (_entity.MovementController != null && _entity.MovementController.IsGrounded)
                        _entity.Machine.ChangeState<CharacterGroundState>();
                    else
                        _entity.Machine.ChangeState<CharacterAirborneState>();
                }
            }
        }

        /// <summary>
        /// 核心规则：根据当前的窗口类型，裁决是否消耗指令池中的指令
        /// </summary>
        private void EvaluateCurrentState()
        {
            if (_isTransitioning) return;
            if (_entity.CommandBuffer == null) return;
            if (activeComboWindows.Count == 0) return;

            // 进入后摇期间，打断逻辑由 CharacterActionBackswingState.OnUpdate 处理。
            // 连段跳转逻辑统一通过 EvaluateTransitionsAgainst 遍历处理。

            var currentSkill = _entity.RuntimeData.NextActionToCast as Config.SkillConfigAsset;
            if (currentSkill == null || currentSkill.OutTransitions == null) return;

            // 为了支持【同一时间存在多个并行的窗口轨道】（例如攻击轨道和闪避轨道重叠），
            // 将遍历层级倒置：优先遍历时间线上最老的【玩家指令】，再去用各并发窗口检测，确保“更早按的键”拥有跨轨道的绝对优先结算权。
            foreach (var cmd in _entity.CommandBuffer.GetUnconsumedCommands())
            {
                // 【长按延期判定】
                if (cmd.InputType == BufferedInputType.BasicAttack)
                {
                    if (_entity.Machine.CurrentState is CharacterSkillState skillState && skillState.IsBasicAttackHold)
                    {
                        bool hasHoldTransition = currentSkill.OutTransitions.Exists(t => t.RequiredCommand == BufferedInputType.BasicAttackHold);
                        if (hasHoldTransition)
                        {
                            continue; // 延期这记单击，循环看下一条指令
                        }
                    }
                }

                // 根据指令在池子里存活的时间，准确区分它是此帧立刻按下的即时指令，还是上一帧/更早的预输入指令
                bool isBuffered = (Time.time - cmd.Timestamp) > 0f;

                foreach (var window in activeComboWindows)
                {
                    if (window.Type != ComboWindowType.Execute) continue;

                    foreach (var transition in currentSkill.OutTransitions)
                    {
                        // 将真实的 isBuffered 传入，由 Transition 辨别是否允许预输入
                        if (transition.Evaluate(cmd.InputType, window.Tag, isBuffered)) 
                        {
                            // 匹配成功！
                            _isTransitioning = true;
                            cmd.IsConsumed = true;

                            // 判定闪避动作
                            if (cmd.InputType == BufferedInputType.EvadeFront)
                            {
                                _entity.RuntimeData.NextActionToCast = _entity.Config.evadeFront[0];
                            }
                            else if (cmd.InputType == BufferedInputType.EvadeBack)
                            {
                                _entity.RuntimeData.NextActionToCast = _entity.Config.evadeBack[0];
                            }
                            else
                            {
                                _entity.RuntimeData.NextActionToCast = transition.NextAction;
                            }
                            
                            RecordExecution(cmd.InputType, _entity.RuntimeData.NextActionToCast);
                            
                            // 必须全清当前技能留下的窗口！
                            activeComboWindows.Clear();
                            _entity.CommandBuffer.Clear();

                            // 通过调用 FSM 的流转，会自动执行旧状态 OnExit(停顿/清理) 和进入新状态 OnEnter(播新技能)
                            var nextSkill = _entity.RuntimeData.NextActionToCast as Config.SkillConfigAsset;
                            if (nextSkill != null && nextSkill.Category == Config.SkillCategory.Evade)
                                _entity.Machine.ChangeState<CharacterEvadeState>();
                            else
                                _entity.Machine.ChangeState<CharacterSkillState>();
                                
                            _isTransitioning = false;
                            return; 
                        }
                    }
                }
            }
        }



        private void EvaluateTransitionsAgainst(string tagToTest)
        {
            if (_isTransitioning) return;
            if (_entity.CommandBuffer == null) return;

            var currentSkill = _entity.RuntimeData.NextActionToCast as Config.SkillConfigAsset;
            if (currentSkill == null || currentSkill.OutTransitions == null) return;

            foreach (var cmd in _entity.CommandBuffer.GetUnconsumedCommands())
            {
                // 【长按延期判定】
                if (cmd.InputType == BufferedInputType.BasicAttack)
                {
                    if (_entity.Machine.CurrentState is CharacterSkillState skillState && skillState.IsBasicAttackHold)
                    {
                        bool hasHoldTransition = currentSkill.OutTransitions.Exists(t => t.RequiredCommand == BufferedInputType.BasicAttackHold);
                        if (hasHoldTransition)
                        {
                            continue; // 延期这记单击，循环看下一条指令
                        }
                    }
                }

                // 根据指令在池子里存活的时间，准确区分它是此帧立刻按下的即时指令，还是上一帧/更早的预输入指令
                bool isBuffered = (Time.time - cmd.Timestamp) > 0f;

                foreach (var transition in currentSkill.OutTransitions)
                {
                    // 将真实的 isBuffered 传入，由 Transition 辨别是否允许预输入
                    if (transition.Evaluate(cmd.InputType, tagToTest, isBuffered)) 
                    {
                        // 匹配成功！
                        _isTransitioning = true;
                        cmd.IsConsumed = true;

                        // 判定闪避动作
                        if (cmd.InputType == BufferedInputType.EvadeFront)
                        {
                            _entity.RuntimeData.NextActionToCast = _entity.Config.evadeFront[0];
                        }
                        else if (cmd.InputType == BufferedInputType.EvadeBack)
                        {
                            _entity.RuntimeData.NextActionToCast = _entity.Config.evadeBack[0];
                        }
                        else
                        {
                            _entity.RuntimeData.NextActionToCast = transition.NextAction;
                        }

                        RecordExecution(cmd.InputType, _entity.RuntimeData.NextActionToCast);

                        // 必须全清当前技能留下的窗口！
                        activeComboWindows.Clear();
                        _entity.CommandBuffer.Clear();

                        // 通过调用 FSM 的流转，会自动执行旧状态 OnExit(停顿/清理) 和进入新状态 OnEnter(播新技能)
                        var nextSkill = _entity.RuntimeData.NextActionToCast as Config.SkillConfigAsset;
                        if (nextSkill != null && nextSkill.Category == Config.SkillCategory.Evade)
                            _entity.Machine.ChangeState<CharacterEvadeState>();
                        else
                            _entity.Machine.ChangeState<CharacterSkillState>();

                        _isTransitioning = false;
                        return; 
                    }
                }
            }
        }

        private void InterruptToGround()
        {
            if (_entity.MovementController != null && _entity.MovementController.IsGrounded)
            {
                _entity.Machine.ChangeState<CharacterGroundState>();
            }
            else
            {
                _entity.Machine.ChangeState<CharacterAirborneState>();
            }
        }
        public void OnComboWindowEnter(string comboTag, SkillEditor.ComboWindowType windowType)
        {
            OnWindowEnter(comboTag, windowType);
        }

        public void OnComboWindowExit(string comboTag, SkillEditor.ComboWindowType windowType)
        {
            OnWindowExit(comboTag, windowType);
        }
    }
}
