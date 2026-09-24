namespace ATEditor
{
    /// <summary>
    /// 路由窗口生命周期调度服务接口。
    /// 由时间轴 RuntimeRouteWindowProcess 主动调用，驱动 ActionController 内部对应的 RouteWindowProcesser。
    /// </summary>
    public interface IRouteWindowHandler : IService
    {
        /// <summary> 窗口开启（时间轴到达 StartTime） </summary>
        void OnWindowEnter(RouteWindow routeWindow);

        /// <summary> 窗口活跃帧推进（由时间轴 OnUpdate 主动驱动） </summary>
        void OnWindowProcess(RouteWindow routeWindow);

        /// <summary> 窗口自然退出（时间轴自然越过 EndTime，触发结算并推入 L2） </summary>
        void OnWindowExit(RouteWindow routeWindow);

        /// <summary> 窗口被打断注销（技能切换、受击强切打断：只退出注销，绝不执行评估，绝不推入 L2） </summary>
        void OnWindowDisable(RouteWindow routeWindow);
    }
}
