using Game.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ATEditor;

namespace Game.GamePlay
{
    /// <summary>
    /// 全局静态技能管理器（非Mono），用于复用 Timeline 数据和角色的播放器
    /// </summary>
    public class ActionManager : Game.Framework.Singleton<ActionManager>
    {
        // 缓存解析过的 JSON 数据 -> 成为 Timeline
        private Dictionary<int, ActionTimeline> _timelineCache = new Dictionary<int, ActionTimeline>();
        private readonly Dictionary<int, Task<ActionTimeline>> _timelineLoadTasks = new Dictionary<int, Task<ActionTimeline>>();

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
                        GLog.Warning(LogTags.Action, $"Timeline for action '{actionConfig.name}' was requested through synchronous preload. Use PreloadCharacterActionsAsync for runtime initialization.");
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

        public ActionTimeline GetOrLoadTimeline(ActionConfigAsset config)
        {
            if (config == null) return null;
            
            // 优先直接使用 SO
            if (config.actionTimelineSO != null)
            {
                _timelineCache[config.ID] = config.actionTimelineSO;
                return config.actionTimelineSO;
            }

            if (config.TimelineAsset == null) return null;
            
            if (_timelineCache.TryGetValue(config.ID, out var timeline))
                return timeline;

            if (_timelineLoadTasks.ContainsKey(config.ID))
            {
                GLog.Warning(LogTags.Action, $"Timeline '{config.name}' is still loading asynchronously. Playback expects it to be preloaded before use.");
            }
            else
            {
                GLog.Warning(LogTags.Action, $"Timeline '{config.name}' is not cached. Call PreloadCharacterActionsAsync/GetOrLoadTimelineAsync before playback.");
            }

            return null;
        }
        
        public async Task<ActionTimeline> GetOrLoadTimelineAsync(ActionConfigAsset config)
        {
            if (config == null) return null;

            // 优先直接使用 SO，免去解析 JSON 的异步任务
            if (config.actionTimelineSO != null)
            {
                _timelineCache[config.ID] = config.actionTimelineSO;
                return config.actionTimelineSO;
            }

            if (config.TimelineAsset == null) return null;

            if (_timelineCache.TryGetValue(config.ID, out var cachedTimeline))
            {
                return cachedTimeline;
            }

            if (_timelineLoadTasks.TryGetValue(config.ID, out var inFlightTask))
            {
                return await inFlightTask;
            }

            Task<ActionTimeline> loadTask = LoadTimelineInternalAsync(config);
            _timelineLoadTasks[config.ID] = loadTask;

            try
            {
                ActionTimeline timeline = await loadTask;
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
        
        private static async Task<ActionTimeline> LoadTimelineInternalAsync(ActionConfigAsset config)
        {
            return await SerializationUtility.OpenFromJsonAsync(config.TimelineAsset);
        }

        /// <summary>
        /// 卸载特定角色的动作 Timeline 缓存，支持按需内存释放
        /// </summary>
        public void UnloadCharacterActions(CharacterConfigAsset config)
        {
            if (config == null) return;
            foreach (var actionConfig in config.GetAllActionConfigs())
            {
                if (actionConfig != null)
                {
                    _timelineCache.Remove(actionConfig.ID);
                    _timelineLoadTasks.Remove(actionConfig.ID);
                }
            }
        }

        /// <summary>
        /// 实体清理时的安全钩子，清理静态静态服务缓存
        /// </summary>
        public void RemoveCache(CharacterEntity entity)
        {
            if (entity != null && entity.gameObject != null)
            {
                ATServiceFactory.RemoveStaticCaches(entity.gameObject);
            }
        }

        public void Shutdown()
        {
            _timelineCache.Clear();
            _timelineLoadTasks.Clear();
        }
    }
}
