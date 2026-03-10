using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能生成处理器接口
    /// 战斗系统需实现此接口，用于处理投射物/召唤物的实例化
    /// </summary>
    public interface ISkillSpawnHandler
    {
        /// <summary>
        /// 技能请求生成物体
        /// </summary>
        /// <param name="data">包含预制体、坐标、旋转、所有者等在内的完整生成参数包</param>
        /// <returns>生成的投射物逻辑控制接口</returns>
        ISkillProjectileHandler Spawn(SpawnData data);
        
        /// <summary>
        /// 技能提早或意外中断时，请求销毁相关的生成物
        /// </summary>
        /// <param name="projectile">之前生成的实例接口</param>
        void DestroySpawnedObject(ISkillProjectileHandler projectile);
    }

    /// <summary>
    /// 生成请求参数包
    /// </summary>
    public struct SpawnData
    {
        public GameObject configPrefab; // 预制体引用
        public Vector3 position;       // 初始世界坐标
        public Quaternion rotation;    // 初始世界旋转
        public bool detach;            // 是否脱离父节点
        public Transform parent;       // 挂载父节点（如果不脱离）
        public string eventTag;        // 事件标识
        public string[] targetTags;    // 目标标签过滤
        public GameObject deployer;    // 释放者（所有者）
    }
}
