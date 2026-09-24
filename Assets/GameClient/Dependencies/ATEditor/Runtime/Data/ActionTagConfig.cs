using System.Collections.Generic;
using Game.Framework;
using UnityEngine;

namespace ATEditor
{
    [CreateAssetMenu(fileName = "ActionTagConfig", menuName = "ATEditor/TagConfig")]
    public class ActionTagConfig : ScriptableObject
    {
        [Tooltip("目标判定标签")]
        public List<string> availableTargetTags = new();

        [Tooltip("时间轴事件标签")]
        public List<string> availableEventTags = new();

        [Tooltip("招式路由窗口配置列表")]
        [SerializeReference, SubclassSelector]
        public List<RouteWindow> availableRouteWindows = new();
    }
}

