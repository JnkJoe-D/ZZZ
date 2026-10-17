using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 生成与挂载设置卡片抽屉 (TransformBindConfig)
    /// </summary>
    public class TransformBindCardDrawer : CardDrawerBase<TransformBindConfig>
    {
        private readonly bool _isMandatory;
        public override bool IsMandatory => _isMandatory;
        public override string CardTitle => "生成与挂载设置";

        public TransformBindCardDrawer(bool isMandatory = true)
        {
            _isMandatory = isMandatory;
        }

        protected override void OnDrawBody(CardDrawContext context)
        {
            DrawSubField(Data, "bindPoint");
            if (Data.bindPoint == BindPoint.CustomBone)
            {
                Data.customBoneName = EditorGUILayout.TextField("自定义骨骼名", Data.customBoneName);
            }
            DrawSubField(Data, "positionOffset");
            DrawSubField(Data, "rotationOffset");
            DrawSubField(Data, "scale");
            DrawSubField(Data, "followTarget");
            if (Data.followTarget)
            {
                DrawSubField(Data, "followMode");
            }
        }
    }
}
