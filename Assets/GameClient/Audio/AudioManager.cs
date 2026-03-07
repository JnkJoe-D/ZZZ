using System.Collections.Generic;
using UnityEngine;
using Game.Pool;

namespace Game.Audio
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
            public float playStartTime;
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
        private int _nextId = 1;
        
        private bool _isSFXPaused = false;

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
            source.transform.SetParent(_audioRoot);
        }

        private ComponentPool<AudioSource> GetPoolByChannel(AudioChannel channel)
        {
            return channel == AudioChannel.UI ? _uiPool : _sfxPool;
        }

        public int PlayAudio(AudioClip clip, AudioChannel channel, Game.Audio.AudioArgs args)
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
            var info = new AudioSourceInfo { id = id, source = source, channel = channel, isBorrowed = true, playStartTime = Time.time };
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

            if (args.spatialBlend > 0.01f && args.parent == null)
            {
                source.transform.position = args.position;
            }
            if (args.parent != null)
            {
                source.transform.SetParent(args.parent);
                source.transform.localPosition = Vector3.zero;
            }

            source.Play();
            
            // Check if SFX should be paused
            if (channel == AudioChannel.SFX && _isSFXPaused)
            {
                source.Pause();
            }

            return id;
        }

        private void PlayBGM(AudioClip clip, Game.Audio.AudioArgs args)
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
                info.source.Pause();
            }
        }

        public void ResumeAudio(int soundId)
        {
            var info = GetInfoById(soundId);
            if (info != null && info.source != null && !info.source.isPlaying)
            {
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
            info.isBorrowed = false;
            info.id = 0;
            _activeInfos.Remove(info);
        }

        private void Update()
        {
            // Auto-return finished sources
            for (int i = _activeInfos.Count - 1; i >= 0; i--)
            {
                var info = _activeInfos[i];
                if (info.isBorrowed && info.source != null && !info.source.isPlaying)
                {
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
        }
    }
}
