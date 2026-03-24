using System.Collections.Generic;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character.Motion
{
    public sealed class SkillMotionWindowHandler : ISkillMotionWindowHandler
    {
        private sealed class ActiveWindow
        {
            public MotionWindowClip Clip;
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

            Transform target = clip.UsesMotionReference()
                ? ResolvePrimaryTarget(clip)
                : null;
            if (clip.UsesConstraintBox() &&
                clip.targetMode == MotionTargetMode.RequireTarget &&
                target == null)
            {
                return;
            }

            MotionWindowRuntimeData runtimeData = new MotionWindowRuntimeData
            {
                Clip = clip,
                PrimaryTarget = target,
                PrimaryTargetCollider = ResolvePrimaryTargetCollider(target),
                StartPosition = _entity != null ? _entity.transform.position : Vector3.zero,
                StartRotation = _entity != null ? _entity.transform.rotation : Quaternion.identity,
                EnterCapsuleCenter = MotionConstraintBoxUtility.ResolveCapsuleCenter(
                    _entity != null ? _entity.gameObject : null,
                    _entity != null ? _entity.transform.position : Vector3.zero),
                EnterTime = enterTime,
                EnterOrder = ++_enterSequence
            };

            runtimeData.ReferenceForward = clip.UsesMotionReference()
                ? ResolveReferenceForward(clip, target)
                : (_entity != null ? _entity.transform.forward : Vector3.forward);
            if (clip.UsesConstraintBox() &&
                clip.constraintBoxUpdateMode == MotionConstraintBoxUpdateMode.FreezeOnEnter &&
                MotionConstraintBoxUtility.TryBuildRuntimeConstraintBox(
                    runtimeData,
                    _entity != null ? _entity.transform : null,
                    out MotionConstraintBoxData boxData))
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

        public void OnMotionWindowCancel(MotionWindowClip clip)
        {
            RemoveWindow(clip);
        }

        public void OnMotionWindowUpdate(MotionWindowClip clip, float currentTime, float deltaTime)
        {
        }

        public void OnMotionWindowExit(MotionWindowClip clip)
        {
            RemoveWindow(clip);
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
                    bestWindow = activeWindow;
                }
            }

            if (bestWindow == null)
            {
                return false;
            }

            runtimeData = bestWindow.RuntimeData;
            return true;
        }

        private void RemoveWindow(MotionWindowClip clip)
        {
            for (int i = _activeWindows.Count - 1; i >= 0; i--)
            {
                ActiveWindow activeWindow = _activeWindows[i];
                if (activeWindow != null && activeWindow.Clip == clip)
                {
                    _activeWindows.RemoveAt(i);
                    return;
                }
            }
        }

        private Transform ResolvePrimaryTarget(MotionWindowClip clip)
        {
            if (_entity?.TargetFinder == null || clip.targetMode == MotionTargetMode.None)
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
            CharacterController characterController = root.GetComponent<CharacterController>();
            if (characterController != null && characterController.enabled)
            {
                return characterController;
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            if (colliders == null || colliders.Length == 0)
            {
                return null;
            }

            Vector3 referencePoint = MotionConstraintBoxUtility.ResolveCapsuleCenter(
                _entity != null ? _entity.gameObject : null,
                _entity != null ? _entity.transform.position : root.position);
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
