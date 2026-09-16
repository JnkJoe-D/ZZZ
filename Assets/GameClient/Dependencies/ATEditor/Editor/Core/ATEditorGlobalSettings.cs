using System;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 技能编辑器全局设置
    /// 用于解耦编辑器核心功能与具体项目的业务逻辑
    /// </summary>
    public static class ATEditorGlobalSettings
    {

        /// <summary>
        /// 编辑器销毁的回调通知　
        /// 业务层可在此注销或清理自身在编辑器态下的静态缓存　
        /// </summary>
        public static Action OnEditorDispose { get; set; }
    }
}
