using System.Collections.Generic;
using Game.Framework;
using ATEditor;

namespace Game.Logic.Character
{
    public struct CharacterTimelineEvent : IGameEvent
    {
        public RoleEntity SourceEntity;
        public string EventName;
        public List<SkillEventParam> Parameters;
    }
}
