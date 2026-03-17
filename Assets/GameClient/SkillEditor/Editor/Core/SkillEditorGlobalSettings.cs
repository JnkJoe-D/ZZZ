using System;
using UnityEngine;
using SkillEditor;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 技能编辑器全局设置
    /// 用于解耦编辑器核心功能与具体项目的业务逻辑
    /// </summary>
    public static class SkillEditorGlobalSettings
    {
        /// <summary>
        /// 服务工厂创建委托。
        /// 业务层可通过 [UnityEditor.InitializeOnLoadMethod] 注册本委托，
        /// 供 SkillEditorWindow 在构建预览 ProcessContext 时获取对应业务的服务层。
        /// </summary>
        public static Func<GameObject, Func<Type, GameObject, object>> DefaultServiceFactoryCreator { get; set; }

        /// <summary>
        /// 编辑器销毁的回调通知。
        /// 业务层可在此注销或清理自身在编辑器态下的静态缓存。
        /// </summary>
        public static Action OnEditorDispose { get; set; }
    }
}
