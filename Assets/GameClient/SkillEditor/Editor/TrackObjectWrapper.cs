using UnityEngine;
using UnityEditor;

namespace SkillEditor.Editor
{
    public static class TrackObjectUtility
    {
        public static void RefreshWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<SkillEditorWindow>();
            foreach (var window in windows) window.RefreshWindow();
        }
    }

    /// <summary>
    /// 分组的 ScriptableObject 封装，用于在 Unity Inspector 中显示
    /// </summary>
    public class GroupObject : ScriptableObject
    {
        [HideInInspector]
        public Group groupData;
        
        [HideInInspector]
        public SkillTimeline timeline;

        public static GroupObject Create(Group groupData, SkillTimeline timeline)
        {
            GroupObject obj = ScriptableObject.CreateInstance<GroupObject>();
            obj.hideFlags = HideFlags.DontSave;
            obj.groupData = groupData;
            obj.timeline = timeline;
            obj.name = groupData.groupName;
            return obj;
        }
    }

    /// <summary>
    /// 轨道的 ScriptableObject 封装，用于在 Unity Inspector 中显示
    /// </summary>
    public class TrackObject : ScriptableObject
    {
        [HideInInspector]
        public TrackBase trackData;
        
        [HideInInspector]
        public SkillTimeline timeline;

        /// <summary>
        /// 创建轨道对象
        /// </summary>
        public static TrackObject Create(TrackBase trackData, SkillTimeline timeline)
        {
            TrackObject obj = ScriptableObject.CreateInstance<TrackObject>();
            obj.hideFlags = HideFlags.DontSave;
            obj.trackData = trackData;
            obj.timeline = timeline;
            obj.name = trackData.trackName;
            return obj;
        }
    }

    /// <summary>
    /// 片段的 ScriptableObject 封装，用于在 Unity Inspector 中显示
    /// </summary>
    public class ClipObject : ScriptableObject
    {
        [HideInInspector]
        public ClipBase clipData;
        
        [HideInInspector]
        public SkillTimeline timeline;

        /// <summary>
        /// 创建片段对象
        /// </summary>
        public static ClipObject Create(ClipBase clipData, SkillTimeline timeline)
        {
            ClipObject obj = ScriptableObject.CreateInstance<ClipObject>();
            obj.hideFlags = HideFlags.DontSave;
            obj.clipData = clipData;
            obj.timeline = timeline;
            obj.name = clipData.GetType().Name;
            return obj;
        }
    }

    /// <summary>
    /// TrackObject 的自定义 Inspector
    /// </summary>
    [CustomEditor(typeof(TrackObject))]
    public class TrackObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TrackObject trackObj = (TrackObject)target;
            if (trackObj.trackData == null) return;

            TrackBase track = trackObj.trackData;

            EditorGUI.BeginChangeCheck();
            // 使用新的 Drawer 系统
            var drawer = SkillEditor.Editor.DrawerFactory.CreateDrawer(track);
            if (drawer != null)
            {
                drawer.UndoContext = (trackObj.timeline != null) ? new Object[] { trackObj, trackObj.timeline } : new Object[] { trackObj };
                drawer.OnInspectorChanged += () => {
                    SceneView.RepaintAll();
                };
                drawer.DrawInspector(track);
            }
            else
            {
                // Fallback (手动处理 Undo)
                if (trackObj.timeline != null) Undo.RecordObjects(new Object[] { trackObj, trackObj.timeline }, "编辑轨道属性");
                track.trackName = EditorGUILayout.TextField("轨道名称", track.trackName);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (trackObj.timeline != null) EditorUtility.SetDirty(trackObj.timeline);
                TrackObjectUtility.RefreshWindows();
            }

            // 标记为已修改
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }

    /// <summary>
    /// ClipObject 的自定义 Inspector
    /// </summary>
    [CustomEditor(typeof(ClipObject))]
    public class ClipObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ClipObject clipObj = (ClipObject)target;
            if (clipObj.clipData == null) return;

            ClipBase clip = clipObj.clipData;

            EditorGUI.BeginChangeCheck();
            // 使用新的 Drawer 系统
            var drawer = SkillEditor.Editor.ClipDrawerFactory.CreateDrawer(clip);
            if (drawer != null)
            {
                drawer.UndoContext = (clipObj.timeline != null) ? new Object[] { clipObj, clipObj.timeline } : new Object[] { clipObj };
                drawer.OnInspectorChanged += () => {
                    SceneView.RepaintAll();
                };
                drawer.DrawInspector(clip);
            }
            else //没有Drawer的默认绘制，一般不会跳转到这里因为即使未定义也会默认Drawer
            {
                // Fallback
                if (clipObj.timeline != null) Undo.RecordObjects(new Object[] { clipObj, clipObj.timeline }, "编辑片段属性");
                clip.clipName = EditorGUILayout.TextField("片段名称", clip.clipName);
                clip.StartTime = EditorGUILayout.FloatField("开始时间", clip.StartTime);
                clip.Duration = EditorGUILayout.FloatField("持续时间", clip.Duration);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (clipObj.timeline != null) EditorUtility.SetDirty(clipObj.timeline);
                TrackObjectUtility.RefreshWindows();
            }

            // 标记为已修改
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }

    /// <summary>
    /// GroupObject 的自定义 Inspector
    /// </summary>
    [CustomEditor(typeof(GroupObject))]
    public class GroupObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GroupObject groupObj = (GroupObject)target;
            if (groupObj.groupData == null) return;

            Group group = groupObj.groupData;

            EditorGUILayout.LabelField("分组信息", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            if (groupObj.timeline != null) Undo.RecordObjects(new Object[] { groupObj, groupObj.timeline }, "编辑分组属性");

            group.groupName = EditorGUILayout.TextField("分组名称", group.groupName);
            group.isEnabled = EditorGUILayout.Toggle("启用", group.isEnabled);
            group.isCollapsed = EditorGUILayout.Toggle("折叠", group.isCollapsed);
            group.isLocked = EditorGUILayout.Toggle("锁定", group.isLocked);

            if (EditorGUI.EndChangeCheck())
            {
                if (groupObj.timeline != null) EditorUtility.SetDirty(groupObj.timeline);
                TrackObjectUtility.RefreshWindows();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("轨道数量", (group.tracks?.Count ?? 0).ToString());

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
