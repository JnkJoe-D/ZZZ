using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Presentation.VFX.CrossFlash
{
    /// <summary>
    /// 全屏十字闪光 URP 渲染通道。
    /// 使用 CoreUtils.DrawFullScreen 将动态十字以 HDR Additive 方式叠加至摄像机最终画面。
    /// </summary>
    public class CrossFlashRenderPass : ScriptableRenderPass
    {
        public static int LastRenderedFrame { get; private set; } = -1;

        private readonly Material _passMaterial;
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("CrossScreenFlashPass");

        public CrossFlashRenderPass(Material passMaterial, RenderPassEvent injectionPoint)
        {
            _passMaterial = passMaterial;
            renderPassEvent = injectionPoint;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_passMaterial == null) return;

            LastRenderedFrame = Time.frameCount;

            ref var cameraData = ref renderingData.cameraData;
            if (cameraData.isPreviewCamera) return;

            CommandBuffer cmd = CommandBufferPool.Get("CrossScreenFlashPass");
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // 目标为当前相机的颜色渲染目标，直接执行全屏三角形绘制 (Additive 混合无需前置抓屏拷贝)
                CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle);
                CoreUtils.DrawFullScreen(cmd, _passMaterial);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
