using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 项目级 ScriptableObject 基类。
    /// 所有配置资产继承此类后，自动获得增强的 Inspector 绘制能力：
    /// - 自动迭代所有序列化属性（添加新字段无需修改任何 Editor 代码）
    /// - 自动支持 [ShowIf] 条件显示/隐藏
    /// - 自动支持 [SerializeReference, SubclassSelector] 多态下拉选择
    /// - 自动支持 [Header], [Tooltip], [Min], [Range] 等所有标准 Unity 属性
    /// </summary>
    public abstract class GameConfigAsset : ScriptableObject
    {
    }
}
