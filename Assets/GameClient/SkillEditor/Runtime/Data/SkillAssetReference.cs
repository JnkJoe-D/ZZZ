using System;

namespace SkillEditor
{
    /// <summary>
    /// 资源引用元数据类
    /// 用于存储资源的 GUID、名称和路径，以便在不加载实际资源的情况下进行寻址或校验。
    /// </summary>
    [Serializable]
    public class SkillAssetReference
    {
        public string guid;
        public string assetName;
        public string assetPath;

        public SkillAssetReference()
        {
            guid = string.Empty;
            assetName = string.Empty;
            assetPath = string.Empty;
        }

        public SkillAssetReference(string guid, string assetName, string assetPath)
        {
            this.guid = guid;
            this.assetName = assetName;
            this.assetPath = assetPath;
        }

        public void Clear()
        {
            guid = string.Empty;
            assetName = string.Empty;
            assetPath = string.Empty;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(guid);
        }
    }
}
