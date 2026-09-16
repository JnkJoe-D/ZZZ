using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework
{
    public enum AudioChannel
    {
        /// <summary>
        /// 最高优先级，无视游戏暂停、时停、无距离衰减（通常为2D）
        /// </summary>
        UI = 0,
        
        /// <summary>
        /// 游戏内核心音效（打击、技能），受时停和暂停控制
        /// </summary>
        SFX = 1,
        
        /// <summary>
        /// 背景音乐及长时效环境音
        /// </summary>
        BGM = 2
    }

    public class AudioArgs
    {
        public float volume = 1f;
        public float pitch = 1f;
        public bool loop = false;
        public float spatialBlend = 0f;
        public float startTime = 0f;
        public Vector3 position = Vector3.zero;
        
        // 扩展字段
        public Transform parent = null;
    }

    public class AudioManager : Game.Framework.MonoSingleton<AudioManager>
    {
        private class AudioSourceInfo
        {
            public int id;
            public AudioSource source;
            public AudioChannel channel;
            public bool isBorrowed;
            public bool isPaused;
            public float playStartTime;
            public Transform followTarget;
        }

        [SerializeField] private int _poolSizeUI = 5;
        [SerializeField] private int _poolSizeSFX = 20;

        private Transform _audioRoot;
        private Transform _uiRoot;
        private Transform _sfxRoot;
        private Transform _bgmRoot;

        private ComponentPool<AudioSource> _uiPool;
        private ComponentPool<AudioSource> _sfxPool;
        
        private AudioSource _bgmSource;
        
        private readonly List<AudioSourceInfo> _activeInfos = new List<AudioSourceInfo>();
        private readonly Stack<AudioSourceInfo> _infoPool = new Stack<AudioSourceInfo>(32);
        private int _nextId = 1;
        
        private bool _isSFXPaused = false;

        private AudioSourceInfo AcquireInfo()
        {
            if (_infoPool.Count > 0)
            {
                return _infoPool.Pop();
            }
            return new AudioSourceInfo();
        }

        private void ReleaseInfo(AudioSourceInfo info)
        {
            if (info == null) return;
            info.id = 0;
            info.source = null;
            info.isBorrowed = false;
            info.isPaused = false;
            info.playStartTime = 0f;
            info.followTarget = null;
            _infoPool.Push(info);
        }

        public void Initialize()
        {
            _audioRoot = transform;
            
            _uiRoot = new GameObject("UI").transform;
            _uiRoot.SetParent(_audioRoot);
            
            _sfxRoot = new GameObject("SFX").transform;
            _sfxRoot.SetParent(_audioRoot);
            
            _bgmRoot = new GameObject("BGM").transform;
            _bgmRoot.SetParent(_audioRoot);

            InitPools();
            InitBGM();
        }

        private void InitPools()
        {
            var uiConfig = new ComponentPool<AudioSource>.Config
            {
                initialSize = _poolSizeUI,
                maxSize = _poolSizeUI * 2
            };
            _uiPool = new ComponentPool<AudioSource>(() => CreateAudioSource(_uiRoot), uiConfig);
            _uiPool.OnGet = SetupAudioSource;
            _uiPool.OnReturn = ResetAudioSource;

            var sfxConfig = new ComponentPool<AudioSource>.Config
            {
                initialSize = _poolSizeSFX,
                maxSize = _poolSizeSFX * 2
            };
            _sfxPool = new ComponentPool<AudioSource>(() => CreateAudioSource(_sfxRoot), sfxConfig);
            _sfxPool.OnGet = SetupAudioSource;
            _sfxPool.OnReturn = ResetAudioSource;
        }

        private void InitBGM()
        {
            var go = new GameObject("BGM_Source");
            go.transform.SetParent(_bgmRoot);
            _bgmSource = go.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
            _bgmSource.ignoreListenerPause = true;
        }

        private AudioSource CreateAudioSource(Transform root)
        {
            var go = new GameObject($"AudioSource");
            go.transform.SetParent(root);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            go.SetActive(false);
            return source;
        }

        private void SetupAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
        }

        private void ResetAudioSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
        }

        private ComponentPool<AudioSource> GetPoolByChannel(AudioChannel channel)
        {
            return channel == AudioChannel.UI ? _uiPool : _sfxPool;
        }

        public int PlayAudio(AudioClip clip, AudioChannel channel, AudioArgs args)
        {
            if (clip == null) return -1;

            if (channel == AudioChannel.BGM)
            {
                PlayBGM(clip, args);
                return 0; // BGM doesn't use ID tracking in the same way
            }

            var pool = GetPoolByChannel(channel);
            var source = pool.Get();
            if (source == null) return -1;

            int id = _nextId++;
            var info = AcquireInfo();
            info.id = id;
            info.source = source;
            info.channel = channel;
            info.isBorrowed = true;
            info.isPaused = false;
            info.playStartTime = Time.time;
            info.followTarget = null;
            _activeInfos.Add(info);

            source.clip = clip;
            source.volume = args.volume;
            source.pitch = args.pitch;
            source.loop = args.loop;
            source.spatialBlend = args.spatialBlend;
            source.time = args.startTime;
            
            if (channel == AudioChannel.UI)
            {
                source.ignoreListenerPause = true;
                source.spatialBlend = 0f; // Force 2D for UI
            }
            else
            {
                source.ignoreListenerPause = false;
            }

            if (args.parent != null)
            {
                info.followTarget = args.parent;
                source.transform.position = args.parent.position;
            }
            else if (args.spatialBlend > 0.01f)
            {
                info.followTarget = null;
                source.transform.position = args.position;
            }
            else
            {
                info.followTarget = null;
            }

            source.Play();
            
            // Check if SFX should be paused
            if (channel == AudioChannel.SFX && _isSFXPaused)
            {
                source.Pause();
            }

            return id;
        }

        private void PlayBGM(AudioClip clip, AudioArgs args)
        {
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
            _bgmSource.clip = clip;
            _bgmSource.volume = args.volume;
            _bgmSource.pitch = args.pitch;
            _bgmSource.Play();
        }

        public void StopAudio(int soundId)
        {
            var info = GetInfoById(soundId);
            if (info != null)
            {
                ReturnSource(info);
            }
        }

        public void PauseAudio(int soundId)
        {
            var info = GetInfoById(soundId);
            if (info != null && info.source != null && info.source.isPlaying)
            {
                info.isPaused = true;
                info.source.Pause();
            }
        }

        public void ResumeAudio(int soundId)
        {
            var info = GetInfoById(soundId);
            if (info != null && info.source != null && !info.source.isPlaying)
            {
                info.isPaused = false;
                info.source.UnPause();
            }
        }

        public void UpdateAudio(int soundId, float volume, float pitch, float time)
        {
            var info = GetInfoById(soundId);
            if (info != null && info.source != null)
            {
                info.source.volume = volume;
                info.source.pitch = pitch;
                if (time >= 0f && Mathf.Abs(info.source.time - time) > 0.1f)
                {
                    info.source.time = time;
                }
            }
        }

        public void PauseChannel(AudioChannel channel)
        {
            if (channel == AudioChannel.BGM)
            {
                _bgmSource.Pause();
            }
            else if (channel == AudioChannel.SFX)
            {
                _isSFXPaused = true;
                foreach (var info in _activeInfos)
                {
                    if (info.isBorrowed && info.channel == AudioChannel.SFX && info.source != null)
                    {
                        info.source.Pause();
                    }
                }
            }
        }

        public void ResumeChannel(AudioChannel channel)
        {
            if (channel == AudioChannel.BGM)
            {
                _bgmSource.UnPause();
            }
            else if (channel == AudioChannel.SFX)
            {
                _isSFXPaused = false;
                foreach (var info in _activeInfos)
                {
                    if (info.isBorrowed && info.channel == AudioChannel.SFX && info.source != null)
                    {
                        info.source.UnPause();
                    }
                }
            }
        }

        public void StopAll()
        {
            for (int i = _activeInfos.Count - 1; i >= 0; i--)
            {
                if (_activeInfos[i].isBorrowed)
                {
                    ReturnSource(_activeInfos[i]);
                }
            }
        }

        private AudioSourceInfo GetInfoById(int id)
        {
            foreach (var info in _activeInfos)
            {
                if (info.isBorrowed && info.id == id) return info;
            }
            return null;
        }

        private void ReturnSource(AudioSourceInfo info)
        {
            if (info.source != null)
            {
                var pool = GetPoolByChannel(info.channel);
                pool?.Return(info.source);
            }
            _activeInfos.Remove(info);
            ReleaseInfo(info);
        }

        private void Update()
        {
            // Sync follow positions and auto-return finished sources
            for (int i = _activeInfos.Count - 1; i >= 0; i--)
            {
                var info = _activeInfos[i];
                if (!info.isBorrowed || info.source == null) continue;

                if (info.followTarget != null)
                {
                    info.source.transform.position = info.followTarget.position;
                }

                if (!info.source.isPlaying)
                {
                    // 若单体被主动暂停，不执行回收
                    if (info.isPaused) continue;

                    // Check if it's actually finished or just paused
                    if (info.channel == AudioChannel.SFX && _isSFXPaused) continue;
                    
                    // Note: Unity's `isPlaying` might be false for the very first frame after Play() is called
                    if (Time.time - info.playStartTime < 0.1f) continue;
                    
                    // Not paused, not playing -> likely finished
                    ReturnSource(info);
                }
            }
        }

        public void Shutdown()
        {
            StopAll();
            _bgmSource?.Stop();
            
            _uiPool?.Dispose();
            _sfxPool?.Dispose();
            _infoPool.Clear();
        }

        protected override void OnDestroy()
        {
            Shutdown();
            base.OnDestroy();
        }
    }
}
