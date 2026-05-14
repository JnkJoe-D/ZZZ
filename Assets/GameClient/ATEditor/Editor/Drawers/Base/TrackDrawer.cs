using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    public class TrackDrawer : SkillInspectorBase
    {
        // 保持 DrawInspector(TrackBase track) 签名，但内部调用 base
        public virtual void DrawInspector(TrackBase track)
        {
            base.DrawInspector(track);
        }
    }
    
    // 自动注册中心
    public static class DrawerFactory
    {
        private static System.Collections.Generic.Dictionary<System.Type, System.Type> _drawerMap;

        private static void Initialize()
        {
            _drawerMap = new System.Collections.Generic.Dictionary<System.Type, System.Type>();
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                // Simple filter to speed up
                var asmName = asm.GetName().Name;
                if (asmName.StartsWith("System") || asmName.StartsWith("mscorlib")) continue;

                 System.Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }

                foreach (var type in types)
                {
                    if (typeof(TrackDrawer).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        var attr = (CustomDrawerAttribute)System.Attribute.GetCustomAttribute(type, typeof(CustomDrawerAttribute));
                        if (attr != null && attr.TargetType != null)
                        {
                            _drawerMap[attr.TargetType] = type;
                        }
                    }
                }
            }
        }

        public static TrackDrawer CreateDrawer(TrackBase track)
        {
            if (_drawerMap == null) Initialize();

            if (track != null && _drawerMap.TryGetValue(track.GetType(), out var drawerType))
            {
                return (TrackDrawer)System.Activator.CreateInstance(drawerType);
            }
            return new DefaultTrackDrawer();
        }
    }
    
    public class DefaultTrackDrawer : TrackDrawer
    {
        public override void DrawInspector(TrackBase track)
        {
            base.DrawInspector(track);
        }
    }
}
