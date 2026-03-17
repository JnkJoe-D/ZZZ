using System;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 服务工厂接口，用于 ProcessContext 的服务懒加载
    /// </summary>
    public interface IServiceFactory
    {
        /// <summary>
        /// 提供指定类型的服务实例
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// /// <param name="owner"></param>
        /// <returns>服务实例，若无法提供则返回 null</returns>
        object ProvideService(Type serviceType,GameObject owner);
    }
}
