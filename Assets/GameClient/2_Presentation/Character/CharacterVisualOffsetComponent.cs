using ATEditor;
using Game.Framework;
using Game.GamePlay;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 角色视觉模型偏移表现组件。
    /// 负责纯表现层的骨骼/网格受阻挤压位移与回正计算，解耦动力学与物理移动。
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterVisualOffsetComponent : MonoBehaviour, IVisualOffsetPresenter
    {
        private Transform _visualRoot;
        private MotionWindowVisualOffsetMode _visualOffsetMode = MotionWindowVisualOffsetMode.None;
        private float _visualRecoverSpeed;
        private bool _isVisualRecoverActive;
        private CharacterEntity _owner;

        private void Awake()
        {
            _owner = GetComponent<CharacterEntity>() ?? GetComponentInParent<CharacterEntity>();

            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.CompareTag("CharacterVisual"))
                {
                    _visualRoot = child;
                    break;
                }
            }

            // 如果没找到对应的 Tag，保留按名字查找作为备选兜底

            if (_visualRoot == null)
            {
                _visualRoot = transform.Find("Visual");
            }
        }

        private void OnDisable()
        {
            ResetVisualOffset();
        }

        public void ResetVisualOffset()
        {
            if (_visualRoot != null)
            {
                _visualRoot.localPosition = Vector3.zero;
            }
        }

        public void SetVisualRecover(bool active, float speed = 0f)
        {
            _isVisualRecoverActive = active;
            _visualRecoverSpeed = speed;
        }

        public void SetVisualOffsetMode(MotionWindowVisualOffsetMode visualOffsetMode)
        {
            _visualOffsetMode = visualOffsetMode;
        }

        public void ApplyVisualOffset(Vector3 rawLocalDelta)
        {
            if (_visualRoot == null)
            {
                return;
            }

            Vector3 currentVisualPos = _visualRoot.localPosition;

            // 1. 矫正模式 (Recover Mode)
            if (_isVisualRecoverActive && currentVisualPos.sqrMagnitude > 0.000001f)
            {
                // 计算回正方向向量
                Vector3 dirToOrigin = -currentVisualPos.normalized;

                // 计算原始位移在回正方向上的投影
                float p = Vector3.Dot(rawLocalDelta, dirToOrigin);

                // 如果 p > 0 (朝向原点)，保留该分量；如果 p <= 0 (背离原点)，设为 0（拒绝远离）。
                Vector3 filteredDelta = Mathf.Max(0, p) * dirToOrigin;

                // 附加基础回收速度，使用宿主实体私有时钟步长，彻底摆脱全局时间单例
                float dt = _owner != null && _owner.Clock != null ? _owner.Clock.DeltaTime : Time.deltaTime;
                filteredDelta += dirToOrigin * (_visualRecoverSpeed * dt);

                // 应用最终位移，并防止超调（Overshoot）
                Vector3 nextPos = currentVisualPos + filteredDelta;
                if (Vector3.Dot(-nextPos, dirToOrigin) < 0)
                {
                    nextPos = Vector3.zero;
                }


                _visualRoot.localPosition = nextPos;
                return;
            }

            // 2. 标准模式 (Standard Offset Mode)
            if (_visualOffsetMode == MotionWindowVisualOffsetMode.None)
            {
                return;
            }

            Vector3 visualHorizontalOffsetDelta = Vector3.zero;
            switch (_visualOffsetMode)
            {
                case MotionWindowVisualOffsetMode.X:
                    visualHorizontalOffsetDelta.x = rawLocalDelta.x;
                    break;
                case MotionWindowVisualOffsetMode.Z:
                    visualHorizontalOffsetDelta.z = rawLocalDelta.z;
                    break;
                case MotionWindowVisualOffsetMode.XZ:
                    visualHorizontalOffsetDelta.x = rawLocalDelta.x;
                    visualHorizontalOffsetDelta.z = rawLocalDelta.z;
                    break;
            }

            _visualRoot.localPosition += visualHorizontalOffsetDelta;
        }
    }
}
