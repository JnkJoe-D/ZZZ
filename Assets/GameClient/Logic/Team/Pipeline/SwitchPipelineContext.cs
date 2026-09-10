using UnityEngine;

namespace Game.Logic.Team.Pipeline
{
    /// <summary>
    /// 切人管线运行时数据上下文总线（支持对象池重置复用，零 GC）
    /// </summary>
    public sealed class SwitchPipelineContext
    {
        // ── 核心环境引用 ──────────────────────────
        public TeamManager Manager { get; set; }

        // ── 切人类型与成员 ────────────────────────
        public SwitchType Type { get; set; }
        public PartyMember OutgoingMember { get; set; }
        public PartyMember IncomingMember { get; set; }
        public RoleEntity OutgoingEntity => OutgoingMember?.Entity;
        public RoleEntity IncomingEntity => IncomingMember?.Entity;
        public int TargetSlotHint { get; set; } = -1;

        // ── 空间落点与朝向 ────────────────────────
        public Vector3 SpawnPosition { get; set; }
        public Quaternion SpawnRotation { get; set; }

        // ── 表现与策略控制 ────────────────────────
        public OutgoingExitPolicy ExitPolicy { get; set; }
        public CameraSwitchMode CamMode { get; set; }
        public float TimeScale { get; set; } = 1.0f;
        public float TimeScaleDuration { get; set; } = 0f;
        public float InvincibleDuration { get; set; } = 0.5f;

        // ── 动作重载 ──────────────────────────────
        public ActionConfigAsset CustomIncomingAction { get; set; }

        // ── 控制流状态 ────────────────────────────
        public bool IsAborted { get; private set; }
        public string AbortReason { get; private set; }

        /// <summary>
        /// 中断当前切人管线执行（短路后续过滤器）
        /// </summary>
        public void Abort(string reason)
        {
            IsAborted = true;
            AbortReason = reason;
        }

        /// <summary>
        /// 重置所有字段（用于对象池复用）
        /// </summary>
        public void Reset()
        {
            Manager = null;
            Type = SwitchType.NormalSwitch;
            OutgoingMember = null;
            IncomingMember = null;
            TargetSlotHint = -1;

            SpawnPosition = Vector3.zero;
            SpawnRotation = Quaternion.identity;

            ExitPolicy = OutgoingExitPolicy.AutoFollowThrough;
            CamMode = CameraSwitchMode.SmoothFollow;
            TimeScale = 1.0f;
            TimeScaleDuration = 0f;
            InvincibleDuration = 0.5f;

            CustomIncomingAction = null;

            IsAborted = false;
            AbortReason = null;
        }
    }
}
