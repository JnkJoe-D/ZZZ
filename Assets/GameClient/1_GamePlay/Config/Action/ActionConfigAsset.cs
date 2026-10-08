using System.Collections.Generic;
using Game.Framework;
using UnityEngine;
using UnityEngine.Serialization;
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

        [Header("动作领域 (Action Domain)")]
        [Tooltip("宏观动作领域分类，供领域生命周期控制器驱动阻尼、受击衰减、闪避计时等宏观业务。")]
        [SerializeField] private ActionDomainId _domainId = ActionDomainId.Locomotion;

        public virtual ActionDomainId DomainId
        {
            get => _domainId;
            set => _domainId = value;
        }

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
            return item != null && item.BlendDuration >= 0f ? item.BlendDuration : -1f;
        }

        /// <summary>
        /// 更新针对目标动作的过渡参数（供过渡编辑器工作台调用）
        /// </summary>
        public void SetTransition(
            ActionConfigAsset targetAction,
            float blendDuration,
            bool hasEndTime = false,
            float endTime = 0f,
            bool hasStartTime = false,
            float startTime = 0f)
        {
            if (_transitionTable == null) _transitionTable = new ActionTransitionTable();
            _transitionTable.SetOrUpdate(targetAction, blendDuration, hasEndTime, endTime, hasStartTime, startTime);
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

        [System.NonSerialized]
        private ActionWindowRouteTable _cachedWindowTable;

        /// <summary>
        /// 获取此动作的窗口分桶快照表（仅缓存静态自有 Routes 与 RouteSets）。
        /// 若未构建则调用 ActionRouteTableBuilder.BuildTable 首次构建并缓存。
        /// </summary>
        public ActionWindowRouteTable GetWindowRouteTable(CharacterEntity actor = null)
        {
            if (_cachedWindowTable != null) return _cachedWindowTable;
            _cachedWindowTable = ActionRouteTableBuilder.BuildTable(this, actor);
            return _cachedWindowTable;
        }

        /// <summary>
        /// 废弃分桶缓存（供编辑器修改或热重载时调用）
        /// </summary>
        public void InvalidateWindowRouteTable()
        {
            _cachedWindowTable = null;
        }
    }

    /// <summary>
    /// 单个目标动作的过渡表现参数（边属性）
    /// 包含源动作 EndTime, HasEndTime, BlendDuration 以及目标动作切入 StartTime, HasStartTime。
    /// </summary>
    [System.Serializable]
    public class ActionTransitionItem
    {
        [Tooltip("目标动作")]
        public ActionConfigAsset TargetAction;

        [Tooltip("源动作退出时间点（秒）。若未开启 HasEndTime 则在当前动作自然播放完成或窗口关闭时切出")]
        [FormerlySerializedAs("CustomExitTime")]
        public float EndTime = 0f;

        [Tooltip("是否指定源动作的自定义退出时间（提前打断/截断）")]
        [FormerlySerializedAs("HasCustomExitTime")]
        public bool HasEndTime = false;

        [Tooltip("混合过渡时间（秒）。-1 表示使用目标动作自身默认起手 BlendIn；>= 0 强制覆盖")]
        [FormerlySerializedAs("CrossfadeDuration")]
        public float BlendDuration = -1f;

        [Tooltip("目标动作切入的起始时间点（秒）。默认为 0")]
        public float StartTime = 0f;

        [Tooltip("是否启用目标动作的自定义起始切入时间（默认关闭，从第 0 秒起手）")]
        public bool HasStartTime = false;

        // ── 向后兼容属性别名 (Backward-compatible Aliases) ──
        public float CustomExitTime { get => EndTime; set => EndTime = value; }
        public bool HasCustomExitTime { get => HasEndTime; set => HasEndTime = value; }
        public float CrossfadeDuration { get => BlendDuration; set => BlendDuration = value; }
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
        public void SetOrUpdate(
            ActionConfigAsset targetAction,
            float blendDuration,
            bool hasEndTime = false,
            float endTime = 0f,
            bool hasStartTime = false,
            float startTime = 0f)
        {
            if (targetAction == null) return;
            if (_items == null) _items = new List<ActionTransitionItem>();

            var item = GetTransition(targetAction);
            if (item == null)
            {
                item = new ActionTransitionItem { TargetAction = targetAction };
                _items.Add(item);
            }

            item.BlendDuration = blendDuration;
            item.HasEndTime = hasEndTime;
            item.EndTime = endTime;
            item.HasStartTime = hasStartTime;
            item.StartTime = startTime;
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
