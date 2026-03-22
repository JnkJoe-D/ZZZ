using System.Collections;
using Game.Logic.Character;
using Game.Logic.Character.Config;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 运行时实机测试生成器：等待本地玩家生成后，在场景中刷出一个带行为树的敌人。
    /// </summary>
    public sealed class BehaviorTreeRuntimePlaytestSpawner : MonoBehaviour
    {
        [Header("Enemy Setup")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private CharacterConfigAsset enemyConfig;
        [SerializeField] private BehaviorTreeGraphAsset enemyBehaviorTree;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Vector3 enemySpawnOffset = new Vector3(5f, 0f, 5f);

        [Header("Flow")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool faceLocalPlayerOnSpawn = true;
        [SerializeField] private float waitForLocalPlayerTimeout = 10f;
        [SerializeField] private bool verboseLogging = true;

        private CharacterEntity spawnedEnemy;

        public CharacterEntity SpawnedEnemy => spawnedEnemy;

        /// <summary>
        /// 组件启动时按配置决定是否自动刷怪。
        /// </summary>
        private void Start()
        {
            if (spawnOnStart)
            {
                StartCoroutine(SpawnRoutine());
            }
        }
        void Update()
        {
            if(UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                SpawnNow();
            }
        }
        [ContextMenu("Spawn Playtest Enemy")]
        /// <summary>
        /// 手动立即生成测试敌人；若旧敌人存在则先销毁。
        /// </summary>
        public void SpawnNow()
        {
            if (spawnedEnemy != null)
            {
                Destroy(spawnedEnemy.gameObject);
                spawnedEnemy = null;
            }

            StartCoroutine(SpawnRoutine());
        }

        /// <summary>
        /// 等待玩家出生后，生成并初始化带行为树的敌人。
        /// </summary>
        /// <returns>协程枚举器。</returns>
        private IEnumerator SpawnRoutine()
        {
            float elapsed = 0f;
            CharacterEntity localPlayer = ResolveLocalPlayer();
            while (localPlayer == null && elapsed < waitForLocalPlayerTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
                localPlayer = ResolveLocalPlayer();
            }

            if (enemyPrefab == null || enemyConfig == null || enemyBehaviorTree == null)
            {
                Debug.LogWarning("[BT Playtest] Missing prefab/config/behavior tree reference.");
                yield break;
            }

            Vector3 spawnPosition = enemySpawnPoint != null
                ? enemySpawnPoint.position
                : (localPlayer != null ? localPlayer.transform.position + enemySpawnOffset : enemySpawnOffset);

            Quaternion spawnRotation = enemySpawnPoint != null
                ? enemySpawnPoint.rotation
                : Quaternion.identity;

            if (Game.Logic.Action.ActionManager.Instance != null)
            {
                var preloadTask = Game.Logic.Action.ActionManager.Instance.PreloadCharacterActionsAsync(enemyConfig);
                while (!preloadTask.IsCompleted)
                {
                    yield return null;
                }
            }

            GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, spawnRotation);
            enemyObject.name = enemyObject.name.Replace("(Clone)", string.Empty) + "_BTEnemy";

            if (faceLocalPlayerOnSpawn && localPlayer != null)
            {
                Vector3 lookDirection = localPlayer.transform.position - enemyObject.transform.position;
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    enemyObject.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                }
            }

            DisablePlayerInputProviders(enemyObject);

            CharacterEntity enemyCharacter = enemyObject.GetComponent<CharacterEntity>();
            if (enemyCharacter == null)
            {
                enemyCharacter = enemyObject.AddComponent<CharacterEntity>();
            }

            enemyCharacter.Init(enemyConfig);

            BehaviorTreeCharacterAgent agent = enemyObject.GetComponent<BehaviorTreeCharacterAgent>();
            if (agent == null)
            {
                agent = enemyObject.AddComponent<BehaviorTreeCharacterAgent>();
            }

            bool initialized = agent.TryInitialize(enemyBehaviorTree);
            if (!initialized)
            {
                Debug.LogWarning("[BT Playtest] Failed to initialize behavior tree agent.");
                Destroy(enemyObject);
                yield break;
            }

            spawnedEnemy = enemyCharacter;

            if (verboseLogging)
            {
                Debug.Log(
                    $"[BT Playtest] Spawned AI enemy '{spawnedEnemy.name}' at {spawnedEnemy.transform.position} with tree '{enemyBehaviorTree.name}'.");
            }
        }

        /// <summary>
        /// 兼容两套管理器来解析当前本地玩家角色。
        /// </summary>
        /// <returns>本地玩家角色；若尚未生成则返回空。</returns>
        private static CharacterEntity ResolveLocalPlayer()
        {
            return Game.Logic.Player.PlayerManager.Instance?.LocalCharacter ?? CharcterManager.Instance?.LocalCharacter;
        }

        /// <summary>
        /// 禁用实例对象上的玩家输入组件，避免和 AI 输入代理冲突。
        /// </summary>
        /// <param name="targetObject">要处理的对象。</param>
        private static void DisablePlayerInputProviders(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            Game.Input.LocalPlayerInputProvider[] inputProviders =
                targetObject.GetComponents<Game.Input.LocalPlayerInputProvider>();
            foreach (Game.Input.LocalPlayerInputProvider inputProvider in inputProviders)
            {
                if (inputProvider != null)
                {
                    inputProvider.enabled = false;
                }
            }
        }
    }
}
