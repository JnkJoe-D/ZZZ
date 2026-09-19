using System.Collections;
 
using Game.Framework;
using Game.GamePlay;

using Game.UI;
using UnityEngine;

namespace Game.App
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
        public Game.GamePlay.CharacterConfigAsset testCharacterConfig;
        public Game.GamePlay.TeamConfigAsset testPartyConfig;
        public Game.GamePlay.MonsterConfigAsset testMonsterConfig;
        public Transform spawnPoint;

        public bool IsSpawnCompleted { get; private set; }
        public bool IsSpawnSucceeded { get; private set; }

        private void Start()
        {
            var tm = TimeManager.Instance; // 初始化单例
            StartCoroutine(InitializeSequence());
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            TimeManager.Instance?.Update();
            EventCenter.FlushPending();

            if (GameCameraManager.Instance != null)
            {
                float uiDelta = Time.unscaledDeltaTime * (TimeManager.Instance != null ? TimeManager.Instance.FinalUIScale : 1f);
                GameCameraManager.Instance.Update(uiDelta);
            }
        }

        private IEnumerator SpawnRoutine()
        {
            yield return new WaitForLogicSeconds(0.5f);

            IsSpawnCompleted = false;
            IsSpawnSucceeded = false;

            if (TeamManager.Instance == null || (testCharacterConfig == null && testPartyConfig == null))
            {
                IsSpawnCompleted = true;
                GLog.Warning(LogTags.Player, "CharacterManager or party configuration is not ready.");
                yield break;
            }

            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GLog.Info(LogTags.Player, "Requesting CharacterManager to spawn controllable role or party...");
            System.Threading.Tasks.Task<RoleEntity> spawnTask = testPartyConfig != null
                ? TeamManager.Instance.InitializePartyAsync(testPartyConfig, pos, rot)
                : TeamManager.Instance.PossessNewCharacterAsync(
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
                GLog.Exception(LogTags.GameRoot, spawnTask.Exception);
            }
            else if (!IsSpawnSucceeded)
            {
                GLog.Warning(LogTags.Player, "Character spawn completed without returning a valid RoleEntity.");
            }
            else
            {
                UIManager.Instance.Open<Game.UI.StatusPanelModule>();
                UIManager.Instance.Open<SkillKeyModule>();
            }
        }

        private IEnumerator InitializeSequence()
        {
            GLog.Info(LogTags.GameRoot, "===== Game Start =====");

            GlobalPoolManager.Initialize();
            GLog.Info(LogTags.GameRoot, "[1/9] Pool ... OK");
            yield return null;

            FSMManager.Instance.Initialize();
            GLog.Info(LogTags.GameRoot, "[2/11] FSM ... OK");

            UIManager.Instance.Initialize(this);
            GLog.Info(LogTags.GameRoot, "[4/11] UI ... OK");
            yield return null;

            yield return StartCoroutine(ResourceManager.Instance.InitializeAsync(_resourceConfig, this));
            if (!ResourceManager.Instance.IsInitialized)
            {
                GLog.Error(LogTags.GameRoot, "ResourceManager initialization failed.");
                yield break;
            }
            GLog.Info(LogTags.GameRoot, "[4/9] Assets ... OK");

            ATEditor.Runtime.ActionSystemContext.InjectAssetLoader(new ATAssetLoader());
            GLog.Info(LogTags.GameRoot, "[4.5/9] SkillEditor AssetLoader Injected ... OK");
            yield return null;

            var configMgrTask = ConfigManager.Instance.InitializeAsync();
            while (!configMgrTask.IsCompleted)
            {
                yield return null;
            }
            GLog.Info(LogTags.GameRoot, "[5/9] Config ... OK");
            yield return null;

            GLog.Info(LogTags.GameRoot, "[6/9] Lua ... (TODO: XLua)");

            AudioManager.Instance.Initialize();
            GLog.Info(LogTags.GameRoot, "[7/9] System Audio ... OK");

            SceneManager.Instance.Initialize(this);
            GLog.Info(LogTags.GameRoot, "[8/10] Scene ... OK");

            Game.GamePlay.InputManager.Instance.Initialize();
            GLog.Info(LogTags.GameRoot, "[9/11] Input ... OK");

            GameCameraManager.Instance.Initialize();
            Game.Presentation.TeamCameraPresenter.Instance.Initialize();

            Game.GamePlay.TeamManager.Instance.Initialize();
            Game.GamePlay.MonsterManager.Instance.Initialize();

            // 自动挂载并初始化波次管理器 (以便测试 F1 刷怪)
            var waveManager = gameObject.GetComponent<Game.GamePlay.WaveManager>();
            if (waveManager == null)
            {
                waveManager = gameObject.AddComponent<Game.GamePlay.WaveManager>();
            }
            waveManager.TestMonsterConfig = testMonsterConfig;
            waveManager.TestSpawnPoint = spawnPoint;

            StartCoroutine(SpawnRoutine());
        }

        private void OnDestroy()
        {
            Game.GamePlay.MonsterManager.Instance?.Shutdown();
            Game.GamePlay.TeamManager.Instance?.Shutdown();
            Game.GamePlay.ActionManager.Instance?.Shutdown();
        }
    }
}
