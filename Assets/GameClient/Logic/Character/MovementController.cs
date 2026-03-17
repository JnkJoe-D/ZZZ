using Game.AI;
using Game.Logic.Action.Config;
using UnityEngine;

namespace Game.Logic.Character
{
    [RequireComponent(typeof(CharacterEntity))]
    public class MovementController : MonoBehaviour, IMovementController
    {
        [Header("Block Detection")]
        [SerializeField] private float _blockCheckRadius = 0.4f;
        [SerializeField] private float _blockCheckHeight = 1.8f;
        [SerializeField] private LayerMask _blockCheckLayers;
        [SerializeField] private Color _gizmoColor = new Color(1f, 0f, 0f, 0.35f);

        [Header("Debug")]
        [SerializeField] private bool _debugAttackLock;
        [SerializeField] private float _debugLateralThreshold = 0.02f;
        [SerializeField] private bool _debugAttackLockVerbose = true;
        [SerializeField] private string _debugActionNameContains = "Rush";

        private CharacterController _cc;
        private CharacterEntity _entity;
        private Animator _animator;
        private ActionConfigAsset _rootMotionAction;
        private Vector3 _lockedRootMotionForward = Vector3.forward;
        private Vector3 _attackTrackOrigin;
        private Vector3 _attackTrackDirection = Vector3.forward;
        private float _attackTrackDistance;
        private bool _hasAttackTrack;
        private float _verticalVelocity;

        public float TurnSpeed => 15f;
        public float Gravity => -9.81f;

        void Awake()
        {
            _cc = gameObject.GetComponent<CharacterController>();
            if (_cc == null)
            {
                _cc = gameObject.AddComponent<CharacterController>();
                _cc.height = 1.6f;
                _cc.radius = 0.3f;
                _cc.center = new Vector3(0f, 0.8f, 0f);
                _cc.excludeLayers = LayerMask.GetMask("Player");
            }

            _entity = GetComponent<CharacterEntity>();
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator != null)
            {
                _animator.applyRootMotion = true;
            }

            if (_blockCheckLayers.value == 0)
            {
                _blockCheckLayers = LayerMask.GetMask("Default", "CharHit", "Character"); // Remove "Ground" and fix typo
            }
        }

        public void Init(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void Move(Vector3 moveDelta)
        {
            if (_cc != null && _cc.enabled)
            {
                _cc.Move(moveDelta);
                return;
            }

            transform.position += moveDelta;
        }

        void OnAnimatorMove()
        {
            if (_animator == null)
            {
                return;
            }

            Vector3 deltaPosition = Vector3.zero;
            Quaternion deltaRotation = Quaternion.identity;

            if (_animator.applyRootMotion)
            {
                deltaPosition = _animator.deltaPosition;
                deltaRotation = _animator.deltaRotation;
            }

            if (_cc != null && _cc.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            Vector3 gravityDelta = Vector3.up * _verticalVelocity * Time.deltaTime;
            bool hasAnimatorMotion = deltaPosition.sqrMagnitude > 0.000001f || deltaRotation != Quaternion.identity;
            if (hasAnimatorMotion || gravityDelta.sqrMagnitude > 0f)
            {
                SyncRootMotionAction();
                ApplyRootMotion(deltaPosition + gravityDelta);
                if (_animator.applyRootMotion)
                {
                    ApplyRootRotation(deltaRotation);
                }
            }
        }

        private void SyncRootMotionAction()
        {
            ActionConfigAsset currentAction = _entity?.ActionPlayer?.CurrentAction;
            if (currentAction == _rootMotionAction)
            {
                return;
            }

            _rootMotionAction = currentAction;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                _lockedRootMotionForward = forward.normalized;
            }

            BeginAttackTrack(currentAction);
        }

        private void ApplyRootMotion(Vector3 deltaPosition)
        {
            Vector3 horizontalDelta = Vector3.ProjectOnPlane(deltaPosition, Vector3.up);
            Vector3 verticalDelta = Vector3.up * deltaPosition.y;

            if (ShouldLockAttackRootMotion())
            {
                ApplyLockedAttackMotion(horizontalDelta, verticalDelta);
                return;
            }

            if (_cc != null && _cc.enabled)
            {
                _cc.Move(horizontalDelta + verticalDelta);
            }
        }

        private void ApplyRootRotation(Quaternion deltaRotation)
        {
            if (deltaRotation == Quaternion.identity)
            {
                return;
            }

            transform.rotation = transform.rotation * deltaRotation;
        }

        private bool ShouldLockAttackRootMotion()
        {
            return ShouldLockAttackRootMotion(_rootMotionAction);
        }

        private bool ShouldLockAttackRootMotion(ActionConfigAsset action)
        {
            if (!(action is SkillConfigAsset skillConfig))
            {
                return false;
            }

            return skillConfig.Category == SkillCategory.LightAttack
                || skillConfig.Category == SkillCategory.HeavyAttack
                || skillConfig.Category == SkillCategory.DashAttack
                || skillConfig.Category == SkillCategory.SpecialSkill;
        }

        private void BeginAttackTrack(ActionConfigAsset action)
        {
            _attackTrackOrigin = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            _attackTrackDistance = 0f;
            _attackTrackDirection = _lockedRootMotionForward;
            _hasAttackTrack = false;

            if (!ShouldLockAttackRootMotion(action))
            {
                if (ShouldEmitAttackLockDebug(action) && _debugAttackLockVerbose)
                {
                    Debug.LogWarning($"[AttackLock] skip action={action?.name ?? "null"} because it is not a locked attack.");
                }
                return;
            }

            if (TryGetAttackTrackDirection(out Vector3 attackDirection))
            {
                _attackTrackDirection = attackDirection;
            }

            if (_attackTrackDirection.sqrMagnitude <= 0.0001f)
            {
                if (ShouldEmitAttackLockDebug(action))
                {
                    Debug.LogWarning($"[AttackLock] invalid track direction for action={action?.name ?? "null"}.");
                }
                return;
            }

            _attackTrackDirection.Normalize();
            _hasAttackTrack = true;

            if (ShouldEmitAttackLockDebug(action))
            {
                Debug.LogWarning(
                    $"[AttackLock] begin action={action?.name ?? "null"}, pos={transform.position}, " +
                    $"origin={_attackTrackOrigin}, dir={_attackTrackDirection}, lockedForward={_lockedRootMotionForward}, " +
                    $"target={(_entity?.TargetFinder?.GetEnemy() != null ? _entity.TargetFinder.GetEnemy().name : "none")}");
            }
        }

        private bool TryGetAttackTrackDirection(out Vector3 direction)
        {
            direction = _lockedRootMotionForward;

            if (_entity?.TargetFinder == null)
            {
                return direction.sqrMagnitude > 0.0001f;
            }

            Transform enemy = _entity.TargetFinder.GetEnemy();
            if (enemy == null)
            {
                return direction.sqrMagnitude > 0.0001f;
            }

            Vector3 toEnemy = enemy.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude <= 0.0001f)
            {
                return direction.sqrMagnitude > 0.0001f;
            }

            direction = toEnemy.normalized;
            return true;
        }

        private Vector3 ApplyAttackTrackMotion(Vector3 horizontalDelta)
        {
            if (!_hasAttackTrack || _attackTrackDirection.sqrMagnitude <= 0.0001f)
            {
                if (ShouldEmitAttackLockDebug() && _debugAttackLockVerbose)
                {
                    Debug.LogWarning(
                        $"[AttackLock] no track available. action={_rootMotionAction?.name ?? "null"}, " +
                        $"hasTrack={_hasAttackTrack}, dir={_attackTrackDirection}, animDelta={horizontalDelta}");
                }
                // Fallback: 如果没有有效的攻击轨道，退回到原始动画位移向量，不要原地打转
                return horizontalDelta; 
            }

            float desiredDistance = _attackTrackDistance + Vector3.Dot(horizontalDelta, _attackTrackDirection);
            _attackTrackDistance = ClampAttackTrackDistance(desiredDistance);

            Vector3 desiredHorizontalPosition = _attackTrackOrigin + _attackTrackDirection * _attackTrackDistance;
            Vector3 currentHorizontalPosition = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            if (ShouldEmitAttackLockDebug() && _debugAttackLockVerbose)
            {
                Debug.LogWarning(
                    $"[AttackLock] track update action={_rootMotionAction?.name ?? "null"}, animDelta={horizontalDelta}, " +
                    $"desiredDistance={desiredDistance:F4}, clampedDistance={_attackTrackDistance:F4}, " +
                    $"currentXZ={currentHorizontalPosition}, desiredXZ={desiredHorizontalPosition}");
            }
            return desiredHorizontalPosition - currentHorizontalPosition;
        }

        private void ApplyLockedAttackMotion(Vector3 horizontalDelta, Vector3 verticalDelta)
        {
            Vector3 lockedHorizontalDelta = ApplyAttackTrackMotion(horizontalDelta);
            if (_cc != null && _cc.enabled)
            {
                Vector3 beforeMove = transform.position;
                _cc.Move(lockedHorizontalDelta + verticalDelta);
                SnapCurrentPositionToAttackTrack();

                if (ShouldEmitAttackLockDebug())
                {
                    Vector3 actualHorizontal = Vector3.ProjectOnPlane(transform.position - beforeMove, Vector3.up);
                    float lateralAfterMove = GetCurrentAttackLateralOffset();
                    if (ShouldEmitAttackLockDebug() && _debugAttackLockVerbose)
                    {
                        Debug.LogWarning(
                            $"[AttackLock] apply locked move action={_rootMotionAction?.name ?? "null"}, " +
                            $"rawHorizontal={horizontalDelta}, lockedHorizontal={lockedHorizontalDelta}, vertical={verticalDelta}, " +
                            $"actualHorizontal={actualHorizontal}, newPos={transform.position}");
                    }

                    if (lateralAfterMove > _debugLateralThreshold)
                    {
                        Debug.LogWarning(
                            $"[AttackLock] lateral offset after CharacterController move is {lateralAfterMove:F4}. " +
                            $"action={_rootMotionAction?.name}, pos={transform.position}, trackOrigin={_attackTrackOrigin}, " +
                            $"trackDir={_attackTrackDirection}, trackDistance={_attackTrackDistance:F4}, animDelta={horizontalDelta}");
                    }
                }
            }
            else
            {
                transform.position += lockedHorizontalDelta + verticalDelta;
                SnapCurrentPositionToAttackTrack();
            }
        }

        private void SnapCurrentPositionToAttackTrack()
        {
            if (!_hasAttackTrack || _attackTrackDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 snappedHorizontal = _attackTrackOrigin + _attackTrackDirection * _attackTrackDistance;
            Vector3 currentPosition = transform.position;
            transform.position = new Vector3(snappedHorizontal.x, currentPosition.y, snappedHorizontal.z);
        }

        private float ClampAttackTrackDistance(float desiredDistance)
        {
            float deltaDistance = desiredDistance - _attackTrackDistance;
            if (_cc == null || !_cc.enabled || Mathf.Abs(deltaDistance) <= 0.0001f)
            {
                return desiredDistance;
            }

            Vector3 moveDirection = deltaDistance >= 0f ? _attackTrackDirection : -_attackTrackDirection;
            float moveDistance = Mathf.Abs(deltaDistance);
            if (!TryFindBlockingHit(moveDirection, moveDistance, out RaycastHit hit))
            {
                return desiredDistance;
            }

            float allowedDistance = Mathf.Max(0f, hit.distance - Mathf.Max(_cc.skinWidth, 0.01f));
            return _attackTrackDistance + Mathf.Sign(deltaDistance) * allowedDistance;
        }

        private bool TryFindBlockingHit(Vector3 moveDirection, float moveDistance, out RaycastHit bestHit)
        {
            bestHit = default;
            if (_cc == null || moveDistance <= 0f)
            {
                if (ShouldEmitAttackLockDebug() && _debugAttackLockVerbose)
                {
                    Debug.LogWarning(
                        $"[AttackLock] skip block cast action={_rootMotionAction?.name ?? "null"}, " +
                        $"hasCC={_cc != null}, moveDistance={moveDistance:F4}");
                }
                return false;
            }

            GetCapsuleWorldPoints(out Vector3 top, out Vector3 bottom);
            float castDistance = moveDistance + Mathf.Max(_cc.skinWidth, 0.01f);
            RaycastHit[] hits = Physics.CapsuleCastAll(
                top,
                bottom,
                Mathf.Max(_blockCheckRadius, 0.01f),
                moveDirection,
                castDistance,
                _blockCheckLayers,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.transform.root == transform.root)
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                }
            }

            if (ShouldEmitAttackLockDebug())
            {
                string hitName = bestDistance < float.MaxValue && bestHit.collider != null ? bestHit.collider.name : "none";
                int hitLayer = bestDistance < float.MaxValue && bestHit.collider != null ? bestHit.collider.gameObject.layer : -1;
                Debug.LogWarning(
                    $"[AttackLock] block cast result: hit={bestDistance < float.MaxValue}, " +
                    $"collider={hitName}, layer={hitLayer}, distance={(bestDistance < float.MaxValue ? bestDistance.ToString("F4") : "n/a")}, " +
                    $"moveDir={moveDirection}, moveDistance={moveDistance:F4}, radius={Mathf.Max(_blockCheckRadius, 0.01f):F3}");
            }

            return bestDistance < float.MaxValue;
        }

        private bool ShouldEmitAttackLockDebug()
        {
            return ShouldEmitAttackLockDebug(_rootMotionAction);
        }

        private bool ShouldEmitAttackLockDebug(ActionConfigAsset action)
        {
            if (!_debugAttackLock)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_debugActionNameContains))
            {
                return true;
            }

            string actionName = action?.name;
            return !string.IsNullOrEmpty(actionName)
                && actionName.IndexOf(_debugActionNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private float GetCurrentAttackLateralOffset()
        {
            if (!_hasAttackTrack || _attackTrackDirection.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            Vector3 currentHorizontal = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 fromOrigin = currentHorizontal - _attackTrackOrigin;
            float forwardDistance = Vector3.Dot(fromOrigin, _attackTrackDirection);
            Vector3 projectedPoint = _attackTrackOrigin + _attackTrackDirection * forwardDistance;
            Vector3 lateral = currentHorizontal - projectedPoint;
            return lateral.magnitude;
        }

        private void GetCapsuleWorldPoints(out Vector3 top, out Vector3 bottom)
        {
            // 稍稍抬高探测胶囊体，避免直接从脚底发射导致撞击地面层
            const float liftOffset = 0.1f; 
            Vector3 center = transform.TransformPoint(_cc != null ? _cc.center : new Vector3(0f, _blockCheckHeight * 0.5f, 0f));
            float halfHeight = Mathf.Max(_blockCheckHeight * 0.5f - _blockCheckRadius, 0f);
            top = center + Vector3.up * halfHeight;
            bottom = center - Vector3.up * (halfHeight - liftOffset); 
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || _cc == null)
            {
                return;
            }

            Gizmos.color = _gizmoColor;
            GetCapsuleWorldPoints(out Vector3 top, out Vector3 bottom);
            DrawCapsule(top, bottom, _blockCheckRadius);
        }

        private void DrawCapsule(Vector3 top, Vector3 bottom, float radius)
        {
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawLine(top + Vector3.left * radius, bottom + Vector3.left * radius);
            Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
            Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
            Gizmos.DrawLine(top + Vector3.back * radius, bottom + Vector3.back * radius);
        }

        public void FaceTo(Vector3 inputDir, float speed = -1f)
        {
            var lookDirection = CalculateWorldDirection(inputDir);
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                speed = speed == -1f ? TurnSpeed : speed > 0 ? speed : TurnSpeed;
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            }
        }

        public void FaceToImmediately(Vector3 inputDir)
        {
            var lookDirection = CalculateWorldDirection(inputDir);
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                _entity.transform.forward = lookDirection.normalized;
            }
        }

        public void FaceToTarget(Transform target, float speed = -1f)
        {
            if (target == null) return;
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                speed = speed == -1f ? TurnSpeed : speed > 0 ? speed : TurnSpeed;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            }
        }

        public void FaceToTargetImmediately(Transform target)
        {
            if (target == null) return;
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                _entity.transform.forward = direction.normalized;
            }
        }

        public Vector3 CalculateWorldDirection(Vector2 inputDir)
        {
            if (_entity.InputProvider is AIInputProvider aiInputProvider &&
                aiInputProvider.TryGetWorldMovementDirection(out Vector3 aiWorldDirection))
            {
                return aiWorldDirection.normalized;
            }

            if (_entity.CameraController != null)
            {
                Vector3 camForward = _entity.CameraController.GetForward();
                Vector3 camRight = _entity.CameraController.GetRight();
                return (camForward * inputDir.y + camRight * inputDir.x).normalized;
            }

            return new Vector3(inputDir.x, 0f, inputDir.y).normalized;
        }

        public bool IsGrounded
        {
            get
            {
                if (_cc != null) return _cc.isGrounded;
                return true;
            }
        }
    }
}
