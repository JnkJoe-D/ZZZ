namespace ATEditor
{
    /// <summary>
    /// 用于在动作编辑器中执行极限支援（如弹刀或极限闪避）瞬移贴脸的处理器接口。
    /// RuntimeProcess 将调用此接口，而具体的业务逻辑由实现了该接口的外部类负责，保证核心数据流解耦。
    /// </summary>
    public interface IAssistHandler
    {
        void ExecuteAssistTeleport();
    }
}
