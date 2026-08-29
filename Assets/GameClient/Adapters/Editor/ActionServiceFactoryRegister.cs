using UnityEditor;
using ATEditor.Editor;
using UnityEngine;

namespace Game.Adapters.Editor
{
    public static class ActionServiceFactoryRegister
    {
        [InitializeOnLoadMethod]
        private static void RegisterToSkillEditor()
        {
            ATEditorGlobalSettings.DefaultServiceFactoryCreator = owner => ATServiceFactory.ProvideService;
            
            ATEditorGlobalSettings.OnEditorDispose -= ATServiceFactory.ClearAllStaticCaches;
            ATEditorGlobalSettings.OnEditorDispose += ATServiceFactory.ClearAllStaticCaches;
        }
    }
}
