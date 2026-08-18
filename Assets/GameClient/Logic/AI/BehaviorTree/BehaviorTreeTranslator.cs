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
        public static TranslationResult Translate(BehaviorTreeAsset asset, Blackboard runtimeBlackboard)
        {
            var result = new TranslationResult();
            if (asset == null || asset.rootNode == null)
            {
                return result;
            }

            Node mainNode = TranslateNode(asset.rootNode, runtimeBlackboard, result.GuidToNodeMap);
            if (mainNode != null)
            {
                result.Root = new Root(runtimeBlackboard, mainNode);
            }
            return result;
        }

        private static Node TranslateNode(NodeData data, Blackboard bb, Dictionary<string, Node> map)
        {
            var node = TranslateNodeInternal(data, bb, map);
            if (node != null && !string.IsNullOrEmpty(data.guid))
            {
                map[data.guid] = node;
            }
            return node;
        }

        private static Node TranslateNodeInternal(NodeData data, Blackboard bb, Dictionary<string, Node> map)
        {
            if (data == null) return null;

            switch (data)
            {
                // -- 根节点特殊处理 --
                case RootData rootData:
                    // RootData 只是一个壳，实际执行其 child
                    return rootData.child != null ? TranslateNode(rootData.child, bb, map) : null;

                // -- Composites --
                case SelectorData _:
                    return new Selector(TranslateChildren(data, bb, map));
                    
                case SequenceData _:
                    return new Sequence(TranslateChildren(data, bb, map));
                    
                case ParallelData parallel:
                    return new Parallel(parallel.successPolicy, parallel.failurePolicy, TranslateChildren(data, bb, map));
                    
                case RandomSelectorData _:
                    return new RandomSelector(TranslateChildren(data, bb, map));
                    
                case RandomSequenceData _:
                    return new RandomSequence(TranslateChildren(data, bb, map));

                // -- Decorators --
                case InverterData _:
                    return new Inverter(TranslateChild(data, bb, map));
                    
                case FailerData _:
                    return new Failer(TranslateChild(data, bb, map));
                    
                case SucceederData _:
                    return new Succeeder(TranslateChild(data, bb, map));
                    
                case RepeaterData repeater:
                    return repeater.loopCount < 0 
                        ? new Repeater(TranslateChild(data, bb, map)) 
                        : new Repeater(repeater.loopCount, TranslateChild(data, bb, map));
                        
                case CooldownData cooldown:
                    return new Cooldown(
                        cooldown.cooldownTime, 
                        cooldown.randomVariation, 
                        cooldown.startAfterDecoratee, 
                        cooldown.resetOnFailure, 
                        cooldown.failOnCooldown, 
                        TranslateChild(data, bb, map)
                    );
                    
                case TimeMaxData timeMax:
                    return new TimeMax(timeMax.limit, timeMax.randomVariation, timeMax.waitForChildButFailOnLimitReached, TranslateChild(data, bb, map));
                    
                case TimeMinData timeMin:
                    return new TimeMin(timeMin.limit, timeMin.randomVariation, timeMin.waitOnFailure, TranslateChild(data, bb, map));
                    
                case RandomData random:
                    return new Random(random.probability, TranslateChild(data, bb, map));

                case BBCheckBoolData bbcBool:
                    return new BlackboardCondition(BBKeyMapper.GetString(bbcBool.key), bbcBool.op, bbcBool.value, bbcBool.stopsOnChange, TranslateChild(data, bb, map));
                case BBCheckIntData bbcInt:
                    return new BlackboardCondition(BBKeyMapper.GetString(bbcInt.key), bbcInt.op, bbcInt.value, bbcInt.stopsOnChange, TranslateChild(data, bb, map));
                case BBCheckFloatData bbcFloat:
                    return new BlackboardCondition(BBKeyMapper.GetString(bbcFloat.key), bbcFloat.op, bbcFloat.value, bbcFloat.stopsOnChange, TranslateChild(data, bb, map));
                case BBCheckStringData bbcString:
                    return new BlackboardCondition(BBKeyMapper.GetString(bbcString.key), bbcString.op, bbcString.value, bbcString.stopsOnChange, TranslateChild(data, bb, map));

                case BlackboardQueryData bbq:
                    return new BlackboardQuery(bbq.keys, bbq.stopsOnChange, () => true, TranslateChild(data, bb, map));

                // 暂时的 Service / Condition 缺省实现（需要具体逻辑注入，可使用拓展或者重载）
                case ServiceData service:
                    return new Service(service.interval, service.randomVariation, () => { /* 默认空服务 */ }, TranslateChild(data, bb, map));
                    
                case ConditionData condition:
                    return new Condition(() => { return true; }, condition.stopsOnChange, condition.checkInterval, condition.checkVariance, TranslateChild(data, bb, map));
                    
                case WaitForConditionData waitCond:
                    return new WaitForCondition(() => { return true; }, waitCond.checkInterval, waitCond.randomVariance, TranslateChild(data, bb, map));

                // -- Tasks --
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

        private static Node[] TranslateChildren(NodeData data, Blackboard bb, Dictionary<string, Node> map)
        {
            var children = data.GetChildren();
            var result = new List<Node>();
            foreach (var childData in children)
            {
                var translated = TranslateNode(childData, bb, map);
                if (translated != null)
                {
                    result.Add(translated);
                }
            }
            return result.ToArray();
        }

        private static Node TranslateChild(NodeData data, Blackboard bb, Dictionary<string, Node> map)
        {
            if (data is DecoratorData decorator && decorator.child != null)
            {
                return TranslateNode(decorator.child, bb, map);
            }
            
            // NPBehave Decorator 必须有子节点。如果编辑器中未连线，用 WaitUntilStopped 占位避免崩溃
            return new WaitUntilStopped(); 
        }
    }
}
