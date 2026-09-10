using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.Combat.Pipeline
{
    /// <summary>
    /// 命中流水线微内核调度引擎
    /// </summary>
    public class HitPipeline
    {
        private static HitPipeline _defaultPipeline;
        public static HitPipeline Default => _defaultPipeline ??= CreateDefaultPipeline();

        private readonly List<IHitPipe> _pipes = new();
        private readonly Stack<HitPipelineContext> _contextPool = new();

        /// <summary>
        /// 注册过滤器并自动按优先级排序
        /// </summary>
        public HitPipeline RegisterPipe(IHitPipe pipe)
        {
            if (pipe == null) return this;
            _pipes.Add(pipe);
            _pipes.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            return this;
        }

        /// <summary>
        /// 执行命中流水线
        /// </summary>
        public void Execute(HitPipelineContext ctx)
        {
            if (ctx == null) return;

            for (int i = 0; i < _pipes.Count; i++)
            {
                var pipe = _pipes[i];

                // 短路机制：一旦标记中断，仅允许处理招架特化的反馈管道继续呈现
                if (ctx.IsAborted)
                {
                    if (ctx.ResultFlags.HasFlag(HitResultFlags.Parried) && pipe.Priority >= 600)
                    {
                        pipe.Process(ctx);
                    }
                    continue;
                }

                pipe.Process(ctx);
            }
        }

        /// <summary>
        /// 从对象池获取一个干净的上下文（零 GC）
        /// </summary>
        public HitPipelineContext AllocateContext()
        {
            if (_contextPool.Count > 0)
            {
                var ctx = _contextPool.Pop();
                ctx.Reset();
                return ctx;
            }
            return new HitPipelineContext();
        }

        /// <summary>
        /// 归还上下文至对象池
        /// </summary>
        public void ReleaseContext(HitPipelineContext ctx)
        {
            if (ctx == null) return;
            ctx.Reset();
            _contextPool.Push(ctx);
        }

        /// <summary>
        /// 创建并装配标准默认命中流水线
        /// </summary>
        public static HitPipeline CreateDefaultPipeline()
        {
            var pipeline = new HitPipeline();
            pipeline.RegisterPipe(new Pipes.ProtectionPipe());
            pipeline.RegisterPipe(new Pipes.ParryPipe());
            pipeline.RegisterPipe(new Pipes.DamageCalculationPipe());
            pipeline.RegisterPipe(new Pipes.ResilienceAndStaggerPipe());
            pipeline.RegisterPipe(new Pipes.MotionAndActionPipe());
            pipeline.RegisterPipe(new Pipes.FeedbackPresentationPipe());
            return pipeline;
        }
    }
}
