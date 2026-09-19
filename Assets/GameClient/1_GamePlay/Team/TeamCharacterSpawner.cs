using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 编队角色生成与资产装配工厂。
    /// 负责队伍角色的动作预加载、模型预制体解析与物理生成装配。
    /// </summary>
    public class TeamCharacterSpawner
    {
        public async Task PreloadPartyActionsAsync(IEnumerable<CharacterConfigAsset> configs)
        {
            if (configs == null || ActionManager.Instance == null) return;

            var loadTasks = new List<Task>(8);
            foreach (var config in configs)
            {
                if (config != null)
                {
                    loadTasks.Add(ActionManager.Instance.PreloadCharacterActionsAsync(config));
                }
            }

            if (loadTasks.Count > 0)
            {
                await Task.WhenAll(loadTasks);
            }
        }

        public async Task<GameObject> ResolveCharacterPrefabAsync(CharacterConfigAsset config, string prefabPathOverride = null)
        {
            if (config != null && config.Prefab != null)
            {
                return config.Prefab;
            }

            if (!string.IsNullOrEmpty(prefabPathOverride))
            {
                return await ResourceManager.Instance.LoadAssetAsync<GameObject>(prefabPathOverride);
            }

            return null;
        }

        public RoleEntity SpawnRoleEntity(
            CharacterConfigAsset config,
            GameObject prefab,
            Vector3 spawnPos,
            Quaternion spawnRot,
            RoleTeamContext teamContext,
            System.Action<RoleEntity> onBeforeInit = null)
        {
            if (prefab == null)
            {
                GLog.Error(LogTags.Team, $"SpawnRoleEntity failed: Prefab is null for config {(config != null ? config.Name : "null")}");
                return null;
            }

            GameObject characterGo = Object.Instantiate(prefab, spawnPos, spawnRot);
            
            RoleEntity entity = characterGo.GetComponent<RoleEntity>();
            if (entity == null)
            {
                entity = characterGo.AddComponent<RoleEntity>();
            }

            onBeforeInit?.Invoke(entity);

            if (teamContext != null)
            {
                entity.AssignTeamContext(teamContext);
            }

            entity.Init(config);
            entity.EnsureRuntimeInitialized();
            entity.SetControlActive(false);
            entity.CommandBuffer?.Clear();

            // 显式时钟生命周期装配：挂载至角色时钟通道
            TimeManager.Instance?.RegisterRoleClock(entity.Clock);

            return entity;
        }
    }
}
