using System.Collections;
using UnityEngine;
using Game.Pool;
using Game.Resource;
using Game.Network;
using Game.Scene;
using Game.UI;
using Game.Config;
using Game.FSM;
using Game.Framework;
using cfg;

namespace Game.Logic.Character
{
    /// <summary>
    /// 测试脚手架：用于在沙盒场景快速生成玩家控制的具体躯壳。
    /// 实际工业应用中，该逻辑会由更上层的副本/登录流程管理。
    /// </summary>
    public class Test_Character : MonoBehaviour
    {
        [Header("资源管理配置")]
        [SerializeField] private ResourceConfig _resourceConfig;
        [Header("Test Spawner Config")]
        public string characterPrefabPath = "Assets/Resources/Character_Player.prefab";
        public Game.Logic.Character.Config.CharacterConfigSO testCharacterConfig;

        public Transform spawnPoint;

        private void Start()
        {
            StartCoroutine(InitializeSequence());
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private IEnumerator SpawnRoutine()
        {
            // 等待 GameRoot 所有的管理器就绪
            yield return new WaitForSeconds(0.5f);

            if (CharcterManager.Instance != null && testCharacterConfig != null)
            {
                var pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
                var rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
                
                Debug.Log("[Test_Character] Requesting PlayerManager to spawn character...");
                CharcterManager.Instance.PossessNewCharacterAsync(
                    characterPrefabPath, 
                    testCharacterConfig, 
                    pos, 
                    rot
                );
            }
            else
            {
                Debug.LogWarning("[Test_Character] PlayerManager 或 Config 未就绪，无法生成测试角色。");
            }
        }
        private IEnumerator InitializeSequence()
        {
            Debug.Log("[GameRoot] ===== 游戏启动 =====");

            // ── Step 1: 全局对象池 ────────────────────
            GlobalPoolManager.Initialize();
            Debug.Log("[GameRoot] [1/9] Pool ... OK");
            yield return null;

            // ── Step 2: 核心底层服务启机 ─────────────────────
            FSMManager.Instance.Initialize();
            Debug.Log("[GameRoot] [2/11] FSM ... OK");

            UIManager.Instance.Initialize(this);
            Debug.Log("[GameRoot] [4/11] UI ... OK");
            yield return null;

            // ── Step 4: 资源管理器（YooAsset）────────────────
            yield return StartCoroutine(
                ResourceManager.Instance.InitializeAsync(_resourceConfig, this)
            );
            if (!ResourceManager.Instance.IsInitialized)
            {
                Debug.LogError("[GameRoot] 资源管理器初始化失败，游戏终止");
                yield break;
            }
            Debug.Log("[GameRoot] [4/9] Assets ... OK");

            // ── Step 4.5: 向技能编辑器注入资源加载适配器 ────────────────
            SkillEditor.Runtime.SkillSystemContext.InjectAssetLoader(new Game.Adapters.SkillAssetLoaderAdapter());
            Debug.Log("[GameRoot] [4.5/9] SkillEditor AssetLoader Injected ... OK");

            yield return null;

            // ── Step 5: 基础配置加载 ──────────────────
            var configMgrTask = ConfigManager.Instance.InitializeAsync();
            while (!configMgrTask.IsCompleted) yield return null;
            Debug.Log("[GameRoot] [5/9] Config ... OK");
            yield return null;

            // ── Step 6: Lua 音频等 ───────────────
            Debug.Log("[GameRoot] [6/9] Lua ... (TODO: XLua)");

            // ── Step 7: 全局音频管理器 ───────────────
            Game.Audio.AudioManager.Instance.Initialize();
            Debug.Log("[GameRoot] [7/9] System Audio ... OK");

            // ── Step 8: 场景管理器 ────────────────────
            SceneManager.Instance.Initialize(this);
            Debug.Log("[GameRoot] [8/10] Scene ... OK");

            // ── Step 9: 输入管理器 ────────────────────
            Game.Input.InputManager.Instance.Initialize();
            Debug.Log("[GameRoot] [9/11] Input ... OK");

            // ── Step 10: 相机管理器 ───────────────────
            Game.Camera.GameCameraManager.Instance.Initialize();

            // ── Step 11: 玩家连接层管理器 ────────────────
            Game.Logic.Player.PlayerManager.Instance.Initialize();
            Debug.Log("[GameRoot] [11/11] Player Manager ... OK");

            StartCoroutine(SpawnRoutine());
        }
    }
}