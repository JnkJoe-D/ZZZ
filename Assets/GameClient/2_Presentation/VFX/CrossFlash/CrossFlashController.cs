using System.Collections.Generic;
using UnityEngine;
using ATEditor;
using Game.Framework;
using Game.GamePlay;

namespace Game.Presentation.VFX.CrossFlash
{
    /// <summary>
    /// 全屏十字闪光多实例纯 C# 控制器。
    /// 负责闪光实例池管理、时钟/流速推进、世界坐标视口投影、相机背面剔除及 GPU 常量数组打包。
    /// 直接与实体的私有时钟 (CharacterEntity.Clock) 联动，杜绝因动作打断导致顿帧恢复丢失的问题。
    /// </summary>
    public class CrossFlashController
    {
        public const int MaxFlashes = 8;

        public static bool HasActiveFlashes { get; private set; }
        public static Material ActiveMaterial { get; set; }

        public class FlashInstance
        {
            public bool IsActive;
            public Vector3 WorldPosition;
            public Transform TargetTransform;
            public Vector3 PositionOffset;
            public CrossFlashParameters Params;
            public TimeClock Clock;
            public float ElapsedTime;

            public float Progress => Mathf.Clamp01(ElapsedTime / Mathf.Max(0.001f, Params.Duration));
            public bool IsFinished => ElapsedTime >= Params.Duration;

            public void Reset()
            {
                IsActive = false;
                TargetTransform = null;
                PositionOffset = Vector3.zero;
                Clock = null;
                ElapsedTime = 0f;
            }
        }

        private readonly FlashInstance[] _instances = new FlashInstance[MaxFlashes];

        // Shader 属性 ID 缓存
        private static readonly int ActiveFlashCountID = Shader.PropertyToID("_ActiveFlashCount");
        private static readonly int FlashParams0ID     = Shader.PropertyToID("_FlashParams0");
        private static readonly int FlashParams1ID     = Shader.PropertyToID("_FlashParams1");
        private static readonly int FlashParams2ID     = Shader.PropertyToID("_FlashParams2");
        private static readonly int FlashColorsID      = Shader.PropertyToID("_FlashColors");

        // GPU 封包数据缓冲 (避免每帧 GC Alloc)
        private readonly Vector4[] _params0 = new Vector4[MaxFlashes];
        private readonly Vector4[] _params1 = new Vector4[MaxFlashes];
        private readonly Vector4[] _params2 = new Vector4[MaxFlashes];
        private readonly Vector4[] _colors  = new Vector4[MaxFlashes];

        public CrossFlashController()
        {
            for (int i = 0; i < MaxFlashes; i++)
            {
                _instances[i] = new FlashInstance();
            }
        }

        /// <summary>
        /// 生成或更新一个新的闪光实例
        /// </summary>
        public void SpawnFlash(Vector3 worldPos, Transform target, Vector3 positionOffset, in CrossFlashParameters parameters, TimeClock clock = null)
        {
            FlashInstance targetSlot = null;

            // 1. 寻找未激活槽位
            for (int i = 0; i < MaxFlashes; i++)
            {
                if (!_instances[i].IsActive)
                {
                    targetSlot = _instances[i];
                    break;
                }
            }

            // 2. 若槽位已满，采用优先级淘汰策略（替换播放进度最接近结束的实例）
            if (targetSlot == null)
            {
                float maxProgress = -1f;
                int replaceIndex = 0;
                for (int i = 0; i < MaxFlashes; i++)
                {
                    if (_instances[i].Progress > maxProgress)
                    {
                        maxProgress = _instances[i].Progress;
                        replaceIndex = i;
                    }
                }
                targetSlot = _instances[replaceIndex];
                targetSlot.Reset();
            }

            // 3. 初始化实例数据
            targetSlot.IsActive = true;
            targetSlot.WorldPosition = worldPos;
            targetSlot.TargetTransform = target;
            targetSlot.PositionOffset = positionOffset;
            targetSlot.Params = parameters;
            targetSlot.Clock = clock;
            targetSlot.ElapsedTime = 0f;

            HasActiveFlashes = true;
        }

        /// <summary>
        /// 每帧更新实例并封包至材质
        /// </summary>
        public void Update(float unscaledDeltaTime, Camera mainCamera, Material material)
        {
            if (mainCamera == null || material == null) return;

            int activeCount = 0;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);

            for (int i = 0; i < MaxFlashes; i++)
            {
                var instance = _instances[i];
                if (!instance.IsActive) continue;

                // 推进时间：直接读取实体私有时钟的有效流速（顿帧时为 0，顿帧结束后自动恢复，不受动作打断影响）
                float effectiveSpeed = (instance.Params.InheritEntityTimeScale && instance.Clock != null)
                    ? instance.Clock.EffectiveScale
                    : 1.0f;
                instance.ElapsedTime += unscaledDeltaTime * Mathf.Max(0f, effectiveSpeed);

                if (instance.IsFinished)
                {
                    instance.Reset();
                    continue;
                }

                // 实时跟踪骨骼位移（核心约束：仅同步绑定点的世界坐标，不同步旋转）
                Vector3 currentWorldPos = (instance.TargetTransform != null && instance.Params.FollowTarget)
                    ? instance.TargetTransform.position + instance.PositionOffset
                    : instance.WorldPosition;

                // 视口坐标投影
                Vector3 viewportPos = mainCamera.WorldToViewportPoint(currentWorldPos);

                // 剔除相机后方的点 (z <= 0)
                float intensity = viewportPos.z > 0.05f ? instance.Params.Intensity : 0f;

                // 数据封包
                _params0[activeCount] = new Vector4(viewportPos.x, viewportPos.y, instance.Progress, intensity);
                _params1[activeCount] = new Vector4(
                    instance.Params.BaseLineWidth,
                    instance.Params.MinLineWidth,
                    instance.Params.MaxLineWidth,
                    instance.Params.WidthScaleRate
                );
                _params2[activeCount] = new Vector4(
                    instance.Params.LineSoftness,
                    instance.Params.NearFadeDistance,
                    aspect,
                    0f
                );
                _colors[activeCount] = instance.Params.FlashColor;

                activeCount++;
            }

            HasActiveFlashes = activeCount > 0;

            // 传递参数到材质 (ShaderLab 中为 Float 类型，使用 SetFloat 杜绝类型不一致报错)
            material.SetFloat(ActiveFlashCountID, (float)activeCount);
            if (activeCount > 0)
            {
                material.SetVectorArray(FlashParams0ID, _params0);
                material.SetVectorArray(FlashParams1ID, _params1);
                material.SetVectorArray(FlashParams2ID, _params2);
                material.SetVectorArray(FlashColorsID, _colors);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < MaxFlashes; i++)
            {
                _instances[i].Reset();
            }
            HasActiveFlashes = false;
        }
    }
}
