using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 全局存储技能编辑器的样式、布局常量与颜色的静态类
    /// 若需美化界面或调整尺寸，请统一修改此处。
    /// </summary>
    public static class SkillEditorStyles
    {
        // ==================== 尺寸与布局常量 ====================
        
        /// <summary> 时间标尺区域的高度 </summary>
        public const float TIME_RULER_HEIGHT = 30f;
        
        /// <summary> 每条轨道项的高度 </summary>
        public const float TRACK_HEIGHT = 40f;
        
        /// <summary> 每个分组头部项的高度 </summary>
        public const float GROUP_HEIGHT = 30f;
        
        /// <summary> 左侧轨道列表区域的总宽度 </summary>
        public const float TRACK_LIST_WIDTH = 200f;
        
        /// <summary> 右侧时间轴内片段区块的高度 </summary>
        public const float CLIP_HEIGHT = 36f;
        
        /// <summary> 片段区块距离轨道顶部的边距（让它上下居中一点） </summary>
        public const float CLIP_MARGIN_TOP = 2f;
        
        /// <summary> 顶部/标题栏的通用高度 </summary>
        public const float HEADER_HEIGHT = 30f;

        // ==================== 视图设置常量 ====================
        
        public const float MIN_ZOOM = 10f;
        public const float MAX_ZOOM = 6000f;

        // ==================== 颜色与画笔样式 (Color) ====================
        
        /// <summary> 被选中片段的高亮边框颜色 </summary>
        public static readonly Color ClipSelectedBorderColor = Color.white;
        
        /// <summary> 片段默认状态下的边框颜色 </summary>
        public static readonly Color ClipDefaultBorderColor = Color.black;
        
        /// <summary> 处于禁用状态下的片段正片叠底颜色 </summary>
        public static readonly Color ClipDisabledOverlayColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        
        /// <summary> 辅助框选框内部填充色 </summary>
        public static readonly Color BoxSelectFillColor = new Color(0.2f, 0.4f, 0.8f, 0.2f);
        
        /// <summary> 辅助框选框外部边框颜色 </summary>
        public static readonly Color BoxSelectBorderColor = new Color(0.8f, 0.8f, 1f, 0.8f);

        /// <summary> 全局深邃背景底色（轨道背景以外的空余部分） </summary>
        public static readonly Color GlobalBackgroundColor = new Color(0.1607843f, 0.1607843f, 0.1607843f);
        
        /// <summary> 时间轴标题与标尺区底色 </summary>
        public static readonly Color HeaderBackgroundColor = new Color(0.18f, 0.18f, 0.18f);
        
        /// <summary> 轨道基础底色（偶数行） </summary>
        public static readonly Color TrackBgOdd = new Color(0.15f, 0.2f, 0.3f);
        
        /// <summary> 轨道深层底色（奇数行） </summary>
        public static readonly Color TrackBgEven = new Color(0.18f, 0.23f, 0.33f);
        
        /// <summary> 左侧轨道列表独立轨道项普通态底色 </summary>
        public static readonly Color TrackListNormalBg = new Color(0.2f, 0.2f, 0.2f);
        
        /// <summary> 轨道被选中时的高亮加强色 </summary>
        public static readonly Color TrackSelectedBg = new Color(0.25f, 0.35f, 0.55f);
        
        /// <summary> 轨道被悬停/拖拽预览时的高亮色 </summary>
        public static readonly Color TrackHoveredBg = new Color(0.3f, 0.5f, 0.8f, 0.4f);
        
        /// <summary> 轨道之间的细微黑线间隔色 </summary>
        public static readonly Color TrackSeparatorColor = new Color(0.1f, 0.1f, 0.1f);
        
        /// <summary> 轨道组别栏正常背景色 </summary>
        public static readonly Color GroupNormalBg = new Color(0.15f, 0.15f, 0.15f);
        
        /// <summary> Timeline画面的结束界限指示线颜色 </summary>
        public static readonly Color TimelineEndLineColor = new Color(0.3f, 0.5f, 1.0f, 0.8f);
        
        /// <summary> 正在播放的当前时间指针红线指示器颜色 </summary>
        public static readonly Color TimeIndicatorColor = Color.red;
        
        /// <summary> 片段融合过渡边缘（斜线区域内部）半透明灰膜 </summary>
        public static readonly Color BlendAreaFillColor = new Color(0f, 0f, 0f, 0.25f);
        
        /// <summary> 片段融合斜线描边 </summary>
        public static readonly Color BlendAreaLineColor = new Color(0f, 0f, 0f, 0.4f);
    }
}
