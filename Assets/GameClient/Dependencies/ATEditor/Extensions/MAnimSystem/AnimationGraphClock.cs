using System.Collections.Generic;
using UnityEngine.Playables;

namespace MAnimSystem
{
    /// <summary>保留权威求值后的图时间/权重；显示采样不能推进逻辑混合、清理或状态事件。</summary>
    internal sealed class AnimationGraphClock
    {
        private struct Sample
        {
            public Playable Node;
            public double Time;
            public float[] Weights;
        }
        private readonly List<Sample> samples = new List<Sample>();
        private readonly HashSet<Playable> visited = new HashSet<Playable>();
        private int count;
        private bool sampled;

        public void Capture(PlayableGraph graph)
        {
            count = 0; visited.Clear(); sampled = false;
            if (!graph.IsValid()) return;
            for (int i = 0; i < graph.GetRootPlayableCount(); i++) Capture(graph.GetRootPlayable(i));
        }
        private void Capture(Playable node)
        {
            if (!node.IsValid() || !visited.Add(node)) return;
            int inputs = node.GetInputCount();
            var sample = count < samples.Count ? samples[count] : default;
            sample.Node = node; sample.Time = node.GetTime();
            if (sample.Weights == null || sample.Weights.Length != inputs) sample.Weights = new float[inputs];
            for (int i = 0; i < inputs; i++) sample.Weights[i] = node.GetInputWeight(i);
            if (count < samples.Count) samples[count] = sample; else samples.Add(sample);
            count++;
            for (int i = 0; i < inputs; i++) Capture(node.GetInput(i));
        }
        public void MarkPresentationSample() => sampled = true;
        public bool Restore()
        {
            if (!sampled) return false;
            sampled = false;
            for (int i = 0; i < count; i++)
            {
                var sample = samples[i];
                if (!sample.Node.IsValid()) continue;
                sample.Node.SetTime(sample.Time);
                for (int input = 0; input < sample.Weights.Length; input++) sample.Node.SetInputWeight(input, sample.Weights[input]);
            }
            return true;
        }
    }
}
