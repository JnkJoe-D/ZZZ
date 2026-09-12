using System.Collections.Generic;
using UnityEngine;
using ATEditor;
using Game.Logic;

namespace Game.Adapters
{
    /// <summary>
    /// ATEditor Buff 时间轴驱动适配器。
    /// 桥接 ATEditor 的 IBuffHandler 接口与具体的 CharacterEntity.StatusModule.Buffs 系统。
    /// 纯粹通过 Luban 配表 ID (buffId) 与动态上下文驱动，彻底解耦 ScriptableObject 资产。
    /// </summary>
    public class ATBuffHandler : IBuffHandler
    {
        private readonly CharacterEntity _entity;
        // 记录由时间轴片段生命周期管理的生效 Buff 实例映射 (clipId -> BuffInstance)
        private readonly Dictionary<string, BuffInstance> _activeClipBuffs = new(4);

        public ATBuffHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void OnBuffEnter(int buffId, string clipId, BuffClipLifetimeMode mode)
        {
            if (_entity?.StatusModule?.Buffs == null) return;
            if (buffId <= 0) return;

            var ctx = new BuffApplyContext
            {
                Instigator = _entity,
                SourceClipId = clipId,
                OverrideDuration = (mode == BuffClipLifetimeMode.ManageByClip) ? -1f : null
            };

            var instance = _entity.StatusModule.Buffs.AddBuff(buffId, ctx);
            if (instance != null && mode == BuffClipLifetimeMode.ManageByClip && !string.IsNullOrEmpty(clipId))
            {
                _activeClipBuffs[clipId] = instance;
            }
        }

        public void OnBuffExit(int buffId, string clipId)
        {
            if (_entity?.StatusModule?.Buffs == null) return;

            if (!string.IsNullOrEmpty(clipId) && _activeClipBuffs.TryGetValue(clipId, out var instance))
            {
                if (instance?.Definition != null)
                {
                    _entity.StatusModule.Buffs.RemoveBuff(instance.Definition.Id);
                }
                _activeClipBuffs.Remove(clipId);
            }
            else if (buffId > 0)
            {
                _entity.StatusModule.Buffs.RemoveBuff(buffId);
            }
        }
    }
}
