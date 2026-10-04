using UnityEngine;
using Game.Framework;
using Game.GamePlay;
using ATEditor;

namespace Game.Presentation.VFX.CrossFlash
{
    /// <summary>
    /// 全屏十字闪光总管理器。
    /// 统一负责全屏十字预警闪光的生命周期、表现层宿主组件的创建与销毁、全局闪光流程控制与调试。
    /// 在 Test_Character / GameRoot 等流程启动时显式调用 Initialize 进行装配。
    /// </summary>
    public class CrossFlashManager : Singleton<CrossFlashManager>
    {
        private GameObject _presenterGameObject;
        private CrossFlashPresenterComponent _presenter;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public CrossFlashController Controller => _presenter != null ? _presenter.Controller : null;

        /// <summary>
        /// 初始化总管理器并装配表现层宿主
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                GLog.Warning(LogTags.Combat, "[CrossFlashManager] Already initialized.");
                return;
            }

            _isInitialized = true;

            var existing = Object.FindObjectOfType<CrossFlashPresenterComponent>();
            if (existing != null)
            {
                _presenter = existing;
                _presenterGameObject = existing.gameObject;
            }
            else
            {
                _presenterGameObject = new GameObject("[CrossFlashPresenter]");
                Object.DontDestroyOnLoad(_presenterGameObject);
                _presenter = _presenterGameObject.AddComponent<CrossFlashPresenterComponent>();
            }

            _presenter.Initialize();
            GLog.Info(LogTags.Combat, "[CrossFlashManager] Initialized successfully.");
        }

        /// <summary>
        /// 关闭总管理器并回收表现层资源
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized) return;
            _isInitialized = false;

            if (_presenter != null)
            {
                _presenter.Shutdown();
            }

            if (_presenterGameObject != null)
            {
                Object.Destroy(_presenterGameObject);
                _presenterGameObject = null;
                _presenter = null;
            }

            GLog.Info(LogTags.Combat, "[CrossFlashManager] Shutdown completed.");
        }

        /// <summary>
        /// 编程式主动触发一次十字闪光（供测试、调试或纯代码逻辑直接调用）
        /// </summary>
        public void Play(Vector3 worldPos, Transform target, Vector3 positionOffset, in CrossFlashParameters parameters, TimeClock clock = null)
        {
            if (!_isInitialized || _presenter == null)
            {
                GLog.Warning(LogTags.Combat, "[CrossFlashManager] Cannot play cross flash: Manager not initialized.");
                return;
            }

            _presenter.Controller.SpawnFlash(worldPos, target, positionOffset, parameters, clock);
        }

        /// <summary>
        /// 清空当前屏幕所有正在播放的十字闪光
        /// </summary>
        public void Clear()
        {
            if (_presenter != null)
            {
                _presenter.Controller.Clear();
            }
        }
    }
}
