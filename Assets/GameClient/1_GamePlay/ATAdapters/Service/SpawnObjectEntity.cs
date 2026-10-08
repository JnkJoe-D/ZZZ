using System.Collections.Generic;
using ATEditor;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 生成物实体组件基类 (MonoBehaviour 实现类，定义在 GamePlay 业务层)
    /// 承载 ISpawnObject 契约，接管生命周期管理、受控时钟位移推进与自主攻击检测。
    /// </summary>
    public class SpawnObjectEntity : MonoBehaviour, ISpawnObject, IProjectileHandler
    {
        protected ISpawnHandler _handler;
        protected SpawnData _spawnData;
        protected TimeClock _ownerClock;

        // ── 运行时状态 ──
        protected bool _isActive = false;
        protected float _lifeTimer = 0f;
        protected float _traveledDistance = 0f;
        protected Vector3 _velocity;
        protected float _currentSpeed;

        // ── 攻击判定状态 ──
        private static readonly Collider[] _hitBuffer = new Collider[32];
        private readonly HashSet<Collider> _hitRecords = new HashSet<Collider>();
        private readonly List<Collider> _cachedValidHits = new List<Collider>(8);
        private float _lastHitCheckTime = -1f;
        private int _hitTimesChecked = 0;
        private int _currentHitCount = 0;

        public GameObject GameObject => gameObject;
        public bool IsActive => _isActive;

        public virtual void Initialize(SpawnData data, ISpawnHandler handler)
        {
            Initialize(data, handler, null);
        }

        public virtual void Initialize(SpawnData data, ISpawnHandler handler, TimeClock clock)
        {
            _spawnData = data;
            _handler = handler;
            _ownerClock = clock;
            _isActive = true;
            _lifeTimer = 0f;
            _traveledDistance = 0f;
            _lastHitCheckTime = -1f;
            _hitTimesChecked = 0;
            _currentHitCount = 0;
            _hitRecords.Clear();
            _cachedValidHits.Clear();

            // 重置拖尾与粒子状态（防止对象池复用时的旧轨迹/粒子残留）
            var trails = GetComponentsInChildren<TrailRenderer>();
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].Clear();
                trails[i].emitting = true;
            }

            var particles = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear(true);
                particles[i].Play(true);
            }

            // 初始化移动参数
            if (data.movementConfig != null)
            {
                _currentSpeed = data.movementConfig.initialSpeed;
                _velocity = transform.forward * _currentSpeed;
            }
            else
            {
                _currentSpeed = 10f;
                _velocity = transform.forward * _currentSpeed;
            }

            // 如果刚生成且检测频率为 Once，首帧执行一次攻击检测
            if (data.enableAttackDetection && data.attackPolicy != null && data.attackPolicy.detectFrequency == Frequency.Once)
            {
                DoHitCheck();
            }
        }

        protected virtual void Update()
        {
            if (!_isActive) return;

            // 遵循时钟隔离铁律，使用受控实体流速步长
            float dt = _ownerClock != null ? _ownerClock.DeltaTime : Time.deltaTime;
            if (dt <= 0f) return;

            // 1. 推进生命周期计时
            UpdateLifecycle(dt);
            if (!_isActive) return;

            // 2. 推进运动轨迹
            UpdateMovement(dt);
            if (!_isActive) return;

            // 3. 推进攻击检测
            UpdateAttackDetection(dt);
        }

        #region 生命周期管理 (Lifecycle Management)

        protected virtual void UpdateLifecycle(float dt)
        {
            _lifeTimer += dt;

            // 超时回收
            float maxLife = _spawnData.lifecycleConfig != null ? _spawnData.lifecycleConfig.maxLifeTime : 0f;
            if (maxLife > 0f && _lifeTimer >= maxLife)
            {
                Recycle();
                return;
            }

            // 超距回收
            float maxDist = _spawnData.movementConfig != null ? _spawnData.movementConfig.maxDistance : 0f;
            if (maxDist > 0f && _traveledDistance >= maxDist)
            {
                Recycle();
                return;
            }
        }

        public virtual void Terminate()
        {
            _isActive = false;

            // 停止挂载的粒子发射
            var particles = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // 清除拖尾
            var trails = GetComponentsInChildren<TrailRenderer>();
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].emitting = false;
            }
        }

        public virtual void Recycle()
        {
            if (!_isActive && !gameObject.activeSelf) return;

            // 统一委托给全局集中管理器 SpawnObjectManager 归还至框架对象池
            SpawnObjectManager.Instance.Recycle(this);
        }

        #endregion

        #region 移动与轨迹推进 (Controlled Movement)

        protected virtual void UpdateMovement(float dt)
        {
            var moveCfg = _spawnData.movementConfig;
            if (moveCfg == null || moveCfg.moveMode == ProjectileMoveMode.Static)
            {
                return;
            }

            Vector3 stepMove = Vector3.zero;

            switch (moveCfg.moveMode)
            {
                case ProjectileMoveMode.StraightLine:
                    if (moveCfg.acceleration != 0f)
                    {
                        _currentSpeed += moveCfg.acceleration * dt;
                        if (moveCfg.maxSpeed > 0f)
                        {
                            _currentSpeed = Mathf.Min(_currentSpeed, moveCfg.maxSpeed);
                        }
                    }
                    stepMove = transform.forward * (_currentSpeed * dt);
                    break;

                case ProjectileMoveMode.Parabola:
                    _velocity += Physics.gravity * (moveCfg.gravityScale * dt);
                    stepMove = _velocity * dt;
                    if (moveCfg.orientToVelocity && stepMove.sqrMagnitude > 0.0001f)
                    {
                        transform.rotation = Quaternion.LookRotation(stepMove.normalized);
                    }
                    break;

                case ProjectileMoveMode.TargetTracking:
                    // 直线向前推进，预留朝向目标平滑插值
                    stepMove = transform.forward * (_currentSpeed * dt);
                    break;

                default:
                    stepMove = transform.forward * (_currentSpeed * dt);
                    break;
            }

            transform.position += stepMove;
            _traveledDistance += stepMove.magnitude;
        }

        #endregion

        #region 自主攻击检测 (Attack Detection)

        protected virtual void UpdateAttackDetection(float dt)
        {
            if (!_spawnData.enableAttackDetection || _spawnData.attackPolicy == null) return;

            var policy = _spawnData.attackPolicy;
            if (policy.detectFrequency == Frequency.Times)
            {
                if (policy.times <= 0 || _hitTimesChecked >= policy.times) return;

                float maxLife = _spawnData.lifecycleConfig != null && _spawnData.lifecycleConfig.maxLifeTime > 0f
                    ? _spawnData.lifecycleConfig.maxLifeTime
                    : 1.0f;
                float interval = policy.times > 1 ? maxLife / policy.times : maxLife;

                if (_lastHitCheckTime < 0f || _lifeTimer - _lastHitCheckTime >= interval)
                {
                    DoHitCheck();
                    _lastHitCheckTime = _lifeTimer;
                    _hitTimesChecked++;
                }
            }
        }

        protected virtual void DoHitCheck()
        {
            var policy = _spawnData.attackPolicy;
            var scope = _spawnData.hitBoxScope;
            if (policy == null || scope == null || _spawnData.hitHandler == null) return;

            if (policy.maxHitTargets > 0 && _currentHitCount >= policy.maxHitTargets) return;

            Vector3 center = transform.position + transform.rotation * scope.positionOffset;
            Quaternion rotation = transform.rotation * Quaternion.Euler(scope.rotationOffset);
            var shape = scope.shape ?? new HitBoxShape();
            int layerMask = policy.hitLayerMask.value;

            int count = 0;
            switch (shape.shapeType)
            {
                case HitBoxType.Sphere:
                    count = Physics.OverlapSphereNonAlloc(center, shape.radius, _hitBuffer, layerMask);
                    break;
                case HitBoxType.Box:
                    count = Physics.OverlapBoxNonAlloc(center, shape.size / 2f, _hitBuffer, rotation, layerMask);
                    break;
                case HitBoxType.Capsule:
                    Vector3 up = rotation * Vector3.up;
                    float h = Mathf.Max(0, shape.height - shape.radius * 2);
                    Vector3 p1 = center - up * (h / 2f);
                    Vector3 p2 = center + up * (h / 2f);
                    count = Physics.OverlapCapsuleNonAlloc(p1, p2, shape.radius, _hitBuffer, layerMask);
                    break;
                default:
                    count = Physics.OverlapSphereNonAlloc(center, shape.radius, _hitBuffer, layerMask);
                    break;
            }

            if (count == 0) return;

            _cachedValidHits.Clear();
            for (int i = 0; i < count; i++)
            {
                var hit = _hitBuffer[i];
                if (hit == null) continue;

                // 排除施法者自身
                if (!policy.isSelfImpacted && _spawnData.deployer != null)
                {
                    if (hit.gameObject == _spawnData.deployer || hit.transform.root == _spawnData.deployer.transform.root)
                    {
                        continue;
                    }
                }

                // 过滤已命中过的目标
                if (_hitRecords.Contains(hit)) continue;

                _cachedValidHits.Add(hit);
            }

            System.Array.Clear(_hitBuffer, 0, count);

            if (_cachedValidHits.Count == 0) return;

            // 记录命中
            foreach (var h in _cachedValidHits)
            {
                _hitRecords.Add(h);
            }
            _currentHitCount += _cachedValidHits.Count;

            // 取对应的 DetectConfig
            int configIndex = (policy.detects != null && policy.detects.Length > 0)
                ? Mathf.Clamp(_hitTimesChecked, 0, policy.detects.Length - 1)
                : 0;
            var detectConfig = (policy.detects != null && policy.detects.Length > 0)
                ? policy.detects[configIndex]
                : new DetectConfig();

            HitData hitData = new HitData
            {
                deployer = _spawnData.deployer,
                hitBoxCenter = center,
                targetsCollilders = _cachedValidHits.ToArray(),
                hitEffectId = detectConfig.hitEffectId,
                hitDirectionMode = policy.hitDirectionMode,
                customHitDirection = policy.customHitDirection,
                customWorldDirection = transform.forward,
                hitMode = detectConfig.hitMode,
                multiHitCount = detectConfig.multiHitCount,
                multiHitDuration = detectConfig.multiHitDuration,
                enableHitStop = detectConfig.enableHitStop,
                hitStopDuration = detectConfig.hitStopDuration,
                hitStopScale = detectConfig.hitStopScale,
                hitVFXPrefab = detectConfig.hitVFXPrefab,
                hitVFXHeight = detectConfig.hitVFXHeight,
                hitVFXScale = detectConfig.hitVFXScale,
                hitAudioClip = detectConfig.hitAudioClip,
                hitStunDuration = detectConfig.hitStunDuration,
                followTarget = detectConfig.followTarget
            };

            _spawnData.hitHandler.OnHitDetect(hitData);

            // 若达到最大目标数上限，直接触发回收
            if (policy.maxHitTargets > 0 && _currentHitCount >= policy.maxHitTargets)
            {
                Recycle();
            }
        }

        #endregion
    }
}
