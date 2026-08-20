using UnityEngine;
using NPBehave;

namespace Game.Logic.AI.BehaviorTree.Extensions
{
    public class MultiFrameDebugTask : Task
    {
        private float _duration;
        private string _message;
        
        // 局部环境隔离：当前计时的状态
        private float _timeRemaining;

        public MultiFrameDebugTask(float duration, string message) : base("MultiFrameDebug")
        {
            _duration = duration;
            _message = message;
        }

        protected override void DoStart()
        {
            // 开始时的逻辑
            _timeRemaining = _duration;
            Debug.Log($"[MultiFrameDebugTask Start] {_message} (需持续: {_duration}s)");

            // 注册每帧的 Update 回调给 NPBehave 时钟
            RootNode.Clock.AddUpdateObserver(Tick);
        }

        private void Tick()
        {
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0)
            {
                Debug.Log($"[MultiFrameDebugTask Finish] {_message}");
                
                // 【关键步骤】必须在成功时注销监听，并用 Stopped(true) 告诉父节点自己已经圆满完成！
                RootNode.Clock.RemoveUpdateObserver(Tick);
                Stopped(true);
            }
        }

        protected override void DoStop()
        {
            // 【打断处理】当节点被行为树更上层的条件/优先级强行切断时会走这里
            Debug.LogWarning($"[MultiFrameDebugTask Interrupted] {_message} 被行为树强制打断了！");
            
            // 清理注册的回调
            RootNode.Clock.RemoveUpdateObserver(Tick);
            
            // 响应打断请求，一般认为本次执行失败
            Stopped(false);
        }
    }
}
