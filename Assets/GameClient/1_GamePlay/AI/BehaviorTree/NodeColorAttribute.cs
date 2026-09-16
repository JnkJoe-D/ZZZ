using System;

namespace Game.GamePlay 
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class NodeColorAttribute : Attribute
    {
        public string HexColor { get; }

        public NodeColorAttribute(string hexColor)
        {
            HexColor = hexColor;
        }
    }
}
