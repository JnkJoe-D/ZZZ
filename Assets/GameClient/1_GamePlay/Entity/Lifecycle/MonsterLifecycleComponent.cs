using System.Collections;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物专属生命周期组件。
    /// 在通用生命周期的基础上，处理停止 AI 行为树、关闭物理胶囊体、并延迟/立即触发 MonsterManager 回收入池。
    /// </summary>
    public class MonsterLifecycleComponent : LifecycleComponent
    {
        [Tooltip("死亡后延迟回收入池时间（秒），供播放死亡动画或溶解特效")]
        public float recycleDelay = 0.5f;

        public override void Init(CharacterEntity entity)
        {
            base.Init(entity);

            // 复用出池时恢复碰撞体激活状态
            var cc = entity.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = true;
            }
        }

        protected override void HandleDeath(CharacterEntity attacker)
        {
            base.HandleDeath(attacker);

            if (_entity is MonsterEntity monster)
            {
                // 1. 立即停止 AI 行为树
                monster.BTRunner?.StopTree();

                // 2. 禁用物理碰撞与控制器，防止尸体阻挡玩家或吃判定
                var cc = monster.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                }

                // 3. 实体死亡事实已由 base.HandleDeath -> EventCenter.Publish(new EntityDiedEvent) 广播，
                // 对象的延迟入池与销毁生命周期完全由其创建持有者 MonsterManager 统筹闭环，本组件不再越级反向调用单例。
            }
        }
    }
}
