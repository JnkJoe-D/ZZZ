using System;
using System.Threading.Tasks;
using UnityEngine;
using Game.Logic.Character;
using Game.Logic.Character.Config;
using Game.Resource;
using Game.Camera;
using Game.Framework;

namespace Game.Logic.Character
{
    /// <summary>
    /// 全局玩家管理器 (PlayerManager)
    /// 负责管理“屏幕前的玩家”的控制权，处理“附身 (Possess)”到具体的 CharacterEntity 上的逻辑。
    /// </summary>
    public class CharcterManager:Singleton<CharcterManager>
    {

        // 当前本机玩家附身控制的具体角色实体 (躯壳)
        public CharacterEntity LocalCharacter { get; private set; }

        public void Initialize()
        {
            
            
            Debug.Log("[PlayerManager] Initialized.");
        }

        public void Shutdown()
        {
            UnpossessCurrentCharacter();
        }

        public void Update(float deltaTime)
        {
            // 如果未来需要处理玩家按下了 ESC 键、呼出菜单暂停输入等，可以在这里检测
        }

        /// <summary>
        /// 异步加载并附身一个新的角色躯壳
        /// </summary>
        /// <param name="characterPrefabPath">资源路径</param>
        /// <param name="config">角色的基因数据配置</param>
        /// <param name="spawnPos">出生位置</param>
        /// <param name="spawnRot">出生旋转</param>
        public async Task<CharacterEntity> PossessNewCharacterAsync(string characterPrefabPath, CharacterConfigSO config, Vector3 spawnPos, Quaternion spawnRot)
        {
            UnpossessCurrentCharacter();

            // 1. 异步加载模型预制体
            var characterPrefab = await ResourceManager.Instance.LoadAssetAsync<GameObject>(characterPrefabPath);
            if (characterPrefab == null)
            {
                Debug.LogError($"[PlayerManager] Failed to load character prefab: {characterPrefabPath}");
                return null;
            }

            // 2. 实例化躯壳
            GameObject characterGo = UnityEngine.Object.Instantiate(characterPrefab, spawnPos, spawnRot);
            CharacterEntity characterEntity = characterGo.GetComponent<CharacterEntity>();
            if (characterEntity == null)
            {
                characterEntity = characterGo.AddComponent<CharacterEntity>();
            }

            // 3. 注入配置并初始化
            characterEntity.Init(config);

            // 4. 正式附身，移交控制权
            LocalCharacter = characterEntity;

            // 5. 转移相机焦点
            //    这代表屏幕前的玩家视线转移到了新的躯壳上
            GameCameraManager.Instance?.SetTarget(characterGo.transform);

            Debug.Log($"[PlayerManager] Successfully possessed new character: {config.RoleName}");
            return LocalCharacter;
        }

        /// <summary>
        /// 解除对当前躯壳的控制
        /// </summary>
        public void UnpossessCurrentCharacter()
        {
            if (LocalCharacter != null)
            {
                // 暂时处理为直接销毁。未来如果是联机或AI接管，则是拔掉 Input 并改变 State
                UnityEngine.Object.Destroy(LocalCharacter.gameObject);
                LocalCharacter = null;
                
                // 相机失去焦点
                GameCameraManager.Instance?.SetTarget(null);
                Debug.Log("[PlayerManager] Unpossessed current character.");
            }
        }

        /// <summary>
        /// 全局输入开关控制
        /// 可用于播片、放必杀技特写、开宝箱时剥夺玩家手柄控制权。
        /// </summary>
        public void SetInputEnable(bool enable)
        {
            if (LocalCharacter != null && LocalCharacter.InputProvider != null)
            {
                // 如果后续你实现了 InputProvider.SetEnable()，可以这里直接调用。
                // LocalCharacter.InputProvider.SetEnable(enable);
            }
        }
    }
}
