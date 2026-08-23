using System.Collections.Generic;
using NPBehave;

namespace Game.Logic.AI.BehaviorTree
{
    public class TranslationResult
    {
        public Root Root;
        public Dictionary<string, Node> GuidToNodeMap = new Dictionary<string, Node>();
    }

    /// <summary>
    /// 将 NodeData (SO) 转化为 NPBehave 运行时节点 (POCO) 的转译器。
    /// </summary>
    public static class BehaviorTreeTranslator
    {
        public static TranslationResult Translate(BehaviorTreeAsset asset, Blackboard runtimeBlackboard, TreeActionAgent agent = null)
        {
            var result = new TranslationResult();
            if (asset == null || asset.rootNode == null)
            {
                return result;
            }

            Node mainNode = TranslateNode(asset.rootNode, runtimeBlackboard, agent, result.GuidToNodeMap);
            if (mainNode != null)
            {
                result.Root = new Root(runtimeBlackboard, mainNode);
            }
            return result;
        }

        private static Node TranslateNode(NodeData data, Blackboard bb, TreeActionAgent agent, Dictionary<string, Node> map)
        {
            var node = TranslateNodeInternal(data, bb, agent, map);
            if (node != null && !string.IsNullOrEmpty(data.guid))
            {
                map[data.guid] = node;
            }
            return node;
        }

        private static Node TranslateNodeInternal(NodeData data, Blackboard bb, TreeActionAgent agent, Dictionary<string, Node> map)
        {
            if (data == null) return null;

            switch (data)
            {
                // -- 根节点特殊处理 --
                case RootData rootData:
                    // RootData 只是一个壳，实际执行其 child
                    return rootData.child != null ? TranslateNode(rootData.child, bb, agent, map) : null;

                // -- Composites --
                case SelectorData _:
                    return new Selector(TranslateChildren(data, bb, agent, map));
                    
                case SequenceData _:
                    return new Sequence(TranslateChildren(data, bb, agent, map));
                    
                case ParallelData parallel:
                    return new Parallel(parallel.successPolicy, parallel.failurePolicy, TranslateChildren(data, bb, agent, map));
                    
                case RandomSelectorData _:
                    return new RandomSelector(TranslateChildren(data, bb, agent, map));
                    
                case RandomSequenceData _:
                    return new RandomSequence(TranslateChildren(data, bb, agent, map));

                // -- Decorators --
                case InverterData _:
                    return new Inverter(TranslateChild(data, bb, agent, map));
                    
                case FailerData _:
                    return new Failer(TranslateChild(data, bb, agent, map));
                    
                case SucceederData _:
                    return new Succeeder(TranslateChild(data, bb, agent, map));
                    
                case RepeaterData repeater:
                    return repeater.loopCount < 0 
                        ? new Repeater(TranslateChild(data, bb, agent, map)) 
                        : new Repeater(repeater.loopCount, TranslateChild(data, bb, agent, map));
                        
                case CooldownData cooldown:
                    return new Cooldown(
                        cooldown.cooldownTime, 
                        cooldown.randomVariation, 
                        cooldown.startAfterDecoratee, 
                        cooldown.resetOnFailure, 
                        cooldown.failOnCooldown, 
                        TranslateChild(data, bb, agent, map)
                    );
                    
                case TimeMaxData timeMax:
                    return new TimeMax(timeMax.limit, timeMax.randomVariation, timeMax.waitForChildButFailOnLimitReached, TranslateChild(data, bb, agent, map));
                    
                case TimeMinData timeMin:
                    return new TimeMin(timeMin.limit, timeMin.randomVariation, timeMin.waitOnFailure, TranslateChild(data, bb, agent, map));
                    
                case RandomData random:
                    return new Random(random.probability, TranslateChild(data, bb, agent, map));

                case BBCheckBoolData bbcBool:
                    return new BlackboardCondition(BBKeyMapper.GetString(bbcBool.key), bbcBool.op, bbcBool.value, bbcBool.stopsOnChange, TranslateChild(data, bb, agent, map));
                case BBCheckIntData bbcInt:
                    return new BlackboardCondition(BBKeyMapper.GetString(bbcInt.key), bbcInt.op, bbcInt.value, bbcInt.stopsOnChange, TranslateChild(data, bb, agent, map));
                case BBCheckFloatData bbcFloat:
                    return new BlackboardCondition(BBKeyMapper.GetString(bbcFloat.key), bbcFloat.op, bbcFloat.value, bbcFloat.stopsOnChange, TranslateChild(data, bb, agent, map));
                case BBCheckStringData bbcString:
                    return new BlackboardCondition(BBKeyMapper.GetString(bbcString.key), bbcString.op, bbcString.value, bbcString.stopsOnChange, TranslateChild(data, bb, agent, map));

                case BBTriggerData bbTrigger:
                    return new NPBehave.BlackboardTrigger(BBKeyMapper.GetString(bbTrigger.key), bbTrigger.stopsOnChange, TranslateChild(data, bb, agent, map));

                case BlackboardQueryData bbq:
                    return new BlackboardQuery(bbq.keys, bbq.stopsOnChange, () => true, TranslateChild(data, bb, agent, map));

                // agent代理的服务节点，通常用于在行为树运行时每帧调用代理的 ServiceUpdate 方法
                case ServiceData service:
                    return new Service(service.interval, service.randomVariation, () => agent.ServiceUpdate(bb), TranslateChild(data, bb, agent, map));
                    
                case ConditionData condition:
                    return new Condition(() => { return true; }, condition.stopsOnChange, condition.checkInterval, condition.checkVariance, TranslateChild(data, bb, agent, map));
                    
                case WaitForConditionData waitCond:
                    return new WaitForCondition(() => { return true; }, waitCond.checkInterval, waitCond.randomVariance, TranslateChild(data, bb, agent, map));

                case BBCheckStateData checkStateData:
                    if (agent != null) {
                        return new Condition(() => agent.CheckAIState(checkStateData.targetState), checkStateData.stopsOnChange, TranslateChild(data, bb, agent, map));
                    }
                    return new Condition(() => false, checkStateData.stopsOnChange, TranslateChild(data, bb, agent, map));

                // -- Tasks --
                case TryPlayActionData tryPlayData:
                    if (agent != null) {
                        return new NPBehave.Action(() => agent.TryPlayAction(tryPlayData.actionConfig, out _));
                    }
                    return new NPBehave.Action(() => { });

                case ChangeAIStateData changeStateData:
                    if (agent != null) {
                        return new NPBehave.Action(() => agent.ChangeAIState(changeStateData.targetState));
                    }
                    return new NPBehave.Action(() => { });

                case EvaluateStateData evalStateData:
                    if (agent != null) {
                        return new NPBehave.Action(() => agent.EvaluateState(evalStateData.chaseDistance));
                    }
                    return new NPBehave.Action(() => { });

                case PlayActionAndWaitData playAndWaitData:
                    if (agent != null) {
                        return new Game.Logic.AI.BehaviorTree.Extensions.PlayActionAndWaitTask(
                            playAndWaitData.actionConfig,
                            playAndWaitData.stopAtEnd,
                            agent.TryPlayAction,
                            agent.CheckCommandFate,
                            () => agent.IsPlayingAction(playAndWaitData.actionConfig)
                        );
                    }
                    return new NPBehave.Action(() => { });

                case DistanceAdjustData adjustData:
                    if (agent != null) {
                        return new Game.Logic.AI.BehaviorTree.Extensions.DistanceAdjustTask(
                            adjustData,
                            agent.GetDistanceToTarget,
                            agent.TryPlayAction,
                            agent.CheckCommandFate,
                            agent.IsPlayingAction
                        );
                    }
                    return new NPBehave.Action(() => { });

                case MonsterAttackData attackData:
                    if (agent != null) {
                        return new Game.Logic.AI.BehaviorTree.Extensions.MonsterAttackTask(
                            attackData,
                            agent.TryPlayAction,
                            agent.CheckCommandFate,
                            agent.IsPlayingAction,
                            agent.StartAttackCooldown
                        );
                    }
                    return new NPBehave.Action(() => { });

                case MonsterHitData hitData:
                    if (agent != null) {
                        return new Game.Logic.AI.BehaviorTree.Extensions.MonsterHitTask(
                            agent.TryGetHitAction,
                            agent.TryPlayAction,
                            agent.CheckCommandFate,
                            agent.IsPlayingAction,
                            agent.ClearHitStun
                        );
                    }
                    return new NPBehave.Action(() => { });

                case MultiFrameDebugData multiDebugData:
                    return new Game.Logic.AI.BehaviorTree.Extensions.MultiFrameDebugTask(
                        multiDebugData.duration, 
                        multiDebugData.message
                    );

                case WaitData wait:
                    if (wait.readFromBlackboard)
                    {
                        return new Wait(wait.blackboardKey, wait.randomVariance);
                    }
                    return new Wait(wait.seconds, wait.randomVariance);

                case WaitUntilStoppedData waitStop:
                    return new WaitUntilStopped(waitStop.successWhenStopped);

                case DebugData debug:
                    return new NPBehave.Action(() => { UnityEngine.Debug.Log(debug.message); });

                case ActionData action:
                    // 作为抽象基类，这里暂不实例化具体行为。实际项目中通常会由具体的 ActionData 子类生成对应的 Task
                    return new NPBehave.Action(() => { });

                default:
                    UnityEngine.Debug.LogWarning($"[BehaviorTreeTranslator] Unsupported NodeData type: {data.GetType().Name}");
                    return null;
            }
        }

        private static Node[] TranslateChildren(NodeData data, Blackboard bb, TreeActionAgent agent, Dictionary<string, Node> map)
        {
            var children = data.GetChildren();
            var result = new List<Node>();
            foreach (var childData in children)
            {
                var translated = TranslateNode(childData, bb, agent, map);
                if (translated != null)
                {
                    result.Add(translated);
                }
            }
            return result.ToArray();
        }

        private static Node TranslateChild(NodeData data, Blackboard bb, TreeActionAgent agent, Dictionary<string, Node> map)
        {
            if (data is DecoratorData decorator && decorator.child != null)
            {
                return TranslateNode(decorator.child, bb, agent, map);
            }
            
            // NPBehave Decorator 必须有子节点。如果编辑器中未连线，用 WaitUntilStopped 占位避免崩溃
            return new WaitUntilStopped(); 
        }
    }
}
