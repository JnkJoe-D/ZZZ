using Game.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 切人管线微内核调度引擎
    /// </summary>
    public class SwitchPipeline
    {
        private static SwitchPipeline _defaultPipeline;
        public static SwitchPipeline Default => _defaultPipeline ??= CreateDefaultPipeline();

        private readonly List<ISwitchPipe> _pipes = new();
        private readonly Stack<SwitchPipelineContext> _contextPool = new();

        /// <summary>
        /// 注册过滤器并自动按优先级升序排序
        /// </summary>
        public SwitchPipeline RegisterPipe(ISwitchPipe pipe)
        {
            if (pipe == null) return this;
            _pipes.Add(pipe);
            _pipes.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            return this;
        }

        /// <summary>
        /// 执行切人流水线
        /// </summary>
        public void Execute(SwitchPipelineContext ctx)
        {
            if (ctx == null) return;

            for (int i = 0; i < _pipes.Count; i++)
            {
                var pipe = _pipes[i];

                if (ctx.IsAborted)
                {
                    GLog.Info(LogTags.Team, $"管线于 {pipe.PipeName} 前中断，原因: {ctx.AbortReason}");
                    break;
                }

                pipe.Process(ctx);
            }
        }

        /// <summary>
        /// 从对象池获取干净上下文（零 GC 开销）
        /// </summary>
        public SwitchPipelineContext AllocateContext()
        {
            if (_contextPool.Count > 0)
            {
                var ctx = _contextPool.Pop();
                ctx.Reset();
                return ctx;
            }
            return new SwitchPipelineContext();
        }

        /// <summary>
        /// 归还上下文至对象池
        /// </summary>
        public void ReleaseContext(SwitchPipelineContext ctx)
        {
            if (ctx == null) return;
            ctx.Reset();
            _contextPool.Push(ctx);
        }

        /// <summary>
        /// 装配默认标准切人管线
        /// </summary>
        public static SwitchPipeline CreateDefaultPipeline()
        {
            var pipeline = new SwitchPipeline();
            pipeline.RegisterPipe(new SwitchValidationPipe());
            pipeline.RegisterPipe(new TimeAndCameraPresentationPipe());
            pipeline.RegisterPipe(new OutgoingHandlingPipe());
            pipeline.RegisterPipe(new IncomingPlacementPipe());
            pipeline.RegisterPipe(new ControlAndAuthorityHandoffPipe());
            pipeline.RegisterPipe(new ActionAndInvincibleTriggerPipe());
            return pipeline;
        }
    }
}
