using ATEditor;
using MAnimSystem;

namespace Game.GamePlay
{
    /// <summary>旧 Gameplay 装配入口；动画实现复用 ATEditor 默认后端，保留外部时间与图所有权。</summary>
    public class ATAnimationHandler : DefaultAnimHandler
    {
        public ATAnimationHandler(AnimComponent target) : base(target) { }
    }
}
