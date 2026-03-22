namespace Game.Logic.Character
{
    public enum CommandContextType
    {
        None = 0,
        GroundIdle = 10,
        GroundJog = 20,
        GroundDash = 30,
        GroundStop = 40,
        Skill = 50,
        Evade = 60,
        Backswing = 70,
        HitStun = 80,
    }
}
