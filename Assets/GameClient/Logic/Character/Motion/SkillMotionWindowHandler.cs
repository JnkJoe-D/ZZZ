using System.Collections.Generic;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character.Motion
{
    public sealed class SkillMotionWindowHandler : ISkillMotionWindowHandler
    {
        private sealed class ActiveWindow
        {
            /* 同一时刻可能存在多个窗口，最终以后进入的窗口为准。 */ public MotionWindowClip Clip;
            public MotionWindowRuntimeData RuntimeData;
        }

        private readonly CharacterEntity _entity;
        private readonly List<ActiveWindow> _activeWindows = new List<ActiveWindow>();
        private int _enterSequence;

        public SkillMotionWindowHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void OnMotionWindowEnter(MotionWindowClip clip, ProcessContext context, float enterTime)
        {
            if (clip == null)
            {
                return;
            }

            Transform target = ResolvePrimaryTarget(clip);
            if (clip.targetMode == MotionTargetMode.RequireTarget && target == null)
            {
                return;
            }

            MotionWindowRuntimeData runtimeData = new MotionWindowRuntimeData
            {
                Clip = clip,
                PrimaryTarget = target,
                PrimaryTargetCollider = ResolvePrimaryTargetCollider(target),
                /* 记录进窗瞬间的位置，后续做结束落点修正时会用到。 */ StartPosition = _entity != null ? _entity.transform.position : Vector3.zero,
                StartRotation = _entity != null ? _entity.transform.rotation : Quaternion.identity,
                EnterCapsuleCenter = MotionConstraintBoxUtility.ResolveCapsuleCenter(_entity != null ? _entity.gameObject : null, _entity != null ? _entity.transform.position : Vector3.zero),
                EnterTime = enterTime,
                EnterOrder = ++_enterSequence
            };

            /* 进入窗口时先锁定一次参考方向，持续跟踪模式会在 Update 里刷新。 */ runtimeData.ReferenceForward = ResolveReferenceForward(clip, target);

            if (clip.enableConstraintBox &&
                MotionConstraintBoxUtility.TryBuildRuntimeConstraintBox(runtimeData, _entity != null ? _entity.transform : null, out MotionConstraintBoxData boxData))
            {
                runtimeData.HasFrozenConstraintBox = true;
                runtimeData.FrozenConstraintBox = boxData;
            }

            _activeWindows.RemoveAll(x => x.Clip == clip);
            _activeWindows.Add(new ActiveWindow
            {
                Clip = clip,
                RuntimeData = runtimeData
            });
        }

        public void OnMotionWindowUpdate(MotionWindowClip clip, float currentTime, float deltaTime)
        {
            ActiveWindow activeWindow = FindActiveWindow(clip);
            if (activeWindow == null)
            {
                return;
            }

            MotionWindowRuntimeData runtimeData = activeWindow.RuntimeData;
            if (runtimeData == null)
            {
                return;
            }

            if (clip.targetMode == MotionTargetMode.ContinuousTrack || clip.referenceMode == MotionReferenceMode.TargetLineContinuous)
            {
                /* 持续跟踪模式允许在窗口期间重新抓取目标。 */ runtimeData.PrimaryTarget = ResolvePrimaryTarget(clip);
                runtimeData.PrimaryTargetCollider = ResolvePrimaryTargetCollider(runtimeData.PrimaryTarget);
            }

            if (clip.referenceMode == MotionReferenceMode.TargetLineContinuous)
            {
                Vector3 desiredForward = ResolveReferenceForward(clip, runtimeData.PrimaryTarget);
                if (desiredForward.sqrMagnitude > 0.0001f)
                {
                    float turnRate = Mathf.Max(clip.continuousTurnRate, 0f);
                    if (turnRate > 0f)
                    {
                        /* 连续跟踪时不要瞬间跳转，而是按配置速率平滑贴近目标方向。 */ runtimeData.ReferenceForward = Vector3.RotateTowards(
                            runtimeData.ReferenceForward,
                            desiredForward,
                            Mathf.Deg2Rad * turnRate * deltaTime,
                            100f);
                    }
                    else
                    {
                        runtimeData.ReferenceForward = desiredForward;
                    }
                }
            }
        }

        public void OnMotionWindowExit(MotionWindowClip clip)
        {
            RemoveWindow(clip, true);
        }

        public void OnMotionWindowCancel(MotionWindowClip clip)
        {
            RemoveWindow(clip, false);
        }

        public bool TryGetActiveWindow(out MotionWindowRuntimeData runtimeData)
        {
            runtimeData = null;
            if (_activeWindows.Count == 0)
            {
                return false;
            }

            ActiveWindow bestWindow = null;
            for (int i = 0; i < _activeWindows.Count; i++)
            {
                ActiveWindow activeWindow = _activeWindows[i];
                if (activeWindow?.RuntimeData == null)
                {
                    continue;
                }

                if (bestWindow == null || activeWindow.RuntimeData.EnterOrder >= bestWindow.RuntimeData.EnterOrder)
                {
                    /* 多窗口重叠时，始终以后进入的窗口覆盖旧窗口。 */ bestWindow = activeWindow;
                }
            }

            if (bestWindow == null)
            {
                return false;
            }

            runtimeData = bestWindow.RuntimeData;
            return true;
        }

        private ActiveWindow FindActiveWindow(MotionWindowClip clip)
        {
            for (int i = _activeWindows.Count - 1; i >= 0; i--)
            {
                ActiveWindow activeWindow = _activeWindows[i];
                if (activeWindow != null && activeWindow.Clip == clip)
                {
                    return activeWindow;
                }
            }

            return null;
        }

        private void RemoveWindow(MotionWindowClip clip, bool applyEndPlacement)
        {
            ActiveWindow activeWindow = FindActiveWindow(clip);
            if (activeWindow == null)
            {
                return;
            }

            if (applyEndPlacement && _entity?.MovementController is MovementController movementController)
            {
                /* 正常离窗时才做收尾落点修正；被打断时只清状态，不强制改落点。 */ movementController.ApplyMotionWindowEndPlacement(activeWindow.RuntimeData);
            }

            _activeWindows.Remove(activeWindow);
        }

        private Transform ResolvePrimaryTarget(MotionWindowClip clip)
        {
            if (_entity?.TargetFinder == null)
            {
                return null;
            }

            if (clip.targetMode == MotionTargetMode.None)
            {
                return null;
            }

            return _entity.TargetFinder.GetEnemy();
        }

        private Collider ResolvePrimaryTargetCollider(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            Transform root = target.root != null ? target.root : target;
            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            if (colliders == null || colliders.Length == 0)
            {
                return null;
            }

            Vector3 referencePoint = MotionConstraintBoxUtility.ResolveCapsuleCenter(_entity != null ? _entity.gameObject : null, _entity != null ? _entity.transform.position : root.position);
            Collider bestCollider = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                Vector3 closestPoint = collider.ClosestPoint(referencePoint);
                float sqrDistance = (closestPoint - referencePoint).sqrMagnitude;
                if (sqrDistance < bestDistance)
                {
                    bestDistance = sqrDistance;
                    bestCollider = collider;
                }
            }

            return bestCollider;
        }

        private Vector3 ResolveReferenceForward(MotionWindowClip clip, Transform target)
        {
            switch (clip.referenceMode)
            {
                case MotionReferenceMode.InputDirectionAtEnter:
                    if (_entity?.InputProvider != null && _entity.MovementController is MovementController movementController)
                    {
                        Vector2 inputDirection = _entity.InputProvider.GetMovementDirection();
                        Vector3 worldDirection = movementController.CalculateWorldDirection(inputDirection);
                        if (worldDirection.sqrMagnitude > 0.0001f)
                        {
                            worldDirection.y = 0f;
                            return worldDirection.normalized;
                        }
                    }
                    break;

                case MotionReferenceMode.TargetLineAtEnter:
                case MotionReferenceMode.TargetLineContinuous:
                    if (target != null && _entity != null)
                    {
                        Vector3 toTarget = target.position - _entity.transform.position;
                        toTarget.y = 0f;
                        if (toTarget.sqrMagnitude > 0.0001f)
                        {
                            return toTarget.normalized;
                        }
                    }
                    break;
            }

            if (_entity != null)
            {
                Vector3 forward = _entity.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    return forward.normalized;
                }
            }

            return Vector3.forward;
        }
    }
}
