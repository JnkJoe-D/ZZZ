using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.GamePlay
{
    public interface IRouteWindowProcesser
    {
        void OnEnter(WindowProcessContext ctx);
        void OnFrameProcess(WindowProcessContext ctx);
        void OnCommandReceived(CharacterCommand cmd, WindowProcessContext ctx);
        void OnExit(WindowProcessContext ctx);
        /// <summary>
        /// 窗口被打断注销时调用（如受击、强切技能打断）。
        /// 【红线约束】必须清理内部捕获状态，绝对不可向 L2 候选池提交任何数据。
        /// </summary>
        void OnDisable(WindowProcessContext ctx);
    }
}