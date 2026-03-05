using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 技能编辑器全局 UI 状态
    /// </summary>
    public class SkillEditorState
    {
        // 核心数据引用
        public SkillTimeline currentTimeline;
        public string currentFilePath; // 当前编辑的 JSON 文件路径
        
        // 性能缓存：通过 ID 快速索引轨道
        private Dictionary<string, TrackBase> trackCache = new Dictionary<string, TrackBase>();
        
        /// <summary>
        /// 全量重建轨道缓存映射（遍历 groups → tracks）
        /// </summary>
        public void RebuildTrackCache()
        {
            trackCache.Clear();
            if (currentTimeline == null) return;
            foreach (var track in currentTimeline.AllTracks)
            {
                AddTrackToCache(track);
            }
        }

        /// <summary>
        /// 增量添加轨道到缓存
        /// </summary>
        public void AddTrackToCache(TrackBase track)
        {
            if (track == null || string.IsNullOrEmpty(track.trackId)) return;
            trackCache[track.trackId] = track;
        }

        /// <summary>
        /// 增量从缓存移除轨道
        /// </summary>
        public void RemoveTrackFromCache(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            trackCache.Remove(trackId);
        }

        /// <summary>
        /// 极速获取轨道（O(1) 复杂度）
        /// </summary>
        public TrackBase GetTrackById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (trackCache.TryGetValue(id, out TrackBase track)) return track;
            return null;
        }

        // 视口状态
        public float zoom = 100f;
        public float scrollOffset = 0f;
        public float verticalScrollOffset = 0f; // 垂直滚动支持预留
        public float timeIndicator = 0f;

        // 时间指示器状态
        //UI层标志，仅UI层可依赖
        public bool isPreviewing = false; // 是否处于鼠标拖拽时间线操作中
        public bool isStopped = true;    // 是否处于停止状态（归 0 且隐藏指示器）

        /// <summary>
        /// 是否应该显示时间指示器（红线）
        /// 规则：拖拽预览中或非停止状态时显示
        /// </summary>
        public bool ShouldShowIndicator => isPreviewing || !isStopped;

        private const string PREF_PREVIEW_SPEED = "SkillEditor_PreviewSpeed";
        private const string PREF_SNAP_ENABLED = "SkillEditor_SnapEnabled";
        private const string PREF_FRAME_RATE = "SkillEditor_FrameRate";
        private const string PREF_TIME_STEP_MODE = "SkillEditor_TimeStepMode";
        private const string PREF_DEFAULT_PREVIEW_TARGET = "SkillEditor_DefaultPreviewTarget";
        
        public string DefaultPreviewCharacterPath
        {
            get => UnityEditor.EditorPrefs.GetString(PREF_DEFAULT_PREVIEW_TARGET, "Assets/SkillEditor/Editor/Resources/DefaultPreviewCharacter.prefab");
            set => UnityEditor.EditorPrefs.SetString(PREF_DEFAULT_PREVIEW_TARGET, value);
        }

        public string Language
        {
            get => Lan.CurrentLanguage;
            set => Lan.SetLanguage(value);
        }
        public float previewSpeedMultiplier
        {
            get => UnityEditor.EditorPrefs.GetFloat(PREF_PREVIEW_SPEED, 1f);
            set => UnityEditor.EditorPrefs.SetFloat(PREF_PREVIEW_SPEED, value);
        }
        public bool snapEnabled
        {
            get => UnityEditor.EditorPrefs.GetBool(PREF_SNAP_ENABLED, true);
            set => UnityEditor.EditorPrefs.SetBool(PREF_SNAP_ENABLED, value);
        }

        public float snapThreshold = 10f;
        
        // 精度与帧控制 (使用 EditorPrefs 持久化)
        public int frameRate
        {
            get => UnityEditor.EditorPrefs.GetInt(PREF_FRAME_RATE, 30);
            set => UnityEditor.EditorPrefs.SetInt(PREF_FRAME_RATE, value);
        }

        public TimeStepMode timeStepMode
        {
            get => (TimeStepMode)UnityEditor.EditorPrefs.GetInt(PREF_TIME_STEP_MODE, (int)TimeStepMode.Variable);
            set => UnityEditor.EditorPrefs.SetInt(PREF_TIME_STEP_MODE, (int)value);
        }

        // 帧吸附现在由 TimeStepMode 自动控制
        public bool useFrameSnap
        {
            get => timeStepMode == TimeStepMode.Fixed;
        }

        public float SnapInterval
        {
            get
            {
                if (useFrameSnap && frameRate > 0)
                {
                    return 1.0f / frameRate;
                }
                return -1f; // 表示使用动态网格
            }
        }

        // 预览角色
        public GameObject previewTarget;
        public bool hasPreviewOriginPose;
        public GameObject previewOriginTarget;
        public Vector3 previewOriginPos;
        public Quaternion previewOriginRot = Quaternion.identity;
        public bool hasPreviewTrackBasePose;
        public Vector3 previewTrackBasePos;
        public Quaternion previewTrackBaseRot = Quaternion.identity;

        /// <summary>
        /// 绑定的预览播放器实例（由 Window 注入，供 Drawer 获取上下文）
        /// </summary>
        public SkillRunner previewRunner;

        /// <summary>
        /// 获取当前的 ProcessContext（用于 Drawer 请求底层服务）
        /// </summary>
        public ProcessContext PreviewContext => previewRunner?.Context;

        // 选中项状态
        public Group selectedGroup;
        public TrackBase selectedTrack;
        public List<ClipBase> selectedClips = new List<ClipBase>();
        public bool isTimelineSelected = false; // 是否选中了整个 Timeline 对象
        
        /// <summary>
        /// 获取当前主选中的片段（多选中的最后一个或单选的那一个）
        /// </summary>
        public ClipBase SelectedClip => selectedClips.Count > 0 ? selectedClips[selectedClips.Count - 1] : null;

        // 复制粘贴
        public struct CopiedClipData
        {
            public ClipBase clip;
            public string sourceTrackId;
            public int sourceTrackIndex; // 用于维持相对轨道层级
        }

        public List<CopiedClipData> copiedClipsData = new List<CopiedClipData>();
        
        // 保留旧的单项引用以维护兼容性
        public ClipBase copiedClip => copiedClipsData.Count > 0 ? copiedClipsData[0].clip : null;
        public TrackBase copiedTrack;   // 复制时的轨道（对单选仍有效）
        public Group copiedGroup;       // 复制的分组
        public List<TrackBase> copiedTracksForGroup = new List<TrackBase>(); // 分组内的轨道拷贝
        
        // 粘贴锚点（跨视图共享）
        public TrackBase pasteTargetTrack; 
        public float pasteTargetTime;
        
        /// <summary>
        /// 重置为默认视图状态
        /// </summary>
        public void ResetView()
        {
            zoom = 100f;
            scrollOffset = 0f;
            verticalScrollOffset = 0f;
        }

        /// <summary>
        /// 计算整个 Timeline 的总显示高度（遍历 groups → tracks）
        /// </summary>
        public float CalculateTotalHeight()
        {
            if (currentTimeline == null) return 0;
            
            const float TIME_RULER_HEIGHT = 30f;
            const float TRACK_HEIGHT = 40f;
            const float GROUP_HEIGHT = 30f;
            
            float totalHeight = TIME_RULER_HEIGHT;
            
            if (currentTimeline.groups != null)
            {
                for (int i = 0; i < currentTimeline.groups.Count; i++)
                {
                    var group = currentTimeline.groups[i];
                    totalHeight += GROUP_HEIGHT;
                    
                    if (!group.isCollapsed && group.tracks != null)
                    {
                        totalHeight += group.tracks.Count * TRACK_HEIGHT;
                    }
                }
            }
            
            return totalHeight;
        }

        /// <summary>
        /// 清除所有选中项
        /// </summary>
        public void ClearSelection()
        {
            selectedGroup = null;
            selectedTrack = null;
            selectedClips.Clear();
            isTimelineSelected = false;
        }

        /// <summary>
        /// 根据片段查找所属轨道（遍历 groups → tracks）
        /// </summary>
        public TrackBase GetTrackByClip(ClipBase clip)
        {
            if (currentTimeline == null || clip == null) return null;
            foreach (var track in currentTimeline.AllTracks)
            {
                if (track.clips.Contains(clip)) return track;
            }
            return null;
        }
    }
}
