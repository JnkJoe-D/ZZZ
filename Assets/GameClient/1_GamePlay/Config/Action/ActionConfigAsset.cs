using System.Collections.Generic;
using Game.Framework;
using UnityEngine;
using ATEditor;
namespace Game.GamePlay
{
    /// <summary>
    /// 动作播放结束后的转换策略。
    /// </summary>
    public enum ActionCompleteMode
    {
        /// <summary>
        /// 动作结束后由状态机默认逻辑决定（通常回 Idle）。
        /// </summary>
        Default = 0,

        /// <summary>
        /// 动作结束后保持当前状态不变（循环动作、由监控器驱动退出）。
        /// </summary>
        Stay = 10,

        /// <summary>
        /// 动作结束后自动衔接 CompleteAction 指定的后续动作。
        /// </summary>
        TransitToAction = 20,
    }

    /// <summary>
    /// 当收集有效路由时，当前动作对于前置动作路由的继承策略。
    /// </summary>
    public enum RouteInheritMode
    {
        [InspectorName("不继承 (None)")]
        None = 0,
        [InspectorName("继承 (继承优先于自身) (InheritPrioritizeInherited)")]
        InheritPrioritizeInherited = 10,
        [InspectorName("继承 (自身优先于继承) (InheritPrioritizeSelf)")]
        InheritPrioritizeSelf = 20,
        [InspectorName("继承 (完全覆盖自身) (InheritAndOverrideSelf)")]
        InheritAndOverrideSelf = 30,
    }

    /// <summary>
    /// 全局动作配置基类。
    /// 用于描述任意动作的基础信息，并关联 ActionTimeline 资产。
    /// </summary>
    /// <summary>
    /// 全局动作配置基类（混合态架构数据载体）
    /// 承载了 Timeline 资源引用、派生路由配置、以及状态机回流提示。
    /// </summary>
    public abstract class ActionConfigAsset : GameConfigAsset
    {
        [Header("基础信息")]
        public int ID;
        public string Name;

        [Header("SkillEditor 核心资产")]
        [Tooltip("SkillEditor 生成的标准化时间轴数据，ActionPlayer 会解析并播放它。")]
        public TextAsset TimelineAsset;
        [Tooltip("SkillEditor 生成的 ScriptableObject 格式时间轴数据（支持运行时热更），ActionPlayer 会优先解析此资源。")]
        public ATEditor.ActionTimeline actionTimelineSO;



        [Header("状态转换")]
        [Tooltip("动作正常完成后的后续转换策略。")]
        public ActionCompleteMode CompleteMode = ActionCompleteMode.Default;

        [Tooltip("当 CompleteMode 设为 TransitToAction 时，自动衔接的这个后续动作。")]
        public ActionConfigAsset CompleteAction;

        [SerializeField, HideInInspector]
        private ActionTransitionTable _transitionTable = new();

        /// <summary>
        /// 过渡表现配置表（仅只读访问，编辑需通过 Action Transition Workbench）
        /// </summary>
        public ActionTransitionTable TransitionTable => _transitionTable ??= new ActionTransitionTable();

        /// <summary>
        /// 获取转移到目标动作的过渡参数（未配置则返回 null）
        /// </summary>
        public ActionTransitionItem GetTransition(ActionConfigAsset targetAction)
        {
            return _transitionTable?.GetTransition(targetAction);
        }

        /// <summary>
        /// 获取转移到目标动作的有效混合时间（秒）。>= 0 表示覆盖值，-1 表示回退到目标动作默认起手 BlendIn
        /// </summary>
        public float GetTransitionCrossfade(ActionConfigAsset targetAction)
        {
            var item = GetTransition(targetAction);
            return item != null && item.CrossfadeDuration >= 0f ? item.CrossfadeDuration : -1f;
        }

        /// <summary>
        /// 更新针对目标动作的过渡参数（供过渡编辑器工作台调用）
        /// </summary>
        public void SetTransition(ActionConfigAsset targetAction, float crossfade, bool hasCustomExit = false, float exitTime = 0f)
        {
            if (_transitionTable == null) _transitionTable = new ActionTransitionTable();
            _transitionTable.SetOrUpdate(targetAction, crossfade, hasCustomExit, exitTime);
        }



        [Header("派生路由 (Action Routes)")]
        [Tooltip("当前动作的派生路由列表，允许在此动作中响应输入或事件进行连段转移。")]
        public List<ActionRoute> Routes = new();

        [Header("通用路由集 (Route Sets)")]
        [Tooltip("通常用于配置闪避、移动等通用动作，打包成集合以便复用。")]
        public List<ActionRouteSetAsset> RouteSets = new();
        /// <summary>
        /// 收集此动作上所有有效的统一路由（展开集合资产）。
        /// 基础行为：收集自身配置的 Routes 和 RouteSets。派生类可重写加入继承逻辑。
        /// </summary>
        public virtual void CollectEffectiveRoutes(List<ActionRoute> results, CharacterEntity actor = null)
        {
            if (results == null) return;
            results.Clear();

            if (Routes != null)
            {
                foreach (var route in Routes)
                {
                    if (route != null) results.Add(route);
                }
            }

            if (RouteSets != null)
            {
                foreach (var routeSet in RouteSets)
                {
                    routeSet?.AppendRoutes(results);
                }
            }
        }
    }

    /// <summary>
    /// 单个目标动作的过渡表现参数（边属性）
    /// </summary>
    [System.Serializable]
    public class ActionTransitionItem
    {
        [Tooltip("目标动作")]
        public ActionConfigAsset TargetAction;

        [Tooltip("混合过渡时间（秒）。-1 表示使用目标动作自身默认起手 BlendIn；>= 0 强制覆盖")]
        public float CrossfadeDuration = -1f;

        [Tooltip("是否启用自定义打断时间（默认关闭，通常用于自循环动作或末尾自动衔接的提前截断）")]
        public bool HasCustomExitTime = false;

        [Tooltip("自定义打断/退出时间点（秒）")]
        public float CustomExitTime = 0f;
    }

    /// <summary>
    /// 动作过渡表现配置表（唯一归宿：ActionConfigAsset）
    /// 仅由专门的 Action Transition Workbench 进行可视化编辑与落盘，杜绝手填盲改。
    /// </summary>
    [System.Serializable]
    public class ActionTransitionTable
    {
        [SerializeField]
        private List<ActionTransitionItem> _items = new();

        public IReadOnlyList<ActionTransitionItem> Items => _items;

        /// <summary>
        /// 查询转移到目标动作的有效过渡配置项
        /// </summary>
        public ActionTransitionItem GetTransition(ActionConfigAsset targetAction)
        {
            if (targetAction == null || _items == null) return null;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null && _items[i].TargetAction == targetAction)
                {
                    return _items[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 更新或新增针对特定目标动作的过渡配置（供编辑器工作台调用）
        /// </summary>
        public void SetOrUpdate(ActionConfigAsset targetAction, float crossfade, bool hasCustomExit = false, float exitTime = 0f)
        {
            if (targetAction == null) return;
            if (_items == null) _items = new List<ActionTransitionItem>();

            var item = GetTransition(targetAction);
            if (item == null)
            {
                item = new ActionTransitionItem { TargetAction = targetAction };
                _items.Add(item);
            }

            item.CrossfadeDuration = crossfade;
            item.HasCustomExitTime = hasCustomExit;
            item.CustomExitTime = exitTime;
        }

        /// <summary>
        /// 移除针对某个目标动作的过渡配置项
        /// </summary>
        public bool Remove(ActionConfigAsset targetAction)
        {
            if (targetAction == null || _items == null) return false;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i] != null && _items[i].TargetAction == targetAction)
                {
                    _items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
