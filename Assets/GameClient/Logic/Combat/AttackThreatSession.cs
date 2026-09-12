using System.Collections.Generic;
using ATEditor;

namespace Game.Logic
{
    /// <summary>
    /// 持续性攻击威胁会话。
    /// 仅由带有 AttackWarningClip / ParryWindowClip 的动作按需创建，普通攻击（如玩家反击、小技能）不产生此会话。
    /// 支持多角色独立豁免列表（玩家 A 闪避后标记，切入的玩家 B 不在列表中依然可招架）。
    /// </summary>
    public class AttackThreatSession
    {
        private static readonly Stack<AttackThreatSession> _pool = new(8);

        public static AttackThreatSession Allocate(CharacterEntity attacker, AttackWarningMarker marker)
        {
            var session = _pool.Count > 0 ? _pool.Pop() : new AttackThreatSession();
            session.Attacker = attacker;
            session.WarningMarker = marker;
            session.RemainingDuration = 0f;
            session.IsClosed = false;
            session._excludedVictims.Clear();
            return session;
        }

        public static void Release(AttackThreatSession session)
        {
            if (session == null) return;
            session.Reset();
            if (_pool.Count < 16)
            {
                _pool.Push(session);
            }
        }

        public CharacterEntity Attacker { get; set; }
        public AttackWarningMarker WarningMarker { get; set; }
        public float RemainingDuration { get; set; }
        public bool IsClosed { get; set; }

        private readonly HashSet<CharacterEntity> _excludedVictims = new(4);

        public bool HasResolvedFor(CharacterEntity victim)
        {
            if (victim == null) return false;
            return _excludedVictims.Contains(victim);
        }

        public void MarkResolved(CharacterEntity victim)
        {
            if (victim != null)
            {
                _excludedVictims.Add(victim);
            }
        }

        public void Reset()
        {
            Attacker = null;
            WarningMarker = null;
            RemainingDuration = 0f;
            IsClosed = false;
            _excludedVictims.Clear();
        }

        public void Close()
        {
            IsClosed = true;
            _excludedVictims.Clear();
        }
    }
}
