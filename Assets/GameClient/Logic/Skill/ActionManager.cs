using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SkillEditor;
using Game.Adapters;
using Game.Logic.Action.Config;
using Game.Logic.Character.Config;

namespace Game.Logic.Action
{
    /// <summary>
    /// 全局静态技能管理器（非Mono），用于复用 Timeline 数据和角色的播放器
    /// </summary>
    public class ActionManager : Game.Framework.Singleton<ActionManager>
    {
        // 缓存解析过的 JSON 数据 -> 成为 Timeline
        private Dictionary<int, SkillTimeline> _timelineCache = new Dictionary<int, SkillTimeline>();
        private readonly Dictionary<int, Task<SkillTimeline>> _timelineLoadTasks = new Dictionary<int, Task<SkillTimeline>>();
        // 缓存各角色的 Context
        private Dictionary<int, ProcessContext> _contextCache = new Dictionary<int, ProcessContext>();
        // 缓存各角色的 Runner
        private Dictionary<int, SkillRunner> _runnerCache = new Dictionary<int, SkillRunner>();

        public void Initialize() { }

        /// <summary>
        /// 一次性预热加载角色挂载的所有动作资源
        /// </summary>
        public void PreloadCharacterActions(CharacterConfigAsset config)
        {
            if (config == null) return;
            foreach (var actionConfig in config.GetAllActionConfigs())
            {
                if (actionConfig != null)
                {
                    if (!_timelineCache.ContainsKey(actionConfig.ID))
                    {
                        Debug.LogWarning($"[ActionManager] Timeline for action '{actionConfig.name}' was requested through synchronous preload. Use PreloadCharacterActionsAsync for runtime initialization.");
                    }
                }
            }
        }

        public async Task PreloadCharacterActionsAsync(CharacterConfigAsset config)
        {
            if (config == null) return;

            var loadTasks = new List<Task>(8);
            foreach (var actionConfig in config.GetAllActionConfigs())
            {
                if (actionConfig != null)
                {
                    loadTasks.Add(GetOrLoadTimelineAsync(actionConfig));
                }
            }

            if (loadTasks.Count > 0)
            {
                await Task.WhenAll(loadTasks);
            }
        }

        public SkillTimeline GetOrLoadTimeline(ActionConfigAsset config)
        {
            if (config == null || config.TimelineAsset == null) return null;
            
            if (_timelineCache.TryGetValue(config.ID, out var timeline))
                return timeline;

            if (_timelineLoadTasks.ContainsKey(config.ID))
            {
                Debug.LogWarning($"[ActionManager] Timeline '{config.name}' is still loading asynchronously. Playback expects it to be preloaded before use.");
            }
            else
            {
                Debug.LogWarning($"[ActionManager] Timeline '{config.name}' is not cached. Call PreloadCharacterActionsAsync/GetOrLoadTimelineAsync before playback.");
            }

            return null;
        }
        
        public async Task<SkillTimeline> GetOrLoadTimelineAsync(ActionConfigAsset config)
        {
            if (config == null || config.TimelineAsset == null) return null;

            if (_timelineCache.TryGetValue(config.ID, out var cachedTimeline))
            {
                return cachedTimeline;
            }

            if (_timelineLoadTasks.TryGetValue(config.ID, out var inFlightTask))
            {
                return await inFlightTask;
            }

            Task<SkillTimeline> loadTask = LoadTimelineInternalAsync(config);
            _timelineLoadTasks[config.ID] = loadTask;

            try
            {
                SkillTimeline timeline = await loadTask;
                if (timeline != null)
                {
                    _timelineCache[config.ID] = timeline;
                }

                return timeline;
            }
            finally
            {
                _timelineLoadTasks.Remove(config.ID);
            }
        }
        
        private static async Task<SkillTimeline> LoadTimelineInternalAsync(ActionConfigAsset config)
        {
            return await SerializationUtility.OpenFromJsonAsync(config.TimelineAsset);
        }
        public ProcessContext GetContext(Character.CharacterEntity entity)
        {
            int id = entity.GetInstanceID();
            if (!_contextCache.TryGetValue(id, out var ctx))
            {
                ctx = new ProcessContext(entity.gameObject, SkillEditor.PlayMode.Runtime, new SkillServiceFactory(entity.gameObject));
                _contextCache[id] = ctx;
            }
            return ctx;
        }

        public SkillRunner GetRunner(Character.CharacterEntity entity)
        {
            int id = entity.GetInstanceID();
            if (!_runnerCache.TryGetValue(id, out var runner))
            {
                runner = new SkillRunner(SkillEditor.PlayMode.Runtime);
                _runnerCache[id] = runner;
            }
            return runner;
        }

        public void RemoveCache(Character.CharacterEntity entity)
        {
            if(entity == null) return;
            int id = entity.GetInstanceID();
            if (_runnerCache.TryGetValue(id, out var runner))
            {
                runner.Stop();
                _runnerCache.Remove(id);
            }
            if (_contextCache.TryGetValue(id, out var ctx))
            {
                ctx.Clear();
                _contextCache.Remove(id);
            }
            SkillServiceFactory.ClearAllStaticCaches(); 
        }

        public void Shutdown()
        {
            foreach (var runner in _runnerCache.Values) runner.Stop();
            foreach (var ctx in _contextCache.Values) ctx.Clear();
            _runnerCache.Clear();
            _contextCache.Clear();
            _timelineCache.Clear();
            _timelineLoadTasks.Clear();
        }
    }
}
