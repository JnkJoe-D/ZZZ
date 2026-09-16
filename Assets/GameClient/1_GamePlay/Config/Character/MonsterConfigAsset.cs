using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物专属资产配置。
    /// 仅承载怪物实体的资源装配与行为资产绑定（Prefab、ActionRoot、HitReactionConfig、BehaviorTree、SensorConfig）。
    /// 所有数值与战斗机制参数（HP、ATK、MaxDaze、弱点、抗性等）严格由 Luban 怪物配表驱动。
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterConfigAsset", menuName = "Config/Role/Monster Config")]
    public class MonsterConfigAsset : CharacterConfigAsset
    {
        [Header("感知与行为参数 (供 AI 读取)")]
        public MonsterSensorConfig SensorConfig = new MonsterSensorConfig();

        [Header("移动与失衡表现配置")]
        [Tooltip("怪物移动表现配置（奔跑与全向步态）")]
        public MonsterLocomotionConfig locomotionConfig;

        [Tooltip("怪物失衡瘫痪表现配置（三段式）")]
        public MonsterStunConfig stunConfig;

        [Header("AI 行为")]
        [Tooltip("怪物的行为树资产")]
        public BehaviorTreeAsset BehaviorTree;

        protected override void CollectActionConfigs(System.Collections.Generic.HashSet<ActionConfigAsset> collectedActions)
        {
            base.CollectActionConfigs(collectedActions);

            if (locomotionConfig != null)
            {
                foreach (var action in locomotionConfig.GetAllActionConfigs())
                {
                    CollectActionRecursive(action, collectedActions);
                }
            }

            if (stunConfig != null)
            {
                foreach (var action in stunConfig.GetAllActionConfigs())
                {
                    CollectActionRecursive(action, collectedActions);
                }
            }

            if (BehaviorTree != null && BehaviorTree.nodes != null)
            {
                foreach (var node in BehaviorTree.nodes)
                {
                    if (node == null) continue;

                    // 使用反射扫描节点中所有引用了 ActionConfigAsset (及其子类) 的字段
                    var fields = node.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (typeof(ActionConfigAsset).IsAssignableFrom(field.FieldType))
                        {
                            if (field.GetValue(node) is ActionConfigAsset action)
                            {
                                CollectActionRecursive(action, collectedActions);
                            }
                        }
                    }
                }
            }
        }
    }
}
