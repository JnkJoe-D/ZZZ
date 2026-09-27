using System;
using UnityEngine;
using UnityEditor;

namespace Game.Editor.ActionTransition
{
    /// <summary>
    /// 独立的 3D 角色动作过渡预览视口。
    /// 基于 Unity PreviewRenderUtility 实现，完全隔离主工程场景。
    /// 包含递归层级装配、全 Layer 渲染支持、地坪参考网格、原点 XYZ 三维坐标轴以及基于 Bounds 的智能视角对焦与复位。
    /// </summary>
    public sealed class ActionTransitionPreviewViewport : IDisposable
    {
        private PreviewRenderUtility _previewUtility;
        private GameObject _previewInstance;
        private Animator _animator;
        private readonly ActionTransitionPreviewPlayer _player;

        // 相机控制参数
        private Vector3 _cameraPivot = new Vector3(0f, 0.9f, 0f);
        private float _cameraDistance = 3.0f;
        private float _cameraYaw = 160f;
        private float _cameraPitch = 12f;

        private GameObject _currentPrefab;
        private Bounds _characterBounds;
        private float _characterHeightOffset = 0.9f;

        // 镜头跟随开关（只跟位置，不跟旋转）
        public bool CameraFollowCharacter { get; set; } = true;
        public bool IsSimulateInputMode { get; set; } = false;
        public Action OnSimulateInputClicked;

        public ActionTransitionPreviewPlayer Player => _player;
        public GameObject CharacterInstance => _previewInstance;
        public Animator Animator => _animator;

        /// <summary>
        /// 重置模型位置到原点 (0, 0, 0) 并恢复初始旋转
        /// </summary>
        public void ResetCharacterTransform()
        {
            if (_previewInstance != null)
            {
                _previewInstance.transform.position = Vector3.zero;
                _previewInstance.transform.rotation = Quaternion.identity;
            }
        }

        public ActionTransitionPreviewViewport()
        {
            _player = new ActionTransitionPreviewPlayer();
            InitPreviewUtility();
        }

        private void InitPreviewUtility()
        {
            _previewUtility = new PreviewRenderUtility();
            _previewUtility.cameraFieldOfView = 36f;
            _previewUtility.camera.nearClipPlane = 0.05f;
            _previewUtility.camera.farClipPlane = 60f;
            _previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            _previewUtility.camera.backgroundColor = new Color(0.15f, 0.15f, 0.17f, 1f);

            // 关键：开启全部 Layer 剔除，确保角色专属 Layer（如 Layer 9）能够被正常渲染！
            _previewUtility.camera.cullingMask = -1;

            _previewUtility.ambientColor = new Color(0.35f, 0.35f, 0.38f, 1.0f);

            // 主方向光 (日光暖色)
            _previewUtility.lights[0].intensity = 1.35f;
            _previewUtility.lights[0].transform.rotation = Quaternion.Euler(42f, 35f, 0f);
            _previewUtility.lights[0].color = new Color(1f, 0.98f, 0.94f);

            // 辅轮廓光 (冷蓝色反向补光)
            _previewUtility.lights[1].intensity = 0.7f;
            _previewUtility.lights[1].transform.rotation = Quaternion.Euler(-25f, -145f, 0f);
            _previewUtility.lights[1].color = new Color(0.65f, 0.8f, 1.0f);
        }

        /// <summary>
        /// 设置或替换预览角色预制体（支持递归装配所有带 Renderer 的子节点）
        /// </summary>
        public void SetPreviewPrefab(GameObject prefab)
        {
            if (_currentPrefab == prefab && _previewInstance != null) return;
            _currentPrefab = prefab;

            if (_previewInstance != null)
            {
                _player.Dispose();
                UnityEngine.Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
                _animator = null;
            }

            if (prefab == null) return;

            _previewInstance = UnityEngine.Object.Instantiate(prefab);
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            _previewInstance.transform.position = Vector3.zero;
            _previewInstance.transform.rotation = Quaternion.identity;

            // 仅对根节点调用 AddSingleGO（整棵层级树会自动跟随根节点移入 PreviewScene）
            _previewUtility.AddSingleGO(_previewInstance);

            // 确保所有 SkinnedMeshRenderer 开启 updateWhenOffscreen，防止视口旋转时被裁剪剔除
            var skinnedMeshes = _previewInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in skinnedMeshes)
            {
                smr.updateWhenOffscreen = true;
            }

            // 寻找 Animator
            _animator = _previewInstance.GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _player.Initialize(_animator);
            }

            // 计算角色复合包围盒以智能对焦
            RecalculateBoundsAndFocus();
        }

        private void RecalculateBoundsAndFocus()
        {
            if (_previewInstance == null) return;

            // 优先查找 SkinnedMeshRenderer 以避免特效/粒子系统产生巨大无效包围盒导致相机被推至极远
            var skinnedRenderers = _previewInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedRenderers.Length > 0)
            {
                _characterBounds = skinnedRenderers[0].bounds;
                for (int i = 1; i < skinnedRenderers.Length; i++)
                {
                    _characterBounds.Encapsulate(skinnedRenderers[i].bounds);
                }
                _cameraPivot = _characterBounds.center;
                float height = Mathf.Max(1.0f, _characterBounds.size.y);
                _cameraDistance = Mathf.Clamp(height * 1.8f, 2.0f, 5.0f);
            }
            else
            {
                var renderers = _previewInstance.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    _characterBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        if (renderers[i] is ParticleSystemRenderer) continue;
                        _characterBounds.Encapsulate(renderers[i].bounds);
                    }
                    _cameraPivot = _characterBounds.center;
                    float height = Mathf.Max(1.0f, _characterBounds.size.y);
                    _cameraDistance = Mathf.Clamp(height * 1.8f, 2.0f, 5.0f);
                }
                else
                {
                    _cameraPivot = new Vector3(0f, 0.9f, 0f);
                    _cameraDistance = 3.0f;
                }
            }
        }

        /// <summary>
        /// 视口 GUI 渲染与相机操作处理
        /// </summary>
        public void OnGUI(Rect rect)
        {
            if (rect.width <= 1 || rect.height <= 1) return;

            Event evt = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // 相机交互处理 (Orbit, Pan, Zoom)
            HandleCameraInput(evt, rect, controlID);

            if (evt.type != EventType.Repaint) return;

            // 1. 开始离屏预览渲染
            _previewUtility.BeginPreview(rect, GUIStyle.none);

            // 2. 更新相机位置与朝向
            UpdateCameraTransform();

            // 3. 渲染场景中的所有角色网格
            _previewUtility.camera.Render();

            // 4. 在相机渲染之后绘制地面参考网格与原点 XYZ 坐标轴（防止被 camera.Render 冲刷清除）
            DrawFloorGridAndGizmos();

            // 5. 提交并绘制到 GUI
            Texture result = _previewUtility.EndPreview();
            GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);

            // 6. 视口小悬浮条（复位视角、模型名称等）
            DrawOverlayControls(rect);
        }

        private void HandleCameraInput(Event evt, Rect rect, int controlID)
        {
            if (!rect.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (evt.button == 1 || evt.button == 2 || (evt.button == 0 && evt.alt))
                    {
                        GUIUtility.hotControl = controlID;
                        evt.Use();
                    }
                    else if (evt.button == 0 && !evt.alt && IsSimulateInputMode)
                    {
                        // 需求 1：只有当前时间指针处于原动作时间内才允许响应，处于播放目标动作时不允许响应
                        if (_player != null && !_player.HasSimulatedInputTriggered && _player.CurrentTime < _player.SourceDuration)
                        {
                            OnSimulateInputClicked?.Invoke();
                            evt.Use();
                        }
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        if (evt.button == 2 || (evt.button == 0 && evt.shift))
                        {
                            // 中键或 Shift+左键：平移相机 Pivot
                            Vector3 right = _previewUtility.camera.transform.right;
                            Vector3 up = _previewUtility.camera.transform.up;
                            float panSpeed = 0.003f * _cameraDistance;
                            _cameraPivot -= (right * evt.delta.x - up * evt.delta.y) * panSpeed;
                        }
                        else
                        {
                            // 右键或 Alt+左键：旋转视口 (Orbit)
                            _cameraYaw += evt.delta.x * 0.7f;
                            _cameraPitch = Mathf.Clamp(_cameraPitch + evt.delta.y * 0.7f, -80f, 85f);
                        }
                        evt.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    _cameraDistance = Mathf.Clamp(_cameraDistance + evt.delta.y * 0.25f, 0.6f, 15f);
                    evt.Use();
                    break;
            }
        }

        private void UpdateCameraTransform()
        {
            Vector3 targetPivot = _cameraPivot;
            if (CameraFollowCharacter && _previewInstance != null)
            {
                // 相机跟随模型位置（仅跟位置，不跟旋转，防止晃晕）
                targetPivot = _previewInstance.transform.position + new Vector3(0f, _characterHeightOffset, 0f);
            }

            Quaternion rot = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
            Vector3 camPos = targetPivot - (rot * Vector3.forward * _cameraDistance);
            _previewUtility.camera.transform.position = camPos;
            _previewUtility.camera.transform.rotation = rot;
        }

        private void DrawFloorGridAndGizmos()
        {
            Handles.SetCamera(_previewUtility.camera);

            // 启用深度测试 (LessEqual)，确保角色模型网格完全遮挡地面辅助线，角色渲染在最前端
            UnityEngine.Rendering.CompareFunction prevZTest = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            try
            {
                // 地面略低于原点 0.001m，防止与角色脚底产生 Z-Fighting
                float groundY = -0.001f;

                // 1. 获取网格跟随中心（以角色当前世界位置为中心，若无角色则以相机注视点为中心）
                Vector3 center = _previewInstance != null ? _previewInstance.transform.position : _cameraPivot;
                center.y = groundY;

                // 2. 动态自适应双层地坪网格（类似 Unity AnimationClip 预览视口，大范围动态铺设，边缘自然渐隐衰减）
                float gridRadius = 18.0f; // 半径 18m，直径 36m，覆盖整个动作位移视野
                float minorStep = 0.5f;
                float majorStep = 2.0f;

                float startX = Mathf.Floor((center.x - gridRadius) / minorStep) * minorStep;
                float endX = Mathf.Ceil((center.x + gridRadius) / minorStep) * minorStep;
                float startZ = Mathf.Floor((center.z - gridRadius) / minorStep) * minorStep;
                float endZ = Mathf.Ceil((center.z + gridRadius) / minorStep) * minorStep;

                // 绘制平行于 Z 轴的网格线 (固定 X)
                for (float x = startX; x <= endX; x += minorStep)
                {
                    float dx = Mathf.Abs(x - center.x);
                    if (dx > gridRadius) continue;

                    float falloff = 1.0f - (dx / gridRadius);
                    float lineLen = Mathf.Sqrt(Mathf.Max(0f, gridRadius * gridRadius - dx * dx));
                    if (lineLen < 0.5f) continue;

                    bool isMajor = Mathf.Abs(x % majorStep) < 0.01f || Mathf.Abs(Mathf.Abs(x % majorStep) - majorStep) < 0.01f;
                    float alpha = isMajor ? 0.35f * falloff : 0.12f * falloff;
                    if (alpha < 0.015f) continue;

                    Color gridColor = isMajor 
                        ? new Color(0.42f, 0.48f, 0.6f, alpha) 
                        : new Color(0.3f, 0.35f, 0.45f, alpha);
                    Handles.color = gridColor;

                    float z1 = center.z - lineLen;
                    float z2 = center.z + lineLen;
                    Handles.DrawLine(new Vector3(x, groundY, z1), new Vector3(x, groundY, z2));
                }

                // 绘制平行于 X 轴的网格线 (固定 Z)
                for (float z = startZ; z <= endZ; z += minorStep)
                {
                    float dz = Mathf.Abs(z - center.z);
                    if (dz > gridRadius) continue;

                    float falloff = 1.0f - (dz / gridRadius);
                    float lineLen = Mathf.Sqrt(Mathf.Max(0f, gridRadius * gridRadius - dz * dz));
                    if (lineLen < 0.5f) continue;

                    bool isMajor = Mathf.Abs(z % majorStep) < 0.01f || Mathf.Abs(Mathf.Abs(z % majorStep) - majorStep) < 0.01f;
                    float alpha = isMajor ? 0.35f * falloff : 0.12f * falloff;
                    if (alpha < 0.015f) continue;

                    Color gridColor = isMajor 
                        ? new Color(0.42f, 0.48f, 0.6f, alpha) 
                        : new Color(0.3f, 0.35f, 0.45f, alpha);
                    Handles.color = gridColor;

                    float x1 = center.x - lineLen;
                    float x2 = center.x + lineLen;
                    Handles.DrawLine(new Vector3(x1, groundY, z), new Vector3(x2, groundY, z));
                }

                // 3. 世界原点 (0, 0, 0) 同心基准圆环
                Vector3 originGround = new Vector3(0f, groundY, 0f);
                float[] rings = new float[] { 1.0f, 2.0f, 3.0f, 5.0f, 8.0f, 12.0f };
                for (int i = 0; i < rings.Length; i++)
                {
                    float r = rings[i];
                    float ringAlpha = Mathf.Clamp01(0.45f - i * 0.06f);
                    Handles.color = new Color(0.35f, 0.45f, 0.6f, ringAlpha);
                    Handles.DrawWireDisc(originGround, Vector3.up, r);
                }

                // 4. 世界原点三维坐标基准轴 (红=X, 绿=Y, 蓝=Z)
                float axisLen = 0.8f;
                Handles.color = new Color(1f, 0.25f, 0.25f, 0.95f); // X 轴
                Handles.DrawLine(originGround, new Vector3(axisLen, groundY, 0f));
                Handles.DrawSolidDisc(new Vector3(axisLen, groundY, 0f), Vector3.up, 0.025f);

                Handles.color = new Color(0.25f, 1f, 0.25f, 0.95f); // Y 轴
                Handles.DrawLine(originGround, new Vector3(0f, axisLen + groundY, 0f));
                Handles.DrawSolidDisc(new Vector3(0f, axisLen + groundY, 0f), Vector3.forward, 0.025f);

                Handles.color = new Color(0.25f, 0.55f, 1f, 0.95f); // Z 轴
                Handles.DrawLine(originGround, new Vector3(0f, groundY, axisLen));
                Handles.DrawSolidDisc(new Vector3(0f, groundY, axisLen), Vector3.up, 0.025f);

                // 5. 角色当前脚底微弱定位投影光圈（若存在位移）
                if (_previewInstance != null && _previewInstance.transform.position.sqrMagnitude > 0.01f)
                {
                    Handles.color = new Color(0.3f, 0.85f, 1f, 0.35f);
                    Handles.DrawWireDisc(center, Vector3.up, 0.35f);
                    Handles.color = new Color(0.3f, 0.85f, 1f, 0.15f);
                    Handles.DrawLine(originGround, center);
                }
            }
            finally
            {
                Handles.zTest = prevZTest;
            }
        }

        private void DrawOverlayControls(Rect rect)
        {
            // 复位模型按钮
            Rect resetModelBtn = new Rect(rect.x + 10f, rect.y + 10f, 75f, 22f);
            if (GUI.Button(resetModelBtn, "复位模型", EditorStyles.miniButton))
            {
                ResetCharacterTransform();
            }

            // 复位视角按钮
            Rect resetCamBtn = new Rect(rect.x + 90f, rect.y + 10f, 75f, 22f);
            if (GUI.Button(resetCamBtn, "复位视角", EditorStyles.miniButton))
            {
                ResetCamera();
            }

            if (_previewInstance != null)
            {
                GUIStyle modelTipStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.7f, 0.8f, 0.9f, 0.8f) }
                };
                GUI.Label(new Rect(rect.x + 175f, rect.y + 12f, 200f, 18f), $"模型: {_previewInstance.name.Replace("(Clone)", "")}", modelTipStyle);
            }
            else
            {
                Rect warnRect = new Rect(rect.x + 10f, rect.y + 38f, rect.width - 20f, 24f);
                EditorGUI.HelpBox(warnRect, "未指定预览模型 Prefab，请在左侧拖入角色预制体以查看 3D 姿态", MessageType.Info);
            }

            // 模拟输入模式下的视口右下角半透明提示文字
            if (IsSimulateInputMode)
            {
                bool canTrigger = _player != null && !_player.HasSimulatedInputTriggered && _player.CurrentTime < _player.SourceDuration;
                float tipW = 275f;
                float tipH = 26f;
                Rect tipRect = new Rect(rect.xMax - tipW - 10f, rect.yMax - tipH - 10f, tipW, tipH);
                EditorGUI.DrawRect(tipRect, canTrigger ? new Color(0.12f, 0.16f, 0.22f, 0.88f) : new Color(0.18f, 0.18f, 0.20f, 0.88f));
                Handles.color = canTrigger ? new Color(0.35f, 0.65f, 1f, 0.85f) : new Color(0.5f, 0.5f, 0.55f, 0.6f);
                Handles.DrawWireCube(tipRect.center, tipRect.size);

                GUIStyle tipStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    normal = { textColor = canTrigger ? new Color(0.4f, 0.85f, 1f, 0.95f) : new Color(0.7f, 0.7f, 0.75f, 0.85f) }
                };
                string tipText = canTrigger ? "[模拟输入] 单击视口模拟按键切换" : "[目标动作播放中] 等待回绕复位";
                GUI.Label(tipRect, tipText, tipStyle);
            }
        }

        public void ResetCamera()
        {
            RecalculateBoundsAndFocus();
            _cameraYaw = 160f;
            _cameraPitch = 12f;
        }

        public void Dispose()
        {
            _player.Dispose();

            if (_previewInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            if (_previewUtility != null)
            {
                _previewUtility.Cleanup();
                _previewUtility = null;
            }
        }
    }
}
