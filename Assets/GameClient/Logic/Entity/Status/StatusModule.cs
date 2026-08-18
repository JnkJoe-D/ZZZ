using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    public class StatusModule
    {
        public AttributeSet Attributes { get; } = new();
        public BuffContainer Buffs { get; } = new();

        private CharacterEntity _owner;
        private readonly HashSet<string> _immuneTags = new();
        private IStatusDataProvider _dataProvider;

        public void Init(CharacterEntity owner, IStatusDataProvider dataProvider, int level = 1)
        {
            _owner = owner;
            Attributes.Init(owner);
            Buffs.Init(owner);
            _immuneTags.Clear();
            _dataProvider = dataProvider;

            if (dataProvider == null) return;

            foreach (var attr in dataProvider.GetInitialAttributes(level))
            {
                Attributes.Register(attr);
            }

            foreach (var tag in dataProvider.GetImmuneTags())
            {
                _immuneTags.Add(tag);
            }
        }

        public void Tick(float deltaTime)
        {
            Attributes.Tick(deltaTime);
            Buffs.Tick(deltaTime);
        }

        public void Clear()
        {
            Buffs.Clear();
            Attributes.Clear();
            _immuneTags.Clear();
        }

        public bool IsTagImmune(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            return _immuneTags.Contains(tag);
        }

        public bool IsBuffImmune(BuffDefAsset buffDef)
        {
            if (buffDef == null || buffDef.Tags == null || buffDef.Tags.Count == 0) return false;

            for (int i = 0; i < buffDef.Tags.Count; i++)
            {
                if (IsTagImmune(buffDef.Tags[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public float GetEXSpecialAttackCost(int skillId)
        {
            return _dataProvider?.GetEXSpecialAttackCost(skillId) ?? 0f;
        }
    }
}
