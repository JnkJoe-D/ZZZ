using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using SimpleJSON;
using cfg.ZZZ;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// Luban 角色数据项
    /// </summary>
    [Serializable]
    public class LubanCharacterInfo
    {
        public int CharacterId { get; }
        public string EnumName { get; }
        public string DisplayName { get; }

        public LubanCharacterInfo(int characterId, string enumName, string displayName)
        {
            CharacterId = characterId;
            EnumName = enumName ?? string.Empty;
            DisplayName = displayName ?? enumName ?? string.Empty;
        }

        public override string ToString()
        {
            return $"{DisplayName} ({EnumName}:{CharacterId})";
        }
    }

    /// <summary>
    /// 全流程工作台 - Luban 角色配表适配器
    /// 专供 Unity Editor 环境下桥接 Luban 角色表 (TbCharacterBase / CharacterId)，
    /// 为工作台提供角色列表、ID 绑定与元数据校验服务。
    /// </summary>
    public static class LubanCharacterSourceAdapter
    {
        public const string CharacterBaseConfigPath = "Assets/Configs/zzz_tbcharacterbase.json";

        private static List<LubanCharacterInfo> _cachedCharacters;
        private static Dictionary<int, LubanCharacterInfo> _idToCharacterMap;
        private static Dictionary<string, LubanCharacterInfo> _nameToCharacterMap;

        /// <summary>
        /// 获取当前配表中所有有效的角色列表
        /// </summary>
        public static IReadOnlyList<LubanCharacterInfo> GetAllCharacters()
        {
            EnsureLoaded();
            return _cachedCharacters;
        }

        /// <summary>
        /// 根据数字 CharacterId 获取角色信息
        /// </summary>
        public static bool TryGetCharacter(int characterId, out LubanCharacterInfo info)
        {
            EnsureLoaded();
            return _idToCharacterMap.TryGetValue(characterId, out info);
        }

        /// <summary>
        /// 根据枚举名称或显示名称获取角色信息（大小写不敏感）
        /// </summary>
        public static bool TryGetCharacterByName(string name, out LubanCharacterInfo info)
        {
            info = null;
            if (string.IsNullOrEmpty(name)) return false;

            EnsureLoaded();
            return _nameToCharacterMap.TryGetValue(name.Trim(), out info);
        }

        /// <summary>
        /// 校验角色 ID 或名称在 Luban 数据中是否有效
        /// </summary>
        public static bool IsCharacterValid(string characterIdOrName)
        {
            if (string.IsNullOrEmpty(characterIdOrName)) return false;

            if (int.TryParse(characterIdOrName, out int id))
            {
                return TryGetCharacter(id, out _);
            }

            return TryGetCharacterByName(characterIdOrName, out _);
        }

        /// <summary>
        /// 强制刷新缓存
        /// </summary>
        public static void Reload()
        {
            _cachedCharacters = null;
            _idToCharacterMap = null;
            _nameToCharacterMap = null;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (_cachedCharacters != null) return;

            _cachedCharacters = new List<LubanCharacterInfo>();
            _idToCharacterMap = new Dictionary<int, LubanCharacterInfo>();
            _nameToCharacterMap = new Dictionary<string, LubanCharacterInfo>(StringComparer.OrdinalIgnoreCase);

            // 1. 基于已编译的 cfg.ZZZ.CharacterId 枚举加载基础骨架
            var enumValues = (CharacterId[])Enum.GetValues(typeof(CharacterId));
            foreach (var val in enumValues)
            {
                if (val == CharacterId.None) continue;
                int id = (int)val;
                string enumName = val.ToString();
                string displayName = GetFallbackDisplayName(val);

                var info = new LubanCharacterInfo(id, enumName, displayName);
                AddCharacterInfo(info);
            }

            // 2. 尝试从 Assets/Configs/zzz_tbcharacterbase.json 补充校验与额外数据
            if (File.Exists(CharacterBaseConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(CharacterBaseConfigPath, Encoding.UTF8);
                    JSONNode root = JSONNode.Parse(json);
                    if (root != null && root.IsArray)
                    {
                        var jsonArray = root.AsArray;
                        for (int i = 0; i < jsonArray.Count; i++)
                        {
                            var row = jsonArray[i];
                            int characterId = row["characterId"].AsInt;
                            if (characterId > 0 && !_idToCharacterMap.ContainsKey(characterId))
                            {
                                string name = $"Role_{characterId}";
                                var info = new LubanCharacterInfo(characterId, name, name);
                                AddCharacterInfo(info);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LubanCharacterSourceAdapter] 读取配表 JSON 出现警告: {ex.Message}");
                }
            }
        }

        private static void AddCharacterInfo(LubanCharacterInfo info)
        {
            if (info == null) return;
            if (!_idToCharacterMap.ContainsKey(info.CharacterId))
            {
                _idToCharacterMap[info.CharacterId] = info;
                _cachedCharacters.Add(info);
            }

            _nameToCharacterMap[info.EnumName] = info;
            if (!string.IsNullOrEmpty(info.DisplayName) && !info.DisplayName.Equals(info.EnumName, StringComparison.OrdinalIgnoreCase))
            {
                _nameToCharacterMap[info.DisplayName] = info;
            }
        }

        private static string GetFallbackDisplayName(CharacterId id)
        {
            switch (id)
            {
                case CharacterId.Anbi: return "安比";
                case CharacterId.Miyabi: return "星见雅";
                case CharacterId.Ellen: return "艾莲";
                case CharacterId.QingYi: return "青衣";
                case CharacterId.PiPie: return "派派";
                default: return id.ToString();
            }
        }
    }
}
