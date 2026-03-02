using UnityEngine;

namespace Game.MAnimSystem
{
    /// <summary>
    /// Example usage of AnimComponent with single state, 1D blend tree, and 2D blend tree.
    ///
    /// Controls:
    /// - Alpha1: Play single clip A
    /// - Alpha2: Play single clip B
    /// - Alpha3: Play 1D blend state
    /// - Alpha4: Play 2D blend state
    /// - Q / E: Decrease / increase 1D parameter
    /// - W / A / S / D: Move 2D parameter
    /// - Space: Reset blend parameters
    /// </summary>
    public class Sample_AnimComponentUsage : MonoBehaviour
    {
        #region References
        public AnimComponent animComponent;
        #endregion

        #region Single State Clips
        public AnimationClip singleClipA;
        public AnimationClip singleClipB;
        public float fadeDuration = 0.15f;
        #endregion

        #region 1D Blend Inputs
        public AnimationClip clipIdle;
        public AnimationClip clipWalk;
        public AnimationClip clipRun;
        public float parameter1D = 0f;
        public float parameter1DStep = 1.2f;
        #endregion

        #region 2D Blend Inputs
        public AnimationClip clipLeft;
        public AnimationClip clipRight;
        public AnimationClip clipForward;
        public AnimationClip clipBack;
        public Vector2 parameter2D = Vector2.zero;
        public float parameter2DSpeed = 2f;
        #endregion

        #region Runtime States
        private BlendTree1DState _blend1D;
        private BlendTree2DState _blend2D;

        private enum ControlMode
        {
            None,
            Blend1D,
            Blend2D
        }

        private ControlMode _mode;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            animComponent ??= GetComponent<AnimComponent>();
            if (animComponent == null)
            {
                animComponent = gameObject.AddComponent<AnimComponent>();
            }

            animComponent.Initialize();
            animComponent.InitializeGraph();
            BuildBlendStates();
        }

        private void Update()
        {
            HandlePlayHotkeys();
            UpdateBlendParameters();
        }
        #endregion

        #region Setup
        private void BuildBlendStates()
        {
            if (clipIdle != null && clipWalk != null && clipRun != null)
            {
                _blend1D = animComponent.CreateBlendTree1DState(new[]
                {
                    new BlendTree1DChild(clipIdle, 0f),
                    new BlendTree1DChild(clipWalk, 0.5f),
                    new BlendTree1DChild(clipRun, 1f)
                }, parameter1D);
            }

            if (clipLeft != null && clipRight != null && clipForward != null && clipBack != null)
            {
                _blend2D = animComponent.CreateBlendTree2DState(new[]
                {
                    new BlendTree2DChild(clipLeft, new Vector2(-1f, 0f)),
                    new BlendTree2DChild(clipRight, new Vector2(1f, 0f)),
                    new BlendTree2DChild(clipForward, new Vector2(0f, 1f)),
                    new BlendTree2DChild(clipBack, new Vector2(0f, -1f))
                }, parameter2D);
            }
        }
        #endregion

        #region Input and Playback
        private void HandlePlayHotkeys()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) && singleClipA != null)
            {
                animComponent.Play(singleClipA, fadeDuration, true);
                _mode = ControlMode.None;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) && singleClipB != null)
            {
                animComponent.Play(singleClipB, fadeDuration, true);
                _mode = ControlMode.None;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3) && _blend1D != null)
            {
                animComponent.Play(_blend1D, fadeDuration, true);
                _mode = ControlMode.Blend1D;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4) && _blend2D != null)
            {
                animComponent.Play(_blend2D, fadeDuration, true);
                _mode = ControlMode.Blend2D;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                parameter1D = 0f;
                parameter2D = Vector2.zero;
                if (_blend1D != null)
                {
                    _blend1D.Parameter = parameter1D;
                }
                if (_blend2D != null)
                {
                    _blend2D.Parameter = parameter2D;
                }
            }
        }

        private void UpdateBlendParameters()
        {
            if (_mode == ControlMode.Blend1D && _blend1D != null)
            {
                float delta = 0f;
                if (UnityEngine.Input.GetKey(KeyCode.Q))
                {
                    delta -= parameter1DStep * Time.deltaTime;
                }
                if (UnityEngine.Input.GetKey(KeyCode.E))
                {
                    delta += parameter1DStep * Time.deltaTime;
                }

                if (!Mathf.Approximately(delta, 0f))
                {
                    parameter1D = Mathf.Clamp01(parameter1D + delta);
                    _blend1D.Parameter = parameter1D;
                }
            }

            if (_mode == ControlMode.Blend2D && _blend2D != null)
            {
                Vector2 input = Vector2.zero;
                if (UnityEngine.Input.GetKey(KeyCode.A)) input.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D)) input.x += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S)) input.y -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.W)) input.y += 1f;

                if (input.sqrMagnitude > 1f)
                {
                    input.Normalize();
                }

                if (input.sqrMagnitude > 0f)
                {
                    parameter2D += input * (parameter2DSpeed * Time.deltaTime);
                    parameter2D = Vector2.ClampMagnitude(parameter2D, 1f);
                    _blend2D.Parameter = parameter2D;
                }
            }
        }
        #endregion
    }
}

