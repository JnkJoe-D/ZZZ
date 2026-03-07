using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 编辑器预览音频管理器
    /// 用对象池管理 AudioSource 组件，避免频繁创建销毁
    /// </summary>
    public class EditorAudioManager
    {
        private static EditorAudioManager instance;
        public static EditorAudioManager Instance => instance ??= new EditorAudioManager();

        private GameObject audioRoot;
        private Queue<AudioSource> pool = new Queue<AudioSource>();
        private List<AudioSource> active = new List<AudioSource>();

        /// <summary>
        /// 从池中获取 AudioSource（池空则新建）
        /// </summary>
        public AudioSource Get()
        {
            AudioSource src;

            if (pool.Count > 0)
            {
                src = pool.Dequeue();
                if (src != null)
                {
                    src.gameObject.SetActive(true);
                    active.Add(src);
                    ResetSource(src); // 确保状态重置
                    return src;
                }
            }

            // 创建新的
            EnsureRoot();
            var go = new GameObject("AudioSource_Preview");
            go.transform.SetParent(audioRoot.transform);
            go.hideFlags = HideFlags.HideAndDontSave;
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            active.Add(src);
            ResetSource(src);
            return src;
        }

        /// <summary>
        /// 归还 AudioSource 到池
        /// </summary>
        public void Return(AudioSource src)
        {
            if (src == null) return;

            if (src.isPlaying) src.Stop();
            src.clip = null;
            src.gameObject.SetActive(false);
            
            if (active.Contains(src)) active.Remove(src);
            if (!pool.Contains(src)) pool.Enqueue(src);
        }

        private void ResetSource(AudioSource src)
        {
            src.clip = null;
            src.volume = 1f;
            src.pitch = 1f;
            src.loop = false;
            src.spatialBlend = 0f;
            src.time = 0f;
        }

        /// <summary>
        /// 归还所有活跃的 AudioSource
        /// </summary>
        public void ReturnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var src = active[i];
                if (src != null)
                {
                    src.Stop();
                    src.clip = null;
                    src.volume = 1f;
                    src.gameObject.SetActive(false);
                    pool.Enqueue(src);
                }
            }
            active.Clear();
        }

        /// <summary>
        /// 销毁所有实例和池
        /// </summary>
        public void Dispose()
        {
            ReturnAll();
            pool.Clear();
            if (audioRoot != null)
            {
                Object.DestroyImmediate(audioRoot);
                audioRoot = null;
            }
            instance = null;
        }

        private void EnsureRoot()
        {
            if (audioRoot == null)
            {
                audioRoot = new GameObject("[EditorAudioPreview]");
                audioRoot.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}
