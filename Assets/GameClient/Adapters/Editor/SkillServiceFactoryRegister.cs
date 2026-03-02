using UnityEditor;
using SkillEditor.Editor;
using UnityEngine;

namespace Game.Adapters.Editor
{
    /// <summary>
    /// 注入业务工厂到技能编辑器中
    /// 存在于 Editor 程序集，可以隐式引用 Runtime 程序集中的游戏层与编辑器层。
    /// 这样就实现了两端物理隔离而不发生程序集编译错误。
    /// </summary>
    public static class SkillServiceFactoryRegister
    {
        [InitializeOnLoadMethod]
        private static void RegisterToSkillEditor()
        {
            // 将业务层的 Service Factory 委托给核心编辑器
            SkillEditorGlobalSettings.DefaultServiceFactoryCreator = owner => new SkillServiceFactory(owner);
            
            // 注册编辑器窗口关停后置清理动作（防丢去重）
            SkillEditorGlobalSettings.OnEditorDispose -= SkillServiceFactory.ClearAllStaticCaches;
            SkillEditorGlobalSettings.OnEditorDispose += SkillServiceFactory.ClearAllStaticCaches;
        }
    }
}
