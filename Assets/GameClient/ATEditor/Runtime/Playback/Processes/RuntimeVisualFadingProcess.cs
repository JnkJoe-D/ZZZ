using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(VisualFadingClip), PlayMode.Runtime | PlayMode.EditorPreview)]
    public class RuntimeVisualFadingProcess : ProcessBase<VisualFadingClip>
    {
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;

        public override void OnEnable()
        {
            if (context.Owner == null) return;
            
            // _mpb = new MaterialPropertyBlock();
            
            // Find visual root
            Transform visualRoot = null;
            Transform[] allChildren = context.Owner.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.CompareTag("CharacterVisual"))
                {
                    visualRoot = child;
                    break;
                }
            }
            
            if (visualRoot == null)
            {
                visualRoot = context.OwnerTransform.Find("Visual");
            }
            
            // 如果实在找不到，就用自身的根节点作为起点
            if (visualRoot == null)
            {
                visualRoot = context.OwnerTransform;
            }

            if (visualRoot != null)
            {
                if (clip.TargetType == RendererTargetType.SkinnedMeshRenderer)
                {
                    _renderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                }
                else if (clip.TargetType == RendererTargetType.MeshRenderer)
                {
                    _renderers = visualRoot.GetComponentsInChildren<MeshRenderer>(true);
                }
                else
                {
                    _renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
                }
                
                // Debug 输出以便验证获取到了多少个组件
                // Debug.Log($"[VisualFadingProcess] 在 '{visualRoot.name}' 下递归找到了 {_renderers.Length} 个 {clip.TargetType} 组件.");
            }
        }

        public override void OnEnter()
        {
            ApplyVisibility(clip.Inverse); // 正常是进入隐藏
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 旧逻辑注释掉
            // if (_renderers == null || _renderers.Length == 0) return;
            // float timeInClip = currentTime - clip.StartTime;
            // float alpha = GetAlphaAtTime(timeInClip);
            // ApplyAlpha(alpha);
        }

        public override void OnSeek(float targetTime)
        {
            // 只要在片段内，就保持应用状态
            ApplyVisibility(clip.Inverse);
        }

        private void ApplyVisibility(bool visible)
        {
            if (_renderers == null) return;
            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.gameObject.SetActive(visible);
                }
            }
        }

        /*
        private void ApplyAlpha(float alpha)
        {
            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(_mpb);
                    
                    bool changed = false;
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var mat = renderer.sharedMaterials[i];
                        if (mat == null) continue;
                        
                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor");
                            c.a = alpha;
                            _mpb.SetColor("_BaseColor", c);
                            changed = true;
                        }
                        else if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.GetColor("_Color");
                            c.a = alpha;
                            _mpb.SetColor("_Color", c);
                            changed = true;
                        }
                    }
                    
                    if (changed)
                    {
                        renderer.SetPropertyBlock(_mpb);
                    }
                }
            }
        }

        private float GetAlphaAtTime(float t)
        {
            float alpha = 1f;
            
            if (t <= clip.BlendInDuration && clip.BlendInDuration > 0)
            {
                float normalizedTime = t / clip.BlendInDuration;
                alpha = clip.BlendInCurve.Evaluate(normalizedTime);
            }
            else if (t >= clip.Duration - clip.BlendOutDuration && clip.BlendOutDuration > 0)
            {
                float normalizedTime = (t - (clip.Duration - clip.BlendOutDuration)) / clip.BlendOutDuration;
                alpha = clip.BlendOutCurve.Evaluate(normalizedTime);
            }
            else
            {
                // Middle section
                if (clip.BlendInDuration > 0)
                    alpha = clip.BlendInCurve.Evaluate(1f);
                else if (clip.BlendOutDuration > 0)
                    alpha = clip.BlendOutCurve.Evaluate(0f);
                else
                    alpha = 0f; // Default hide
            }
            
            if (clip.Inverse)
            {
                alpha = 1f - alpha;
            }
            
            // Clamp to avoid visual artifacts with over-evaluated curves
            return Mathf.Clamp01(alpha);
        }
        */

        public override void OnExit()
        {
            // 退出片段时恢复原本的显示状态
            ApplyVisibility(!clip.Inverse);
        }

        public override void OnDisable()
        {
            // 防止播放打断时没有恢复
            ApplyVisibility(true);
        }
    }
}
