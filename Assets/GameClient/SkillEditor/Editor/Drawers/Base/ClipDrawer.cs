using UnityEditor;
using UnityEngine;
using SkillEditor;

namespace SkillEditor.Editor
{
    public class ClipDrawer : SkillInspectorBase
    {
        public virtual void DrawInspector(ClipBase clip)
        {
            base.DrawInspector(clip);
        }

        public virtual void DrawSceneGUI(ClipBase clip, SkillEditorState state)
        {
            // 供子类重写，用于在 Scene 窗口绘制辅助图形 (Gizmos/Handles)
        }

        public virtual void DrawTimelineGUI(ClipBase clip, Rect clipRect, SkillEditorState state, Color clipColor, string displayName)
        {
            if (!clip.isEnabled) clipColor.a *= 0.5f;

            EditorGUI.DrawRect(clipRect, clipColor);
            if (!clip.isEnabled) EditorGUI.DrawRect(clipRect, SkillEditorStyles.ClipDisabledOverlayColor);

            EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, clipRect.width, 1), SkillEditorStyles.ClipDefaultBorderColor);
            EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y + clipRect.height - 1, clipRect.width, 1), SkillEditorStyles.ClipDefaultBorderColor);
            EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, 1, clipRect.height), SkillEditorStyles.ClipDefaultBorderColor);
            EditorGUI.DrawRect(new Rect(clipRect.x + clipRect.width - 1, clipRect.y, 1, clipRect.height), SkillEditorStyles.ClipDefaultBorderColor);

            if (state.selectedClips.Contains(clip))
            {
                Color highlightColor = SkillEditorStyles.ClipSelectedBorderColor;
                float borderWidth = 2f;
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, clipRect.width, borderWidth), highlightColor);
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y + clipRect.height - borderWidth, clipRect.width, borderWidth), highlightColor);
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, borderWidth, clipRect.height), highlightColor);
                EditorGUI.DrawRect(new Rect(clipRect.x + clipRect.width - borderWidth, clipRect.y, borderWidth, clipRect.height), highlightColor);
            }

            // Blending 
            if (clip.SupportsBlending)
            {
                if (clip.blendInDuration > 0)
                {
                    float blendInWidth = Mathf.Min(clip.blendInDuration * state.zoom, clipRect.width);
                    Vector3[] verts = new Vector3[] {
                        new Vector3(clipRect.x, clipRect.y + clipRect.height, 0),
                        new Vector3(clipRect.x + blendInWidth, clipRect.y, 0),
                        new Vector3(clipRect.x, clipRect.y, 0)
                    };

                    Handles.BeginGUI();
                    Handles.color = SkillEditorStyles.BlendAreaFillColor;
                    Handles.DrawAAConvexPolygon(verts);

                    Handles.color = SkillEditorStyles.BlendAreaLineColor;
                    Handles.DrawLine(verts[0], verts[1]);
                    Handles.DrawLine(new Vector3(clipRect.x, clipRect.y, 0), new Vector3(clipRect.x, clipRect.y + clipRect.height, 0));
                    Handles.EndGUI();
                }

                if (clip.blendOutDuration > 0)
                {
                    float blendOutWidth = Mathf.Min(clip.blendOutDuration * state.zoom, clipRect.width);
                    Vector3[] verts = new Vector3[] {
                        new Vector3(clipRect.x + clipRect.width - blendOutWidth, clipRect.y, 0),
                        new Vector3(clipRect.x + clipRect.width, clipRect.y + clipRect.height, 0),
                        new Vector3(clipRect.x + clipRect.width, clipRect.y, 0)
                    };

                    Handles.BeginGUI();
                    Handles.color = SkillEditorStyles.BlendAreaFillColor;
                    Handles.DrawAAConvexPolygon(verts);

                    Handles.color = SkillEditorStyles.BlendAreaLineColor;
                    Handles.DrawLine(verts[0], verts[1]);
                    Handles.DrawLine(new Vector3(clipRect.x + clipRect.width, clipRect.y, 0), new Vector3(clipRect.x + clipRect.width, clipRect.y + clipRect.height, 0));
                    Handles.EndGUI();
                }
            }

            // Name text
            float blendInW = clip.SupportsBlending ? (clip.blendInDuration * state.zoom) : 0;
            float blendOutW = clip.SupportsBlending ? (clip.blendOutDuration * state.zoom) : 0;
            Rect textRect = new Rect(clipRect.x + blendInW, clipRect.y, Mathf.Max(0, clipRect.width - blendInW - blendOutW), clipRect.height);

            if (textRect.width > 15)
            {
                GUIStyle clipStyle = new GUIStyle(EditorStyles.whiteLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    padding = new RectOffset(2, 2, 0, 0),
                    clipping = TextClipping.Clip
                };
                
                GUI.Label(textRect, displayName, clipStyle);
            }
        }
    }
    
    public static class ClipDrawerFactory
    {
        private static System.Collections.Generic.Dictionary<System.Type, System.Type> _drawerMap;
        private static System.Collections.Generic.Dictionary<System.Type, ClipDrawer> _drawerInstances = new System.Collections.Generic.Dictionary<System.Type, ClipDrawer>();

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
                    if (typeof(ClipDrawer).IsAssignableFrom(type) && !type.IsAbstract)
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

        public static ClipDrawer CreateDrawer(ClipBase clip)
        {
            if (_drawerMap == null) Initialize();

            if (clip != null && _drawerMap.TryGetValue(clip.GetType(), out var drawerType))
            {
                return (ClipDrawer)System.Activator.CreateInstance(drawerType);
            }
            return new DefaultClipDrawer();
        }

        public static ClipDrawer GetDrawerInstance(ClipBase clip)
        {
            if (_drawerMap == null) Initialize();
            System.Type clipType = clip != null ? clip.GetType() : null;
            if (clipType != null)
            {
                if (!_drawerInstances.TryGetValue(clipType, out var drawer))
                {
                    if (_drawerMap.TryGetValue(clipType, out var drawerType))
                    {
                        drawer = (ClipDrawer)System.Activator.CreateInstance(drawerType);
                    }
                    else
                    {
                        drawer = new DefaultClipDrawer();
                    }
                    _drawerInstances[clipType] = drawer;
                }
                return drawer;
            }
            return new DefaultClipDrawer();
        }
    }
    
    public class DefaultClipDrawer : ClipDrawer
    {
        public override void DrawInspector(ClipBase clip)
        {
            base.DrawInspector(clip);
        }
    }
}
