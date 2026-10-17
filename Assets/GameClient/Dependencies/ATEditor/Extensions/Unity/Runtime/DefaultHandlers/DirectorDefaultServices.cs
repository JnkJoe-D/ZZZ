using System;
using MAnimSystem;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 默认服务组合根。按需懒加载基础默认服务，由宿主集中管理生命周期与释放。
    /// 遵循 SRP 原则，纯粹作为服务工厂与容器，不承担帧驱动（Tick）或根运动劫持责任。
    /// </summary>
    internal sealed class DirectorDefaultServices : IDisposable
    {
        private readonly GameObject _owner;
        private readonly Func<ActionObjectPool> _getPool;
        private readonly ActionDirector _director;

        private DefaultAnimHandler _animation;
        private IBoneGetter _bones;
        private DefaultVFXHandler _vfx;
        private DefaultAudioHandler _audio;
        private DefaultTransformHandler _transform;
        private DefaultRootMotionHandler _rootMotion;
        private DefaultAttackDetectionHandler _attackDetection;
        private DefaultAttackTargetResolver _attackTargets;
        private DefaultTargetFinder _targetFinder;

        public ProcessContext Context { get; set; }

        public DirectorDefaultServices(ActionDirector director, Func<ActionObjectPool> getPool)
        {
            _director = director;
            _owner = director.gameObject;
            _getPool = getPool;
        }

        public object GetService(Type type)
        {
            if (type == typeof(IAttackDetectionHandler)) return _attackDetection ??= new DefaultAttackDetectionHandler();
            if (type == typeof(IAttackTargetResolver)) return _attackTargets ??= new DefaultAttackTargetResolver();
            if (type == typeof(IAttackObservationHandler)) return _director;
            if (type == typeof(IATTargetFinder)) return _targetFinder ??= new DefaultTargetFinder();
            if (type == typeof(IAnimationHandler))
            {
                if (_animation == null)
                {
                    var component = _owner.GetComponent<AnimComponent>();
                    if (component == null) component = _owner.AddComponent<AnimComponent>();
                    component.Initialize();
                    // 默认自驱动，不接管 MAnim 的原生 Update
                    _animation = new DefaultAnimHandler(component, manuallyDriven: false, ownsGraph: false);
                }
                return _animation;
            }
            if (type == typeof(IBoneGetter)) return _bones ??= new ATBoneGetter(_owner);
            if (type == typeof(ITransformHandler))
                return _transform ??= new DefaultTransformHandler(_owner, () => Context?.GetService<IATTargetFinder>());
            if (type == typeof(IMotionWindowHandler))
                return _rootMotion ??= new DefaultRootMotionHandler(_owner.transform, Context?.GetService<ITransformHandler>());
            if (type == typeof(IVFXHandler)) return _vfx ??= new DefaultVFXHandler(_getPool());
            if (type == typeof(IAudioHandler)) return _audio ??= new DefaultAudioHandler(_getPool());

            return null;
        }

        public void SetPlayback(float speed, bool paused)
        {
            Context?.SetPresentationPlayback(speed, paused);
            _animation?.SetPlaybackClock(speed, paused);
            _vfx?.SetPlaybackClock(speed, paused);
            _audio?.SetPlayback(speed, paused);
        }

        public void Dispose()
        {
            _animation?.Dispose();
            _vfx?.Dispose();
            _audio?.Dispose();
            _rootMotion?.Dispose();

            _animation = null;
            _bones = null;
            _vfx = null;
            _audio = null;
            _transform = null;
            _rootMotion = null;
            _attackDetection = null;
            _attackTargets = null;
            _targetFinder = null;
        }
    }
}
