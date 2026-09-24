using System.Collections.Generic;
using UnityEngine;

namespace ATEditor.Test
{
    /// <summary>
    /// 测试专用动画服务模拟桩，用于验证 ProcessContext 的 LayerMask 托管栈推退逻辑
    /// </summary>
    public class MockAnimationHandler : IAnimationHandler
    {
        private readonly Dictionary<int, AvatarMask> _masks = new Dictionary<int, AvatarMask>();
        public readonly List<string> CallLogs = new List<string>();

        public void SetLayerMask(int layerIndex, AvatarMask mask)
        {
            _masks[layerIndex] = mask;
            CallLogs.Add($"SetLayerMask: Layer={layerIndex}, Mask={(mask != null ? mask.name : "null")}");
        }

        public AvatarMask GetLayerMask(int layerIndex)
        {
            _masks.TryGetValue(layerIndex, out var mask);
            CallLogs.Add($"GetLayerMask: Layer={layerIndex}, Mask={(mask != null ? mask.name : "null")}");
            return mask;
        }

        public void PlayAnimation(UnityEngine.AnimationClip clip, int layerIndex, float fadeDuration, float speed, float startTime = 0f)
        {
            CallLogs.Add($"PlayAnimation: Clip={(clip != null ? clip.name : "null")}, Layer={layerIndex}");
        }

        public void SetLayerSpeed(int layerIndex, float speed)
        {
            CallLogs.Add($"SetLayerSpeed: Layer={layerIndex}, Speed={speed}");
        }

        public void SetTime(int layerIndex, float time)
        {
            CallLogs.Add($"SetTime: Layer={layerIndex}, Time={time}");
        }

        public void Initialize()
        {
            CallLogs.Add("Initialize");
        }
    }
}
