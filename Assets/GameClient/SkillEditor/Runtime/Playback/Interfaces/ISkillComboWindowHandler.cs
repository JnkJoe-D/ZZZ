namespace SkillEditor
{
    public interface ISkillComboWindowHandler
    {
        void OnComboWindowEnter(string comboTag, ComboWindowType windowType);
        void OnComboWindowExit(string comboTag, ComboWindowType windowType);
    }
}
