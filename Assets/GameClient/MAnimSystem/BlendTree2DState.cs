using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.MAnimSystem
{
    public struct BlendTree2DChild
    {
        public AnimationClip Clip;
        public Vector2 Position;
        public float Speed;

        public BlendTree2DChild(AnimationClip clip, Vector2 position, float speed = 1f)
        {
            Clip = clip;
            Position = position;
            Speed = speed;
        }
    }

    /// <summary>
    /// 2D blend tree state driven by IDW interpolation.
    /// </summary>
    public sealed class BlendTree2DState : StateBase
    {
        #region Types
        private sealed class ChildRuntime
        {
            public AnimationClip Clip;
            public Vector2 Position;
            public float Speed;
            public AnimationClipPlayable Playable;
        }
        #endregion

        #region Fields
        private readonly List<ChildRuntime> _children = new List<ChildRuntime>();
        private AnimationMixerPlayable _mixer;
        private Vector2 _parameter;
        private bool _weightsDirty = true;
        #endregion

        #region Config
        public float IdwPower { get; set; } = 2f;

        public Vector2 Parameter
        {
            get => _parameter;
            set
            {
                _parameter = value;
                _weightsDirty = true;
            }
        }

        public int ChildCount => _children.Count;

        public void SetChildren(IReadOnlyList<BlendTree2DChild> children)
        {
            _children.Clear();

            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    BlendTree2DChild c = children[i];
                    if (c.Clip == null)
                    {
                        continue;
                    }

                    _children.Add(new ChildRuntime
                    {
                        Clip = c.Clip,
                        Position = c.Position,
                        Speed = Mathf.Approximately(c.Speed, 0f) ? 1f : c.Speed,
                        Playable = default
                    });
                }
            }

            _weightsDirty = true;

            if (ParentLayer != null)
            {
                RecreatePlayableInGraph(ParentLayer.Graph);
            }
        }
        #endregion

        #region StateBase
        protected override Playable CreatePlayable(PlayableGraph graph)
        {
            _mixer = AnimationMixerPlayable.Create(graph, _children.Count);

            for (int i = 0; i < _children.Count; i++)
            {
                ChildRuntime child = _children[i];
                child.Playable = AnimationClipPlayable.Create(graph, child.Clip);
                child.Playable.SetApplyFootIK(false);
                child.Playable.SetApplyPlayableIK(false);
                child.Playable.SetSpeed(child.Speed);
                child.Playable.SetDone(false);
                child.Playable.SetTime(0d);

                graph.Connect(child.Playable, 0, _mixer, i);
                _mixer.SetInputWeight(i, 0f);
                _children[i] = child;
            }

            _weightsDirty = true;
            ApplyWeights();
            return _mixer;
        }

        public override void RebuildPlayable()
        {
            base.RebuildPlayable();

            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i].Playable.IsValid())
                {
                    _children[i].Playable.SetDone(false);
                    _children[i].Playable.SetTime(0d);
                    _children[i].Playable.SetSpeed(_children[i].Speed);
                }
            }

            _weightsDirty = true;
            ApplyWeights();
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            if (_weightsDirty)
            {
                ApplyWeights();
            }
        }

        public override void Destroy()
        {
            DestroyInternalPlayable();
            base.Destroy();
        }
        #endregion

        #region Internal
        private void RecreatePlayableInGraph(PlayableGraph graph)
        {
            if (!graph.IsValid())
            {
                return;
            }

            bool wasConnected = ParentLayer != null && PortIndex >= 0;
            float prevWeight = wasConnected ? ParentLayer.GetInputWeight(PortIndex) : 0f;

            if (wasConnected)
            {
                graph.Disconnect(ParentLayer.Mixer, PortIndex);
            }

            DestroyInternalPlayable();
            _playableCache = CreatePlayable(graph);

            if (wasConnected && _playableCache.IsValid())
            {
                graph.Connect(_playableCache, 0, ParentLayer.Mixer, PortIndex);
                ParentLayer.SetInputWeight(PortIndex, prevWeight);
            }
        }

        private void DestroyInternalPlayable()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i].Playable.IsValid())
                {
                    _children[i].Playable.Destroy();
                    _children[i].Playable = default;
                }
            }

            if (_mixer.IsValid())
            {
                _mixer.Destroy();
                _mixer = default;
            }

            _playableCache = Playable.Null;
        }

        private void ApplyWeights()
        {
            _weightsDirty = false;

            if (!_mixer.IsValid())
            {
                return;
            }

            int count = _children.Count;
            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                _mixer.SetInputWeight(i, 0f);
            }

            if (count == 1)
            {
                _mixer.SetInputWeight(0, 1f);
                return;
            }

            const float epsilon = 0.0001f;
            int exactIndex = -1;
            float sum = 0f;
            float[] weights = new float[count];

            float power = Mathf.Max(0.0001f, IdwPower);
            for (int i = 0; i < count; i++)
            {
                float distance = Vector2.Distance(_parameter, _children[i].Position);
                if (distance <= epsilon)
                {
                    exactIndex = i;
                    break;
                }

                float w = 1f / Mathf.Pow(Mathf.Max(distance, epsilon), power);
                weights[i] = w;
                sum += w;
            }

            if (exactIndex >= 0)
            {
                _mixer.SetInputWeight(exactIndex, 1f);
                return;
            }

            if (sum <= epsilon)
            {
                float even = 1f / count;
                for (int i = 0; i < count; i++)
                {
                    _mixer.SetInputWeight(i, even);
                }
                return;
            }

            float inv = 1f / sum;
            for (int i = 0; i < count; i++)
            {
                _mixer.SetInputWeight(i, weights[i] * inv);
            }
        }
        #endregion
    }
}
