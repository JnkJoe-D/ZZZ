using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillEditor
{
    [Serializable]
    [ClipDefinition(typeof(CameraTrack), "相机控制")]
    public class CameraControlClip : ClipBase
    {
        [Header("资源")]
        [SkillProperty("相机预制体")]
        public GameObject cameraPrefab;

        [SkillAssetReference("cameraPrefab")]
        public SkillAssetReference cameraRef = new SkillAssetReference();

        [SkillProperty("Timeline资源")]
        public PlayableAsset timelineAsset;

        [SkillAssetReference("timelineAsset")]
        public SkillAssetReference timelineRef = new SkillAssetReference();

        [SkillProperty("跟拍骨骼名")]
        public string followBoneName;

        [SkillProperty("看向骨骼名")]
        public string lookAtBoneName;

        [Header("摄像机设置覆盖")]
        [SkillProperty("启用设置覆盖")]
        public bool overrideSettings = false;

        [SkillProperty("背景颜色")]
        [ShowIf("overrideSettings", true)]
        public Color backgroundColor = Color.black;

        [SkillProperty("渲染层级")]
        [ShowIf("overrideSettings", true)]
        public LayerMask cullingMask = -1;

        public CameraControlClip()
        {
            clipName = "Camera Control Clip";
            duration = 2.0f;
        }

        public override ClipBase Clone()
        {
            return new CameraControlClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                cameraPrefab = this.cameraPrefab,
                cameraRef = new SkillAssetReference(this.cameraRef.guid, this.cameraRef.assetName, this.cameraRef.assetPath),
                timelineAsset = this.timelineAsset,
                timelineRef = new SkillAssetReference(this.timelineRef.guid, this.timelineRef.assetName, this.timelineRef.assetPath),
                followBoneName = this.followBoneName,
                lookAtBoneName = this.lookAtBoneName,
                overrideSettings = this.overrideSettings,
                backgroundColor = this.backgroundColor,
                cullingMask = this.cullingMask
            };
        }
    }
}
