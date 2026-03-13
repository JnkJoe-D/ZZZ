namespace Game.Logic.Character
{
    /// <summary>
    /// 玩家操作指令缓存类型
    /// </summary>
    public enum BufferedInputType
    {
        None,
        Evade=10,
        BasicAttack=20,
        BasicAttackCancel = 21,
        BasicAttackHold =30,
        BasicAttackHoldCancel = 31,
        SpecialAttack =40,
        SpecialAttackHold = 45,
        SpecialAttackHoldCancel = 46,
        Ultimate =50
    }
}
