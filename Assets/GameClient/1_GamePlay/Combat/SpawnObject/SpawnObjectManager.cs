using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Framework;
using ATEditor;

namespace Game.GamePlay
{
    /// <summary>
    /// 全局生成物集中管理器 (SpawnObjectManager)
    /// 统一接管所有生成物（投射物/召唤物/机关等）的生命周期、对象池存取、大盘索引与跨场景集中清理。
    /// 底层无缝桥接框架对象池 GlobalPoolManager，与轻量化适配器 ATSpawnHandler 协同交互。
    /// </summary>
    public class SpawnObjectManager : Singleton<SpawnObjectManager>
    {
        // ── 活跃生成物大盘管理 ──
        private readonly List<ISpawnObject> _activeObjects = new List<ISpawnObject>();
        public IReadOnlyList<ISpawnObject> ActiveObjects => _activeObjects;
        public int ActiveCount => _activeObjects.Count;

        // ── 释放者与生成物双向映射 ──
        private readonly Dictionary<CharacterEntity, List<ISpawnObject>> _ownerObjects = new Dictionary<CharacterEntity, List<ISpawnObject>>();
        private readonly Dictionary<ISpawnObject, CharacterEntity> _objectToOwner = new Dictionary<ISpawnObject, CharacterEntity>();

        // ── 预分配遍历缓冲区（避免 GC Alloc） ──
        private readonly List<ISpawnObject> _recycleBuffer = new List<ISpawnObject>();

        public SpawnObjectManager()
        {
            EventCenter.Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            if (evt.Victim != null)
            {
                RecycleAttachedByOwner(evt.Victim);
            }
        }

        /// <summary>
        /// 核心生成接口：从框架对象池实例化/复用生成物并完成大盘登记与生命周期托管
        /// </summary>
        /// <param name="data">ATEditor 运行时生成的参数包</param>
        /// <param name="owner">释放者角色实体（用于提取时钟与建立归属关系）</param>
        /// <returns>生成的 ISpawnObject 实例控制接口</returns>
        public ISpawnObject Spawn(SpawnData data, CharacterEntity owner = null)
        {
            if (data.configPrefab == null)
            {
                GLog.Warning(LogTags.Combat, "[SpawnObjectManager] 生成失败：SpawnData.configPrefab 为空！");
                return null;
            }

            Transform targetParent = (!data.detach && data.parent != null) ? data.parent : null;

            // 1. 通过框架对象池 GlobalPoolManager 统一获取实例
            GameObject instance = GlobalPoolManager.Spawn(data.configPrefab, data.position, data.rotation, targetParent);
            if (instance == null)
            {
                GLog.Error(LogTags.Combat, $"[SpawnObjectManager] GlobalPoolManager.Spawn 失败：{data.configPrefab.name}");
                return null;
            }

            instance.SetActive(true);

            // 2. 识别或装配 ISpawnObject 业务组件
            ISpawnObject sp = instance.GetComponent<ISpawnObject>() ?? instance.AddComponent<SpawnObjectEntity>();

            // 3. 初始化并注入所有者私有时钟流速
            if (sp is SpawnObjectEntity spawnEntity)
            {
                spawnEntity.Initialize(data, null, owner?.Clock);
            }
            else
            {
                sp.Initialize(data, null);
            }

            // 4. 登记至大盘管理与所有者映射
            if (!_activeObjects.Contains(sp))
            {
                _activeObjects.Add(sp);
            }
            RegisterOwner(sp, owner);

            return sp;
        }

        /// <summary>
        /// 原生 GameObject 对象池生成（底层桥接 GlobalPoolManager）
        /// </summary>
        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, bool detach, Transform parent)
        {
            if (prefab == null) return null;
            Transform targetParent = (!detach && parent != null) ? parent : null;
            var instance = GlobalPoolManager.Spawn(prefab, position, rotation, targetParent);
            if (instance != null)
            {
                instance.SetActive(true);
            }
            return instance;
        }

        /// <summary>
        /// 核心回收接口：统一将生成物归还至框架对象池并移出大盘管理
        /// </summary>
        public void Recycle(ISpawnObject spawnObject)
        {
            if (spawnObject == null) return;

            // 1. 从大盘列表中剔除
            _activeObjects.Remove(spawnObject);
            UnregisterOwner(spawnObject);

            // 2. 表现层逻辑终止（粒子停止发射、拖尾清理、碰撞失活）
            try
            {
                spawnObject.Terminate();
            }
            catch (Exception ex)
            {
                GLog.Exception(LogTags.Combat, ex);
            }

            // 3. 归还至框架对象池
            GameObject go = spawnObject.GameObject;
            if (go != null && go.scene.isLoaded)
            {
                go.SetActive(false);
                GlobalPoolManager.Return(go);
            }
        }

        /// <summary>
        /// 回收指定释放者产生的所有活跃生成物
        /// </summary>
        public void RecycleByOwner(CharacterEntity owner)
        {
            if (owner == null) return;
            if (!_ownerObjects.TryGetValue(owner, out var list) || list == null || list.Count == 0) return;

            _recycleBuffer.Clear();
            _recycleBuffer.AddRange(list);

            for (int i = 0; i < _recycleBuffer.Count; i++)
            {
                var obj = _recycleBuffer[i];
                if (obj != null)
                {
                    Recycle(obj);
                }
            }

            _recycleBuffer.Clear();
        }

        /// <summary>
        /// 回收指定实体身上挂载的（未脱离自身的）生成物
        /// </summary>
        public void RecycleAttachedByOwner(CharacterEntity owner)
        {
            if (owner == null) return;
            if (!_ownerObjects.TryGetValue(owner, out var list) || list == null || list.Count == 0) return;

            _recycleBuffer.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var obj = list[i];
                if (obj != null && obj.GameObject != null)
                {
                    if (obj.GameObject.transform.IsChildOf(owner.transform))
                    {
                        _recycleBuffer.Add(obj);
                    }
                }
            }

            for (int i = 0; i < _recycleBuffer.Count; i++)
            {
                Recycle(_recycleBuffer[i]);
            }
            _recycleBuffer.Clear();
        }

        /// <summary>
        /// 全局清扫：回收当前所有处于活跃状态的生成物（适合波次重置、战斗结束或场景切换时调用）
        /// </summary>
        public void RecycleAll()
        {
            if (_activeObjects.Count == 0) return;

            _recycleBuffer.Clear();
            _recycleBuffer.AddRange(_activeObjects);

            for (int i = 0; i < _recycleBuffer.Count; i++)
            {
                var obj = _recycleBuffer[i];
                if (obj != null)
                {
                    Recycle(obj);
                }
            }

            _recycleBuffer.Clear();
            _activeObjects.Clear();
            _ownerObjects.Clear();
            _objectToOwner.Clear();
        }

        private void RegisterOwner(ISpawnObject obj, CharacterEntity owner)
        {
            if (obj == null || owner == null) return;
            _objectToOwner[obj] = owner;
            if (!_ownerObjects.TryGetValue(owner, out var list))
            {
                list = new List<ISpawnObject>();
                _ownerObjects[owner] = list;
            }
            if (!list.Contains(obj))
            {
                list.Add(obj);
            }
        }

        private void UnregisterOwner(ISpawnObject obj)
        {
            if (obj == null) return;
            if (_objectToOwner.TryGetValue(obj, out var owner))
            {
                _objectToOwner.Remove(obj);
                if (owner != null && _ownerObjects.TryGetValue(owner, out var list))
                {
                    list.Remove(obj);
                    if (list.Count == 0)
                    {
                        _ownerObjects.Remove(owner);
                    }
                }
            }
        }

        public void Shutdown()
        {
            EventCenter.Unsubscribe<EntityDiedEvent>(OnEntityDied);
            RecycleAll();
        }
    }
}
