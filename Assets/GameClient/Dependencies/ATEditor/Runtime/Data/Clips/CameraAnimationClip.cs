using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ATEditor
{
    /// <summary>
    /// 动画相机片段：用于播放相机预制体、PlayableDirector/Timeline 运镜动画与专属特写
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(CameraTrack), "动画相机")]
    public class CameraAnimationClip : ClipBase
    {
        [Header("资源")]
        [ActionProperty("相机预制体")]
        public GameObject cameraPrefab;

        [ActionAssetReference("cameraPrefab")]
        public ActionAssetReference cameraRef = new ActionAssetReference();

        [ActionProperty("Timeline资源")]
        public PlayableAsset timelineAsset;

        [ActionAssetReference("timelineAsset")]
        public ActionAssetReference timelineRef = new ActionAssetReference();

        [ActionProperty("跟拍骨骼名")]
        public string followBoneName;

        [ActionProperty("看向骨骼名")]
        public string lookAtBoneName;

        [Header("摄像机设置覆盖")]
        [ActionProperty("启用设置覆盖")]
        public bool overrideSettings = false;

        [ActionProperty("背景颜色")]
        [ATShowIf("overrideSettings", true)]
        public Color backgroundColor = Color.black;

        [ActionProperty("渲染层级")]
        [ATShowIf("overrideSettings", true)]
        public LayerMask cullingMask = -1;

        public CameraAnimationClip()
        {
            clipName = "Camera Animation Clip";
            duration = 2.0f;
        }

        public override ClipBase Clone()
        {
            return new CameraAnimationClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                cameraPrefab = this.cameraPrefab,
                cameraRef = new ActionAssetReference(this.cameraRef.guid, this.cameraRef.assetName, this.cameraRef.assetPath),
                timelineAsset = this.timelineAsset,
                timelineRef = new ActionAssetReference(this.timelineRef.guid, this.timelineRef.assetName, this.timelineRef.assetPath),
                followBoneName = this.followBoneName,
                lookAtBoneName = this.lookAtBoneName,
                overrideSettings = this.overrideSettings,
                backgroundColor = this.backgroundColor,
                cullingMask = this.cullingMask
            };
        }
    }
}
