using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    /// <summary>
    /// 编辑器模式下的特效/子物体生成处理
    /// </summary>
    [ProcessBinding(typeof(SpawnClip), PlayMode.EditorPreview)]
    public class EditorSpawnProcess : ProcessBase<SpawnClip>
    {
        private GameObject spawnedInstance;

        public override void OnEnter()
        {
            if (clip.prefab == null) return;

            GetMatrix(out Vector3 pos, out Quaternion rot, out Transform parent);

            spawnedInstance = Object.Instantiate(clip.prefab, pos, rot);
            spawnedInstance.name = "[Preview] " + clip.prefab.name;
            spawnedInstance.hideFlags = HideFlags.HideAndDontSave;

            bool isFollow = clip.bindConfig != null && clip.bindConfig.followTarget;
            if (isFollow && parent != null)
            {
                spawnedInstance.transform.position = pos;
                spawnedInstance.transform.rotation = rot;
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (spawnedInstance == null) return;
            
            bool isFollow = clip.bindConfig != null && clip.bindConfig.followTarget;
            if (isFollow)
            {
                GetMatrix(out Vector3 startPos, out Quaternion rot, out Transform parent);
                spawnedInstance.transform.position = startPos;
                spawnedInstance.transform.rotation = rot;
            }
        }

        public override void OnExit()
        {
            CleanUpInstance();
        }

        public override void OnStop()
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
                Object.DestroyImmediate(spawnedInstance);
                spawnedInstance = null;
            }
        }

        private void GetMatrix(out Vector3 pos, out Quaternion rot, out Transform parent)
        {
            parent = null;
            var bindConfig = clip.bindConfig ?? new TransformBindConfig();

            if (context != null && context.Owner != null)
            {
                var boneGetter = new ATBoneGetter(context.Owner);
                parent = boneGetter.GetBone(bindConfig.bindPoint, bindConfig.customBoneName);
            }

            if (parent != null)
            {
                pos = parent.position + parent.rotation * bindConfig.positionOffset;
                rot = parent.rotation * Quaternion.Euler(bindConfig.rotationOffset);
            }
            else
            {
                pos = bindConfig.positionOffset;
                rot = Quaternion.Euler(bindConfig.rotationOffset);
            }
        }
    }
}
