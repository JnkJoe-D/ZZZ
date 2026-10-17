using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 生命周期设置卡片抽屉 (LifecycleConfig)
    /// </summary>
    public class LifecycleCardDrawer : CardDrawerBase<LifecycleConfig>
    {
        private readonly bool _isMandatory;
        public override bool IsMandatory => _isMandatory;
        public override string CardTitle => "生命周期设置";

        public LifecycleCardDrawer(bool isMandatory = true)
        {
            _isMandatory = isMandatory;
        }

        protected override void OnDrawBody(CardDrawContext context)
        {
            DrawSubField(Data, "destroyOnEnd");
            DrawSubField(Data, "maxLifeTime");
            DrawSubField(Data, "destroyOnInterrupt");
            DrawSubField(Data, "stopEmissionOnEnd");
        }
    }
}
