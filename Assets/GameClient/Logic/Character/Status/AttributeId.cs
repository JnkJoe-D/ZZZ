namespace Game.Logic
{
    /// <summary>
    /// 属性类型枚举。纯枚举管理，代码层快速索引。
    /// 预留间隔便于未来在同一类别中插入新属性。
    /// </summary>
    public enum AttributeId
    {
        None = 0,

        // ── 生命 ──
        HP      = 100,
        MaxHP   = 101,

        // ── 能量 ──
        Energy    = 200,
        MaxEnergy = 201,

        // ── 喧响值 (受击方累积) ──
        Daze    = 300,
        MaxDaze = 301,

        // ── 攻防 (占位) ──
        ATK = 400,
        DEF = 500,

        // ── 角色独有仪表 (按角色分段预留) ──
        // 1000~1099 : 角色 A 专用
        // 1100~1199 : 角色 B 专用
        // ...
        CustomGauge01 = 1000,
        CustomGauge02 = 1100,
        CustomGauge03 = 1200,
    }
}
