using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Presentation.VFX.CrossFlash
{
    /// <summary>
    /// 全屏十字闪光 URP Renderer Feature。
    /// 挂载于 Universal Renderer Data，在后处理之后将十字闪光叠加到画面上。
    /// 当无活跃闪光时自动 Bypass，杜绝无意义的全屏绘制开销。
    /// </summary>
    public class CrossFlashRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private Material _passMaterial;

        [SerializeField]
        private RenderPassEvent _injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

        private CrossFlashRenderPass _renderPass;

        public Material PassMaterial => _passMaterial;

        public override void Create()
        {
            if (_passMaterial == null)
            {
                var shader = Shader.Find("Game/VFX/CrossScreenFlash");
                if (shader != null)
                {
                    _passMaterial = CoreUtils.CreateEngineMaterial(shader);
                }
            }

            if (_passMaterial != null)
            {
                CrossFlashController.ActiveMaterial = _passMaterial;
                _renderPass = new CrossFlashRenderPass(_passMaterial, _injectionPoint);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_renderPass == null || _passMaterial == null) return;

            // 零开销特性：无活跃闪光时直接不将 Pass 入队
            if (!CrossFlashController.HasActiveFlashes)
            {
                return;
            }

            renderer.EnqueuePass(_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            // 清理动态创建的材质
            if (disposing && _passMaterial != null && _passMaterial.name.Contains("(Clone)"))
            {
                CoreUtils.Destroy(_passMaterial);
            }
        }
    }
}
