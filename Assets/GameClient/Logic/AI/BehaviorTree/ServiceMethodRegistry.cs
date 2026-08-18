using System;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public static class ServiceMethodRegistry
    {
        public static Action GetMethod(ServiceMethodType methodType, NPBehave.Blackboard bb)
        {
            switch (methodType)
            {
                case ServiceMethodType.UpdateTargetState:
                    return () => UpdateTargetState(bb);
                case ServiceMethodType.UpdateSelfState:
                    return () => UpdateSelfState(bb);
                default:
                    return () => { };
            }
        }

        private static void UpdateTargetState(NPBehave.Blackboard bb)
        {
            // Fetch owner entity from blackboard (injected in BTRunner)
            if (!bb.Isset("GameObject")) return;
            var go = bb.Get<GameObject>("GameObject");
            if (go == null) return;

            var monster = go.GetComponent<MonsterEntity>();
            if (monster == null || monster.TargetFinder == null) return;

            // Example target logic
            var target = monster.TargetFinder.GetTarget();
            if (target != null)
            {
                bb[BBKeyMapper.GetString(BBKey.HasTarget)] = true;
                bb[BBKeyMapper.GetString(BBKey.TargetDistance)] = Vector3.Distance(go.transform.position, target.transform.position);
                
                var dir = (target.transform.position - go.transform.position).normalized;
                bb[BBKeyMapper.GetString(BBKey.TargetDirection)] = dir;
            }
            else
            {
                bb[BBKeyMapper.GetString(BBKey.HasTarget)] = false;
            }
        }

        private static void UpdateSelfState(NPBehave.Blackboard bb)
        {
            if (!bb.Isset("GameObject")) return;
            var go = bb.Get<GameObject>("GameObject");
            if (go == null) return;

            var monster = go.GetComponent<MonsterEntity>();
            if (monster == null) return;

            // bb[BBKeyMapper.GetString(BBKey.CurrentHP)] = monster.CurrentHP;
            // bb[BBKeyMapper.GetString(BBKey.IsDead)] = false; // TODO: link to actual HP logic
        }
    }
}
