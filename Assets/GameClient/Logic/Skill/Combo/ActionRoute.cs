using System;
using System.Collections.Generic;
using Game.Logic.Action.Config;
using Game.Logic.Character;
using UnityEngine;

namespace Game.Logic.Action.Combo
{
    /// <summary>
    /// 路由触发源类别。
    /// </summary>
    public enum RouteTriggerCategory
    {
        /// <summary>玩家输入指令（BasicAttack.Started 等），需要 ComboWindow 配合。</summary>
        PlayerCommand = 0,
        /// <summary>动作生命周期事件（动作播放完毕），在 OnComplete 时评估。</summary>
        ActionLifecycle = 10
    }

    /// <summary>
    /// 修饰触发源类别。
    /// 当路由需要两个条件同时满足时，使用 Modifier 表示第二触发源。
    /// 类似 InputSystem 的 OneModifierComposite。
    /// </summary>
    public enum ModifierCategory
    {
        /// <summary>无修饰触发源，路由仅需主触发源即可执行。</summary>
        None = 0,
        /// <summary>修饰触发源是一个玩家指令（需在 ComboWindow 内）。</summary>
        PlayerCommand = 10,
        /// <summary>修饰触发源是一组 ITransitionCondition。</summary>
        Condition = 20
    }

    /// <summary>
    /// 速度倍率类型，决定使用 CharacterConfigAsset 中的哪个倍率字段。
    /// </summary>
    public enum SpeedMultiplierType
    {
        None = 0,
        Jog = 10,
        Dash = 20,
        Dodge = 30,
        Attack = 40,
        Skill = 50
    }

    /// <summary>
    /// 当修饰触发源 (Modifier) 不满足时的处理策略。
    /// </summary>
    public enum ModifierNonSatisfiedPolicy
    {
        /// <summary>立即向下寻找其他路由 (不拦截指令)。适合 Condition 类型判定。</summary>
        FallThrough = 0,
        /// <summary>暂存指令并等待条件满足 (拦截指令)。适合组合按键判定。</summary>
        Pending = 10
    }

    /// <summary>
    /// 统一的动作路由。
    /// 支持两种触发源：玩家指令、动作生命周期。
    /// 可通过 Modifier 配置组合触发源（两个条件同时满足才触发）。
    /// </summary>
    [Serializable]
    public class ActionRoute
    {
        [Header("主触发源")]
        public RouteTriggerCategory Category;

        // ── PlayerCommand 专用字段 ──────────────────────────────
        [Header("玩家指令触发（仅 PlayerCommand 时有效）")]
        public InputCommand RequiredType;
        public CommandPhase RequiredPhase = CommandPhase.Started;

        [Tooltip("只有匹配此 Tag 的 ComboWindow 激活时才能触发。")]
        public string RequiredWindowTag = "Normal";

        public ComboTriggerMode TriggerMode = ComboTriggerMode.Buffered;

        // ── 修饰触发源（组合触发源） ─────────────────────────────
        [Header("修饰触发源（组合触发源）")]
        [Tooltip("None=单触发源；PlayerCommand/Condition=需两个触发源同时满足。")]
        public ModifierCategory Modifier = ModifierCategory.None;

        [Header("Modifier 指令（仅 Modifier=PlayerCommand 时有效）")]
        public InputCommand ModifierRequiredType;
        public CommandPhase ModifierRequiredPhase = CommandPhase.Started;

        [Header("Modifier 条件（仅 Modifier=Condition 时有效）")]
        public List<ConditionCommand> ModifierConditions = new();

        [Header("Modifier 未满足时的策略")]
        [Tooltip("FallThrough: 立即向下寻找其他路由；Pending: 暂存并等待条件满足。")]
        public ModifierNonSatisfiedPolicy NonSatisfiedPolicy = ModifierNonSatisfiedPolicy.FallThrough;

        // ── 共用字段 ────────────────────────────────────────────
        [Header("目标动作")]
        [Tooltip("留空时由 CommandActionResolver 动态决定。")]
        public ActionConfigAsset NextAction;

        [Header("额外条件")]
        [SerializeReference]
        public List<ITransitionCondition> ExtraConditions = new();

        [Header("执行")]
        [Tooltip("高优先级路由在同一触发源的多个匹配中优先。同优先级取最新输入。")]
        public int Priority = 0;

        // ── 便捷属性 ────────────────────────────────────────────

        /// <summary>是否配置了修饰触发源。</summary>
        public bool HasModifier => Modifier != ModifierCategory.None;

        // ── 评估方法 ────────────────────────────────────────────

        /// <summary>
        /// 评估 PlayerCommand 类型路由的主触发源。
        /// 仅检查主指令 + 窗口 + TriggerMode + ExtraConditions。
        /// Modifier 由 Controller 单独评估。
        /// </summary>
        public bool EvaluatePlayerCommand(
            CharacterCommand command,
            string activeWindowTag,
            bool isBuffered,
            CharacterEntity actor)
        {
            if (Category != RouteTriggerCategory.PlayerCommand)
                return false;

            if (!CommandRouteEvaluator.MatchesCommand(RequiredType, RequiredPhase, command))
                return false;

            if (!CommandRouteEvaluator.MatchesTriggerMode(TriggerMode, isBuffered))
                return false;

            if (!string.IsNullOrEmpty(RequiredWindowTag) && activeWindowTag != RequiredWindowTag)
                return false;

            return CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
        }

        /// <summary>
        /// 评估修饰触发源是否满足。
        /// </summary>
        /// <param name="actor">角色实体（用于条件检查）</param>
        /// <param name="commandBuffer">指令缓冲区（用于 PlayerCommand 类型 Modifier 的指令匹配）</param>
        /// <param name="activeWindowTag">当前激活窗口 Tag（PlayerCommand Modifier 需要窗口约束）</param>
        /// <returns>Modifier 条件是否满足</returns>
        public bool EvaluateModifier(CharacterEntity actor, CommandBuffer commandBuffer = null, string activeWindowTag = null)
        {
            switch (Modifier)
            {
                case ModifierCategory.None:
                    return true;

                case ModifierCategory.Condition:
                    return EvaluateModifierConditions(actor);

                case ModifierCategory.PlayerCommand:
                    if (commandBuffer == null) return false;
                    foreach (var cmd in commandBuffer.GetUnconsumedCommands())
                    {
                        if (CommandRouteEvaluator.MatchesCommand(ModifierRequiredType, ModifierRequiredPhase, cmd))
                        {
                            // Modifier 指令也需要在 ComboWindow 内
                            if (string.IsNullOrEmpty(activeWindowTag) || activeWindowTag == RequiredWindowTag)
                                return true;
                        }
                    }
                    return false;

                default:
                    return true;
            }
        }

        private bool EvaluateModifierConditions(CharacterEntity actor)
        {
            if (ModifierConditions == null || ModifierConditions.Count == 0) return true;
            foreach (var cond in ModifierConditions)
            {
                if (!CheckCondition(cond, actor)) return false;
            }
            return true;
        }

        private bool CheckCondition(ConditionCommand cond, CharacterEntity actor)
        {
            if (actor == null) return false;

            switch (cond)
            {
                case ConditionCommand.None:
                    return true;
                case ConditionCommand.Move:
                    return actor.InputProvider != null && actor.InputProvider.HasMovementInput();
                case ConditionCommand.ShortMove:
                    return actor.RuntimeData != null && actor.RuntimeData.IsShortMoveInput;
                case ConditionCommand.LostMove:
                    return actor.InputProvider != null && !actor.InputProvider.HasMovementInput();
                default:
                    return false;
            }
        }


        /// <summary>
        /// 评估 ActionLifecycle 类型的路由（动作播放结束时调用）。
        /// </summary>
        public bool EvaluateLifecycle(CharacterEntity actor)
        {
            if (Category != RouteTriggerCategory.ActionLifecycle)
                return false;

            return CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
        }

        /// <summary>
        /// 检查此路由是否匹配指定的指令（仅用于 OwnsCommand 判断）。
        /// </summary>
        public bool MatchesCommand(InputCommand commandType, CommandPhase commandPhase)
        {
            return Category == RouteTriggerCategory.PlayerCommand &&
                   RequiredType == commandType &&
                   RequiredPhase == commandPhase;
        }
    }
}
