using System.Collections.Generic;
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
        // 缓存各角色的 Context
        private Dictionary<int, ProcessContext> _contextCache = new Dictionary<int, ProcessContext>();
        // 缓存各角色的 Runner
        private Dictionary<int, SkillRunner> _runnerCache = new Dictionary<int, SkillRunner>();

        public void Initialize() { }

        /// <summary>
        /// 一次性预热加载角色挂载的所有动作资源
        /// </summary>
        public void PreloadCharacterActions(CharacterConfigSO config)
        {
            if (config == null) return;
            foreach (var actionConfig in config.GetAllActionConfigs())
            {
                if (actionConfig != null)
                {
                    GetOrLoadTimeline(actionConfig);
                }
            }
        }

        public SkillTimeline GetOrLoadTimeline(ActionConfigSO config)
        {
            if (config == null || config.TimelineAsset == null) return null;
            
            if (_timelineCache.TryGetValue(config.ID, out var timeline))
                return timeline;

            timeline = SerializationUtility.OpenFromJson(config.TimelineAsset);
            if (timeline != null) _timelineCache[config.ID] = timeline;
            
            return timeline;
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
        }
    }
}
