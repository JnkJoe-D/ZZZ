using UnityEngine;
using UnityEngine.Rendering;
using Game.Framework;
using Game.GamePlay;

namespace Game.Presentation.VFX.CrossFlash
{
    /// <summary>
    /// 全屏十字闪光表现层驱动组件。
    /// 监听 CrossFlashTriggeredEvent 领域事件，自顶向下推进闪光实例，
    /// 并将多点视口投影数据同步至 URP 后处理材质。
    /// </summary>
    [DisallowMultipleComponent]
    public class CrossFlashPresenterComponent : MonoBehaviour
    {
        private static CrossFlashPresenterComponent _instance;
        public static CrossFlashPresenterComponent Instance => _instance;

        [SerializeField]
        private Material _material;

        [SerializeField]
        private Camera _camera;

        private readonly CrossFlashController _controller = new CrossFlashController();

        public CrossFlashController Controller => _controller;

        public void Initialize()
        {
            EnsureMaterial();
            EnsureCamera();
        }

        public void Shutdown()
        {
            _controller.Clear();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureMaterial();
            EnsureCamera();
        }

        private void OnEnable()
        {
            EventCenter.Subscribe<CrossFlashTriggeredEvent>(OnCrossFlashTriggered);
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            EventCenter.Unsubscribe<CrossFlashTriggeredEvent>(OnCrossFlashTriggered);
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            _controller.Clear();
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            // 如果本帧已由 URP Renderer Feature 渲染，则无需兜底，避免重复绘制
            if (CrossFlashRenderPass.LastRenderedFrame == Time.frameCount) return;
            if (!CrossFlashController.HasActiveFlashes) return;
            if (_material == null) return;
            if (cam != _camera && cam != Camera.main) return;

            // 零配置全屏绘制兜底（用于独立测试场景或未配置 Renderer Feature 的情况）
            var cmd = CommandBufferPool.Get("CrossScreenFlash_Fallback");
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3, 1);
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            CommandBufferPool.Release(cmd);
        }

        private void LateUpdate()
        {
            EnsureCamera();
            EnsureMaterial();

            if (_material == null || _camera == null) return;

            // 使用 unscaledDeltaTime，具体实例按自身有效流速推进
            _controller.Update(Time.unscaledDeltaTime, _camera, _material);
        }

        private void OnCrossFlashTriggered(CrossFlashTriggeredEvent evt)
        {
            _controller.SpawnFlash(evt.WorldPosition, evt.TargetTransform, evt.PositionOffset, evt.Parameters, evt.Attacker?.Clock);
        }

        private void EnsureCamera()
        {
            if (_camera == null || !_camera.gameObject.activeInHierarchy)
            {
                _camera = Camera.main;
            }
        }

        private void EnsureMaterial()
        {
            if (CrossFlashController.ActiveMaterial != null)
            {
                _material = CrossFlashController.ActiveMaterial;
                return;
            }

            if (_material == null)
            {
                var shader = Shader.Find("Game/VFX/CrossScreenFlash");
                if (shader != null)
                {
                    _material = new Material(shader) { name = "M_CrossScreenFlash_Runtime" };
                    CrossFlashController.ActiveMaterial = _material;
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            _controller.Clear();
        }
    }
}
