using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SkillEditor;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 轨道注册表：负责扫描和缓存带有 [TrackDefinition] 的轨道类型
    /// </summary>
    public static class TrackRegistry
    {
        public class TrackInfo
        {
            public Type TrackType;
            public Type[] ClipTypes;
            public TrackDefinitionAttribute Attribute;
        }

        private static List<TrackInfo> registeredTracks;
        private static Dictionary<Type, List<Type>> trackToClipTypesMap;

        /// <summary>
        /// 获取所有已注册的轨道信息
        /// </summary>
        public static List<TrackInfo> GetRegisteredTracks()
        {
            if (registeredTracks == null)
            {
                Initialize();
            }
            return registeredTracks;
        }

        private static void Initialize()
        {
            registeredTracks = new List<TrackInfo>();
            trackToClipTypesMap = new Dictionary<Type, List<Type>>();

            // 第一阶段：扫描所有轨道
            List<Type> allTypes = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var asmName = asm.GetName().Name;
                if (asmName.StartsWith("System") || asmName.StartsWith("Unity") || 
                    asmName.StartsWith("mscorlib") || asmName.StartsWith("Mono"))
                    continue;

                try
                {
                    allTypes.AddRange(asm.GetTypes());
                }
                catch { }
            }

            foreach (var type in allTypes)
            {
                if (type == null || type.IsAbstract) continue;

                // 注册轨道
                if (type.IsSubclassOf(typeof(TrackBase)))
                {
                    var attr = type.GetCustomAttribute<TrackDefinitionAttribute>();
                    if (attr != null)
                    {
                        registeredTracks.Add(new TrackInfo
                        {
                            TrackType = type,
                            ClipTypes = attr.ClipTypes, // 初始保留轨道定义的片段
                            Attribute = attr
                        });
                    }
                }
            }

            // 第二阶段：扫描所有片段并进行反向注册
            foreach (var type in allTypes)
            {
                if (type == null || type.IsAbstract || !type.IsSubclassOf(typeof(ClipBase)))
                    continue;

                var clipAttr = type.GetCustomAttribute<ClipDefinitionAttribute>();
                if (clipAttr != null && clipAttr.TargetTrackTypes != null)
                {
                    foreach (var targetTrackType in clipAttr.TargetTrackTypes)
                    {
                        if (!trackToClipTypesMap.ContainsKey(targetTrackType))
                        {
                            trackToClipTypesMap[targetTrackType] = new List<Type>();
                        }
                        
                        if (!trackToClipTypesMap[targetTrackType].Contains(type))
                        {
                            trackToClipTypesMap[targetTrackType].Add(type);
                        }
                    }
                }
            }

            // 第三阶段：合并轨道定义的片段（如果有）到映射表中
            foreach (var info in registeredTracks)
            {
                if (info.ClipTypes != null)
                {
                    if (!trackToClipTypesMap.ContainsKey(info.TrackType))
                    {
                        trackToClipTypesMap[info.TrackType] = new List<Type>();
                    }

                    foreach (var ct in info.ClipTypes)
                    {
                        if (!trackToClipTypesMap[info.TrackType].Contains(ct))
                        {
                            trackToClipTypesMap[info.TrackType].Add(ct);
                        }
                    }
                }
            }

            // 根据 Order 排序
            registeredTracks.Sort((a, b) => a.Attribute.Order.CompareTo(b.Attribute.Order));
        }

        /// <summary>
        /// 创建指定类型的轨道实例
        /// </summary>
        public static TrackBase CreateTrack(Type trackType)
        {
            if (trackType == null || !typeof(TrackBase).IsAssignableFrom(trackType))
                return null;

            return (TrackBase)Activator.CreateInstance(trackType);
        }

        /// <summary>
        /// 根据轨道类型名称获取图标
        /// </summary>
        public static string GetTrackIcon(string trackTypeName)
        {
            if (registeredTracks == null) Initialize();

            foreach (var info in registeredTracks)
            {
                if (info.TrackType.Name == trackTypeName)
                {
                    return info.Attribute.Icon;
                }
            }
            return "ScriptableObject Icon"; // 默认图标
        }
        /// <summary>
        /// 根据轨道类型获取关联的所有片段类型
        /// </summary>
        public static Type[] GetClipTypes(Type trackType)
        {
            if (trackToClipTypesMap == null) Initialize();

            if (trackToClipTypesMap.TryGetValue(trackType, out var list))
            {
                return list.ToArray();
            }
            return Array.Empty<Type>();
        }

        /// <summary>
        /// 根据轨道类型获取关联的第一个片段类型 (兼容旧逻辑)
        /// </summary>
        public static Type GetClipType(Type trackType)
        {
            var types = GetClipTypes(trackType);
            return types.Length > 0 ? types[0] : null;
        }

        /// <summary>
        /// 获取片段类型的显示名称
        /// </summary>
        public static string GetClipDisplayName(Type clipType)
        {
            if (clipType == null) return "未知";

            var attr = clipType.GetCustomAttribute<ClipDefinitionAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.DisplayName))
            {
                return attr.DisplayName;
            }

            // 备选方案：清理类名，去掉 "Clip" 后缀并按驼峰拆分（可选，暂简单处理）
            string name = clipType.Name;
            if (name.EndsWith("Clip")) name = name.Substring(0, name.Length - 4);
            return name;
        }

        /// <summary>
        /// 根据轨道类型名称获取颜色
        /// </summary>
        public static Color GetTrackColor(string trackTypeName)
        {
            if (registeredTracks == null) Initialize();

            foreach (var info in registeredTracks)
            {
                if (info.TrackType.Name == trackTypeName)
                {
                    Color color;
                    if (ColorUtility.TryParseHtmlString(info.Attribute.ColorHex, out color))
                    {
                        return color;
                    }
                }
            }
            return Color.gray;
        }

        /// <summary>
        /// 根据片段类型获取对应的轨道类型名称
        /// </summary>
        public static string GetTrackTypeByClipType(Type clipType)
        {
            if (trackToClipTypesMap == null) Initialize();

            foreach (var kvp in trackToClipTypesMap)
            {
                if (kvp.Value.Contains(clipType))
                {
                    return kvp.Key.Name;
                }
            }
            return null;
        }
    }
}