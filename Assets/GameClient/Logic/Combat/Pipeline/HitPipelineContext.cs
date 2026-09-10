using System;
using UnityEngine;
using ATEditor;
using cfg.ZZZ;

namespace Game.Logic.Combat.Pipeline
{
    /// <summary>
    /// 命中流水线运行时数据上下文（支持对象池重置复用）
    /// </summary>
    public sealed class HitPipelineContext
    {
        // ── 基础输入数据 ──────────────────
        public CharacterEntity Attacker { get; set; }
        public CharacterEntity Victim { get; set; }
        public Collider HitCollider { get; set; }
        public HitData RawHitData { get; set; }
        public HitEffect HitEffectConfig { get; set; }
        public int HitEffectId => RawHitData.hitEffectId;

        // ── 空间几何特征 ──────────────────
        public Vector3 HitPoint { get; set; }
        public Vector3 HitDirection { get; set; }
        public Vector3 ReactionAxis { get; set; }

        // ── 规则仲裁与数值输出 ────────────
        public float FinalDamage { get; set; }
        public float FinalDazeAmount { get; set; }
        public HitReactionType SelectedReactionType { get; set; }
        public int InterruptLevel { get; set; }
        public int TargetResilience { get; set; }

        // ── 视听与打击感参数 ──────────────
        public bool EnableHitStop { get; set; }
        public float HitStopDuration { get; set; }
        public float HitStopScale { get; set; }
        public GameObject HitVFXPrefab { get; set; }
        public Vector3 HitVFXScale { get; set; }
        public float HitVFXHeight { get; set; }
        public bool HitVFXFollowTarget { get; set; }
        public UnityEngine.AudioClip HitAudioClip { get; set; }
        public float HitStunDuration { get; set; }

        // ── 多段打击标记 ──────────────────
        public int CurrentHitIndex { get; set; }
        public int TotalHitCount { get; set; }

        // ── 控制流状态 ────────────────────
        public HitResultFlags ResultFlags { get; set; }
        public bool IsAborted { get; private set; }
        public string AbortReason { get; private set; }

        /// <summary>
        /// 中断当前管道执行（短路后续过滤器）
        /// </summary>
        public void Abort(string reason, HitResultFlags flag = HitResultFlags.None)
        {
            IsAborted = true;
            AbortReason = reason;
            ResultFlags |= flag;
        }

        /// <summary>
        /// 重置所有状态（对象池复用，零 GC）
        /// </summary>
        public void Reset()
        {
            Attacker = null;
            Victim = null;
            HitCollider = null;
            RawHitData = default;
            HitEffectConfig = null;

            HitPoint = Vector3.zero;
            HitDirection = Vector3.forward;
            ReactionAxis = Vector3.back;

            FinalDamage = 0f;
            FinalDazeAmount = 0f;
            SelectedReactionType = HitReactionType.None;
            InterruptLevel = 0;
            TargetResilience = 0;

            EnableHitStop = false;
            HitStopDuration = 0f;
            HitStopScale = 0f;
            HitVFXPrefab = null;
            HitVFXScale = Vector3.one;
            HitVFXHeight = 0f;
            HitVFXFollowTarget = false;
            HitAudioClip = null;
            HitStunDuration = 0f;

            CurrentHitIndex = 0;
            TotalHitCount = 1;

            ResultFlags = HitResultFlags.None;
            IsAborted = false;
            AbortReason = null;
        }
    }
}
