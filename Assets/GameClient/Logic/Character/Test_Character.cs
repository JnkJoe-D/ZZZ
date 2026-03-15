using System.Collections;
using Game.Config;
using Game.FSM;
using Game.Framework;
using Game.Pool;
using Game.Resource;
using Game.Scene;
using Game.UI;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// Lightweight runtime bootstrap used for local character spawning tests.
    /// </summary>
    public class Test_Character : MonoBehaviour
    {
        [Header("Resource Config")]
        [SerializeField] private ResourceConfig _resourceConfig;

        [Header("Test Spawner Config")]
        public string characterPrefabPath = "Assets/Resources/Character_Player.prefab";
        public Game.Logic.Character.Config.CharacterConfigAsset testCharacterConfig;
        public Transform spawnPoint;

        public bool IsSpawnCompleted { get; private set; }
        public bool IsSpawnSucceeded { get; private set; }

        private void Start()
        {
            StartCoroutine(InitializeSequence());
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private IEnumerator SpawnRoutine()
        {
            yield return new WaitForSeconds(0.5f);

            IsSpawnCompleted = false;
            IsSpawnSucceeded = false;

            if (CharcterManager.Instance == null || testCharacterConfig == null)
            {
                IsSpawnCompleted = true;
                Debug.LogWarning("[Test_Character] CharacterManager or testCharacterConfig is not ready.");
                yield break;
            }

            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            Debug.Log("[Test_Character] Requesting CharacterManager to spawn character...");
            var spawnTask = CharcterManager.Instance.PossessNewCharacterAsync(
                characterPrefabPath,
                testCharacterConfig,
                pos,
                rot);

            while (!spawnTask.IsCompleted)
            {
                yield return null;
            }

            IsSpawnCompleted = true;
            IsSpawnSucceeded = !spawnTask.IsFaulted && !spawnTask.IsCanceled && spawnTask.Result != null;

            if (spawnTask.IsFaulted)
            {
                Debug.LogException(spawnTask.Exception);
            }
            else if (!IsSpawnSucceeded)
            {
                Debug.LogWarning("[Test_Character] Character spawn completed without returning a valid CharacterEntity.");
            }
        }

        private IEnumerator InitializeSequence()
        {
            Debug.Log("[GameRoot] ===== Game Start =====");

            GlobalPoolManager.Initialize();
            Debug.Log("[GameRoot] [1/9] Pool ... OK");
            yield return null;

            FSMManager.Instance.Initialize();
            Debug.Log("[GameRoot] [2/11] FSM ... OK");

            UIManager.Instance.Initialize(this);
            Debug.Log("[GameRoot] [4/11] UI ... OK");
            yield return null;

            yield return StartCoroutine(ResourceManager.Instance.InitializeAsync(_resourceConfig, this));
            if (!ResourceManager.Instance.IsInitialized)
            {
                Debug.LogError("[GameRoot] ResourceManager initialization failed.");
                yield break;
            }
            Debug.Log("[GameRoot] [4/9] Assets ... OK");

            SkillEditor.Runtime.SkillSystemContext.InjectAssetLoader(new Game.Adapters.SkillAssetLoader());
            Debug.Log("[GameRoot] [4.5/9] SkillEditor AssetLoader Injected ... OK");
            yield return null;

            var configMgrTask = ConfigManager.Instance.InitializeAsync();
            while (!configMgrTask.IsCompleted)
            {
                yield return null;
            }
            Debug.Log("[GameRoot] [5/9] Config ... OK");
            yield return null;

            Debug.Log("[GameRoot] [6/9] Lua ... (TODO: XLua)");

            Game.Audio.AudioManager.Instance.Initialize();
            Debug.Log("[GameRoot] [7/9] System Audio ... OK");

            SceneManager.Instance.Initialize(this);
            Debug.Log("[GameRoot] [8/10] Scene ... OK");

            Game.Input.InputManager.Instance.Initialize();
            Debug.Log("[GameRoot] [9/11] Input ... OK");

            Game.Camera.GameCameraManager.Instance.Initialize();

            StartCoroutine(SpawnRoutine());
        }
    }
}
