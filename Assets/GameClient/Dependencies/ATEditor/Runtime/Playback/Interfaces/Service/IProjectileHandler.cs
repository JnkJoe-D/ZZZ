using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 技能投射物接口（继承 ISpawnObject）
    /// 保持向下兼容的同时具备生成物的所有抽象规范。
    /// </summary>
    public interface IProjectileHandler : ISpawnObject
    {
    }
}
