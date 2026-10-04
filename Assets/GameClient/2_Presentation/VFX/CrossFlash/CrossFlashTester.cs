using UnityEngine;
using Game.GamePlay;
using ATEditor;

namespace Game.Presentation.VFX.CrossFlash
{
    /// <summary>
    /// 全屏十字闪光独立 Mono 测试组件。
    /// 允许挂载在任意单独场景中的 GameObject 上：
    /// 1. 支持拖拽一个空对象作为生成中心点；
    /// 2. 支持在 Inspector 面板中任意微调所有效果形态与颜色参数；
    /// 3. 使用传统 Input.GetKeyDown(KeyCode.Space) 监听空格键多次连续生成；
    /// 4. 支持快捷键 C 或面板按钮一键清空所有闪光；
    /// 5. 支持常驻预览模式 (Loop / Freeze)，方便慢调参数。
    /// </summary>
    [DisallowMultipleComponent]
    public class CrossFlashTester : MonoBehaviour
    {
        [Header("===== 目标与中心点 =====")]
        [Tooltip("十字闪光绑定的目标对象 (为空时默认当前 GameObject)")]
        [SerializeField]
        private Transform _targetCenter;

        [Tooltip("相对于中心点的世界空间位置偏移")]
        [SerializeField]
        private Vector3 _positionOffset = Vector3.zero;

        [Header("===== 效果参数 (支持面板实时调试) =====")]
        [SerializeField]
        private CrossFlashParameters _parameters = CrossFlashParameters.Default;

        [Header("===== 调试模式 =====")]
        [Tooltip("常驻预览模式 (开启后十字闪光将按固定进度常驻屏幕，不自动倒计时结束，极大方便调参)")]
        [SerializeField]
        private bool _constantPreview = false;

        [Tooltip("常驻预览时的进度采样值 (0 为刚触发，0.12 为最亮极值，1 为淡出完成)")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _previewProgress = 0.12f;

        [Tooltip("是否在屏幕左上角显示可视化操作面板 (GUI)")]
        [SerializeField]
        private bool _showOnScreenGUI = true;

        private void Start()
        {
            // 确保总管理器已初始化，自动装配全屏后处理通道与兜底管线
            CrossFlashManager.Instance.Initialize();
        }

        private void Update()
        {
            // 1. 传统 Input 监听空格按键连续触发多次闪光
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                TriggerFlash();
            }

            // 2. 传统 Input 监听 C 键一键清除
            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
            {
                ClearAllFlashes();
            }

            // 3. 常驻预览模式逻辑
            if (_constantPreview)
            {
                EnsureConstantPreview();
            }
        }

        /// <summary>
        /// 触发一次新的十字闪光 (支持连续调用，最多 8 实例并发)
        /// </summary>
        [ContextMenu("触发十字闪光 (Space)")]
        public void TriggerFlash()
        {
            CrossFlashManager.Instance.Initialize();

            Transform target = _targetCenter != null ? _targetCenter : transform;
            Vector3 worldPos = target.position + _positionOffset;

            CrossFlashManager.Instance.Play(worldPos, _parameters.FollowTarget ? target : null, _positionOffset, _parameters);
        }

        /// <summary>
        /// 一键清除当前所有正在播放的十字闪光
        /// </summary>
        [ContextMenu("清空所有闪光 (C)")]
        public void ClearAllFlashes()
        {
            CrossFlashManager.Instance.Clear();
        }

        private void EnsureConstantPreview()
        {
            CrossFlashManager.Instance.Initialize();

            Transform target = _targetCenter != null ? _targetCenter : transform;
            Vector3 worldPos = target.position + _positionOffset;

            var controller = CrossFlashManager.Instance.Controller;
            if (controller == null) return;

            // 构造超长时长的参数，并强制设置进度
            var previewParams = _parameters;
            previewParams.Duration = 99999f;

            CrossFlashManager.Instance.Clear();
            controller.SpawnFlash(worldPos, previewParams.FollowTarget ? target : null, _positionOffset, previewParams);
        }

        private void OnGUI()
        {
            if (!_showOnScreenGUI) return;

            GUILayout.BeginArea(new Rect(15, 15, 340, 240), GUI.skin.box);
            {
                GUILayout.Label("<size=14><b>【全屏十字闪光测试工具】</b></size>");
                GUILayout.Space(4);

                Transform target = _targetCenter != null ? _targetCenter : transform;
                Vector3 worldPos = target.position + _positionOffset;
                Camera cam = Camera.main;
                Vector3 vp = cam != null ? cam.WorldToViewportPoint(worldPos) : Vector3.zero;

                GUILayout.Label($"中心目标: {(target != null ? target.name : "None")}");
                GUILayout.Label($"中心世界坐标: {worldPos:F2}");
                GUILayout.Label($"屏幕视口坐标: ({vp.x:F3}, {vp.y:F3}, z={vp.z:F2})");
                GUILayout.Label($"状态: {(vp.z > 0.05f ? "<color=green>在视锥内可见</color>" : "<color=red>在相机后方被剔除</color>")}");
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("触发闪光 [Space]", GUILayout.Height(32)))
                {
                    TriggerFlash();
                }
                if (GUILayout.Button("一键清除 [C]", GUILayout.Height(32)))
                {
                    ClearAllFlashes();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                _constantPreview = GUILayout.Toggle(_constantPreview, " 常驻预览模式 (不自动消失，便于调参)");
                if (_constantPreview)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("预览进度:", GUILayout.Width(70));
                    _previewProgress = GUILayout.HorizontalSlider(_previewProgress, 0f, 1f);
                    GUILayout.Label($"{_previewProgress:F2}", GUILayout.Width(35));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            Transform target = _targetCenter != null ? _targetCenter : transform;
            Vector3 worldPos = target.position + _positionOffset;

            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(worldPos, 0.2f);
            Gizmos.DrawLine(worldPos - Vector3.right * 0.5f, worldPos + Vector3.right * 0.5f);
            Gizmos.DrawLine(worldPos - Vector3.up * 0.5f, worldPos + Vector3.up * 0.5f);
            Gizmos.DrawLine(worldPos - Vector3.forward * 0.5f, worldPos + Vector3.forward * 0.5f);

            if (_targetCenter != null && _positionOffset != Vector3.zero)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(_targetCenter.position, worldPos);
            }
        }
    }
}
