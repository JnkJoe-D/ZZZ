namespace ATEditor.Editor
{
    /// <summary>
    /// 轨道列表拖拽类型
    /// </summary>
    public enum TrackListDragType 
    { 
        None, 
        Track, 
        Group 
    }

    /// <summary>
    /// 时间轴片段拖拽模式
    /// </summary>
    public enum ClipDragMode
    {
        None,
        MoveClip,
        ResizeLeft,
        ResizeRight,
        CrossTrackDrag,
        BlendIn,
        BlendOut
    }

    /// <summary>
    /// 时间步长模式
    /// </summary>
    public enum TimeStepMode
    {
        Variable = 0, // 使用动态网格（基于缩放级别自动调整）
        Fixed = 1     // 使用固定帧率网格（基于 frameRate 属性）
    }
}
