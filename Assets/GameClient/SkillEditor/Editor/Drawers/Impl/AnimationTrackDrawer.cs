using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(AnimationTrack))]
    public class AnimationTrackDrawer : TrackDrawer
    {
        public override void DrawInspector(TrackBase track)
        {
            var animTrack = track as AnimationTrack;
            if (animTrack == null) return;

            EditorGUILayout.LabelField("动画轨道", EditorStyles.boldLabel);

            // 获取同 Timeline 下的所有 AnimationTrack
            List<AnimationTrack> allAnimTracks = CollectAnimationTracks();

            // 在绘制基础属性之前，先强制执行约束（静默修正非法状态）
            EnforceMasterTrackConstraints(allAnimTracks, animTrack);

            // 绘制基础属性（isMasterTrack 由我们手动绘制，基类跳过）
            base.DrawInspector(track);

            // 手动绘制 isMasterTrack Toggle（带 ToggleGroup 约束）
            DrawMasterTrackToggle(animTrack, allAnimTracks);
        }

        /// <summary>
        /// 隐藏 isMasterTrack 字段，由 DrawMasterTrackToggle 手动绘制
        /// </summary>
        protected override bool ShouldShow(FieldInfo field, object obj)
        {
            if (field.Name == nameof(AnimationTrack.isMasterTrack))
            {
                return false;
            }

            return base.ShouldShow(field, obj);
        }

        /// <summary>
        /// 手动绘制主轨道 Toggle，实现 ToggleGroup 排他约束
        /// </summary>
        private void DrawMasterTrackToggle(AnimationTrack currentTrack, List<AnimationTrack> allAnimTracks)
        {
            bool isSoleTrack = allAnimTracks.Count <= 1;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (isSoleTrack)
            {
                // 只有一条动画轨道：强制 true，不可编辑
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("主轨道", true);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField("(唯一轨道)", EditorStyles.miniLabel, GUILayout.Width(60));
            }
            else if (currentTrack.isMasterTrack)
            {
                // 当前是主轨道：不允许直接取消勾选（必须勾选其他轨道来替代）
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("主轨道", true);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField("(激活中)", EditorStyles.miniLabel, GUILayout.Width(60));
            }
            else
            {
                // 当前不是主轨道：允许勾选，勾选后会取消其他轨道的主轨道状态
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayout.Toggle("主轨道", false);
                if (EditorGUI.EndChangeCheck() && newValue)
                {
                    SetAsMasterTrack(currentTrack, allAnimTracks);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 将指定轨道设为主轨道，取消其他所有 AnimationTrack 的主轨道状态
        /// </summary>
        private void SetAsMasterTrack(AnimationTrack newMaster, List<AnimationTrack> allAnimTracks)
        {
            if (UndoContext != null && UndoContext.Length > 0)
            {
                Undo.RecordObjects(UndoContext, "切换主轨道");
            }

            foreach (var t in allAnimTracks)
            {
                t.isMasterTrack = (t == newMaster);
            }

            TrackObjectUtility.RefreshWindows();
        }

        /// <summary>
        /// 静默修正非法状态（在绘制前执行，不弹窗）
        /// - 只有一条：强制 true
        /// - 多条全 false：排序最前的设为 true
        /// - 多条 true：只保留排序最前的
        /// </summary>
        private static void EnforceMasterTrackConstraints(List<AnimationTrack> allAnimTracks, AnimationTrack currentTrack)
        {
            if (allAnimTracks.Count == 0) return;

            if (allAnimTracks.Count == 1)
            {
                // 只有一条：强制 true
                allAnimTracks[0].isMasterTrack = true;
                return;
            }

            // 统计有多少条标记了 isMaster
            int masterCount = 0;
            AnimationTrack firstMaster = null;
            foreach (var t in allAnimTracks)
            {
                if (t.isMasterTrack)
                {
                    masterCount++;
                    if (firstMaster == null) firstMaster = t;
                }
            }

            if (masterCount == 0)
            {
                // 全 false：将排序最前的设为 true
                allAnimTracks[0].isMasterTrack = true;
            }
            else if (masterCount > 1)
            {
                // 多条 true：只保留排序最前的（即 firstMaster）
                foreach (var t in allAnimTracks)
                {
                    t.isMasterTrack = (t == firstMaster);
                }
            }
        }

        /// <summary>
        /// 从当前 Selection 获取 Timeline，收集其中所有 AnimationTrack（按轨道在 groups 中的自然顺序）
        /// </summary>
        private static List<AnimationTrack> CollectAnimationTracks()
        {
            var result = new List<AnimationTrack>();

            SkillTimeline timeline = GetCurrentTimeline();
            if (timeline == null) return result;

            foreach (var track in timeline.AllTracks)
            {
                if (track is AnimationTrack animTrack)
                {
                    result.Add(animTrack);
                }
            }

            return result;
        }

        /// <summary>
        /// 通过 Inspector 选中项获取当前编辑的 SkillTimeline
        /// </summary>
        private static SkillTimeline GetCurrentTimeline()
        {
            // 优先从 TrackObject 获取（当前选中轨道时）
            if (Selection.activeObject is TrackObject trackObj && trackObj.timeline != null)
            {
                return trackObj.timeline;
            }

            // 兜底：从 SkillEditorWindow 获取
            var windows = Resources.FindObjectsOfTypeAll<SkillEditorWindow>();
            if (windows.Length > 0)
            {
                // SkillEditorWindow 有 state.currentTimeline
                var stateField = typeof(SkillEditorWindow).GetField("state",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateField != null)
                {
                    var state = stateField.GetValue(windows[0]) as SkillEditorState;
                    return state?.currentTimeline;
                }
            }

            return null;
        }
    }
}
