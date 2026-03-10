using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能角色接口
    /// 角色脚本需实现此接口，以便 SkillEditor 获取挂点
    /// </summary>
    public interface ISkillBoneGetter
    {
        /// <summary>
        /// 获取特效挂载点 Transform
        /// </summary>
        /// <param name="point">挂点类型</param>
        /// <param name="customName">自定义骨骼名（仅当 point 为 CustomBone 时使用）</param>
        /// <returns>挂点 Transform</returns>
        Transform GetBone(BindPoint point, string customName = "");
    }
}
