using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Logic;

namespace Game.Logic.Level
{
    /// <summary>
    /// 轻量级波次/刷怪管理器。
    /// 负责按波次、按时间节点触发怪物生成，并监听怪物死亡以推进进度。
    /// 处于 LevelManager (业务层) 的概念，调用 MonsterManager (执行层)。
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Serializable]
        public class SpawnInfo
        {
            [Tooltip("怪物的配置资产")]
            public MonsterConfigAsset MonsterConfig;
            [Tooltip("怪物生成的坐标和朝向点")]
            public Transform SpawnPoint;
        }

        [Serializable]
        public class WaveData
        {
            [Tooltip("本波次刷出前的延迟等待时间(秒)")]
            public float DelayBeforeWave = 1f;
            [Tooltip("本波次需要生成的所有怪物")]
            public List<SpawnInfo> Spawns = new List<SpawnInfo>();
        }

        [Header("波次配置")]
        public List<WaveData> Waves = new List<WaveData>();
        
        [Header("全局设置")]
        [Tooltip("是否在 Start 时自动开始刷怪流")]
        public bool AutoStart = true;

        [Header("测试功能 (Debug)")]
        [Tooltip("按下 F1 时强制手动刷出的怪物")]
        public MonsterConfigAsset TestMonsterConfig;
        [Tooltip("F1 刷怪点")]
        public Transform TestSpawnPoint;

        private int _currentWaveIndex = 0;
        private List<MonsterEntity> _currentAliveMonsters = new List<MonsterEntity>();
        private bool _isSpawning = false;
        private bool _isFinished = false;

        private void Start()
        {
            if (AutoStart)
            {
                StartWaveFlow();
            }
        }

        /// <summary>
        /// 提供给外部系统(如触发器)的启动接口
        /// </summary>
        public void StartWaveFlow()
        {
            if (Waves == null || Waves.Count == 0) return;
            StartCoroutine(StartWavesRoutine());
        }

        private IEnumerator StartWavesRoutine()
        {
            Debug.Log("[WaveManager] 开始提取全波次怪物配置并下发预加载指令...");
            
            // 1. 汇总当前关卡/当前节点所需的所有去重怪物配置
            var allConfigs = new HashSet<MonsterConfigAsset>();
            foreach(var wave in Waves)
            {
                foreach(var spawn in wave.Spawns)
                {
                    if (spawn.MonsterConfig != null)
                    {
                        allConfigs.Add(spawn.MonsterConfig);
                    }
                }
            }

            // 2. 将数据层的配置剥离，下发给物理执行层 MonsterManager 进行异步预载
            var loadTask = MonsterManager.Instance.PreloadMonstersAsync(new List<MonsterConfigAsset>(allConfigs));
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            Debug.Log("[WaveManager] 全怪物资源预加载完成，开始第一波出怪。");
            
            // 3. 预载完毕，正式开始波次状态机
            SpawnNextWave();
        }

        private void SpawnNextWave()
        {
            if (_currentWaveIndex >= Waves.Count)
            {
                _isFinished = true;
                Debug.Log("[WaveManager] 所有波次均已清理完毕！关卡/节点完成。");
                // TODO: 在这里可以触发结界开启、掉落宝箱、进入下一段剧情等全局事件
                return;
            }

            StartCoroutine(SpawnWaveRoutine(Waves[_currentWaveIndex]));
        }

        private IEnumerator SpawnWaveRoutine(WaveData wave)
        {
            _isSpawning = true;
            
            if (wave.DelayBeforeWave > 0)
            {
                yield return new WaitForSeconds(wave.DelayBeforeWave);
            }

            _currentAliveMonsters.Clear();

            // 循环下发出生指令给 MonsterManager
            foreach (var spawn in wave.Spawns)
            {
                if (spawn.MonsterConfig == null || spawn.SpawnPoint == null) continue;

                var spawnTask = MonsterManager.Instance.SpawnMonsterAsync(
                    spawn.MonsterConfig, 
                    spawn.SpawnPoint.position, 
                    spawn.SpawnPoint.rotation);

                while (!spawnTask.IsCompleted)
                {
                    yield return null;
                }

                var monster = spawnTask.Result;
                if (monster != null)
                {
                    _currentAliveMonsters.Add(monster);
                }
            }

            _isSpawning = false;
            _currentWaveIndex++;
            
            Debug.Log($"[WaveManager] 第 {_currentWaveIndex} 波怪物已生成，共 {_currentAliveMonsters.Count} 只。");
        }

        private void Update()
        {
            // ===== Debug: F1 手动刷怪 =====
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                SpawnTestMonster();
            }

            // 如果还在生成过程中，或者已经结束了，就不检查死亡条件
            if (_isSpawning || _isFinished || _currentAliveMonsters.Count == 0) return;
            
            // 轮询检查怪物的存活状态 (轻量级做法：检查是否被回收入池而失活)
            // 在完整的框架中，可以通过监听 MonsterManager 派发的 EntityDiedEvent 来做
            for (int i = _currentAliveMonsters.Count - 1; i >= 0; i--)
            {
                var monster = _currentAliveMonsters[i];
                if (monster == null || !monster.gameObject.activeInHierarchy)
                {
                    _currentAliveMonsters.RemoveAt(i);
                }
            }

            // 当前波次的怪物被清理干净了
            if (_currentAliveMonsters.Count == 0)
            {
                Debug.Log($"[WaveManager] 第 {_currentWaveIndex} 波怪物已全灭，准备下一波...");
                SpawnNextWave();
            }
        }

        private async void SpawnTestMonster()
        {
            if (TestMonsterConfig == null)
            {
                Debug.LogWarning("[WaveManager] TestMonsterConfig is null, 无法手动刷怪！请在 Inspector 赋值。");
                return;
            }

            Vector3 pos = TestSpawnPoint != null ? TestSpawnPoint.position : Vector3.zero;
            Quaternion rot = TestSpawnPoint != null ? TestSpawnPoint.rotation : Quaternion.identity;

            // 如果该怪物的资源之前没预载过，可能动作会丢，或者你需要在这里补一刀预载。
            // 这里假定已在波次里配了或者手动挂了。
            var monster = await MonsterManager.Instance.SpawnMonsterAsync(TestMonsterConfig, pos, rot);
            
            if (monster != null)
            {
                Debug.Log($"[WaveManager] 已强制生成测试怪：{TestMonsterConfig.Name}");
                _currentAliveMonsters.Add(monster);
            }
        }
    }
}
