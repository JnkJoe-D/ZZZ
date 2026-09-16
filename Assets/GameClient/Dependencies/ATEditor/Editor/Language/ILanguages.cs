using System;
namespace ATEditor.Editor
{
    public interface ILanguages { }

    [AttributeUsage(AttributeTargets.Class)]
    public class NameAttribute : Attribute
    {
        public string Name;
        public NameAttribute(string name) { Name = name; }
    }
}
