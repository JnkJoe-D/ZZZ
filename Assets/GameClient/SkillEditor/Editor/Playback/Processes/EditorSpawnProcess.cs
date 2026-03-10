using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 编辑器模式下的特效/子物体生成处理
    /// 当播放器进入 EditorPreview 模式时，它将在场景中生成被标记为 HideAndDontSave 的临时对象，
    /// 以防止这些生成的特性或弹道被误存到场景中，并在离开时自动清理。
    /// </summary>
    [ProcessBinding(typeof(SpawnClip), PlayMode.EditorPreview)]
    public class EditorSpawnProcess : ProcessBase<SpawnClip>
    {
        private GameObject spawnedInstance;

        public override void OnEnter()
        {
            if (clip.prefab == null) return;

            GetMatrix(out Vector3 pos, out Quaternion rot, out Transform parent);

            // 在编辑器下使用 Instantiate 并在稍后赋予隐藏标识
            // 如果不希望触发 Awake/Start 中的重度游戏逻辑，可以考虑通过 PrefabUtility 去 InstantiatePreview
            // Editor 模式下的 Spawn 主要为了：
            // 1. 挂载碰撞体供后续的 DamageTrack 或者环境预览核对位置
            // 2. 预览它的速度轨迹
            spawnedInstance = Object.Instantiate(clip.prefab, pos, rot);
            spawnedInstance.name = "[Preview] " + clip.prefab.name;
            
            // 重要：标记为 HideAndDontSave 防止被序列化进 Scene 或者被用户误操作
            spawnedInstance.hideFlags = HideFlags.HideAndDontSave;

            if (!clip.detach && parent != null)
            {
                spawnedInstance.transform.SetParent(parent, true);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (spawnedInstance == null) return;
            
            // SpawnProcess 纯粹负责生成。
            // 编辑器预览状态下，投射物的可视化位移（弹道）理论上应由挂载在 Prefab 上的 `ISkillProjectile` 自身在接收
            // Initialize 参数后，通过 ExecuteAlways 或 EditorUpdate 自主完成。
            // 如果仅想可视化静态生成位置和朝向偏移，这一步保持静止即可，后续依靠 `SpawnClipDrawer` 绘制抽象轨线更为符合单一职责。
            
            // 为了维持跟随效果：如果不脱离父物体，确保其基础 Transform 同步
            if (!clip.detach)
            {
                GetMatrix(out Vector3 startPos, out Quaternion rot, out Transform parent);
                // OnEnter 设置了 SetParent 所以大部分情况下位置会自动正确
                // 但如果需要完全绝对同步绑定点，可以这里重新归位 localPosition = zero
                spawnedInstance.transform.localPosition = Vector3.zero;
            }
        }

        public override void OnExit()
        {
            CleanUpInstance();
        }

        public override void Reset()
        {
            base.Reset();
            CleanUpInstance();
        }

        private void CleanUpInstance()
        {
            if (spawnedInstance != null)
            {
                // 在 Editor 模式下用 DestroyImmediate，由于我们标记了 HideAndDontSave，不用担心撤销系统引发问题
                Object.DestroyImmediate(spawnedInstance);
                spawnedInstance = null;
            }
        }

        private void GetMatrix(out Vector3 pos, out Quaternion rot, out Transform parent)
        {
            parent = null;
            if (context != null)
            {
                var actor = context.GetService<ISkillBoneGetter>();
                if (actor != null)
                {
                    parent = actor.GetBone(clip.bindPoint, clip.customBoneName);
                }
            }

            if (parent != null)
            {
                pos = parent.position + parent.rotation * clip.positionOffset;
                rot = parent.rotation * Quaternion.Euler(clip.rotationOffset);
            }
            else
            {
                pos = clip.positionOffset;
                rot = Quaternion.Euler(clip.rotationOffset);
            }
        }
    }
}
