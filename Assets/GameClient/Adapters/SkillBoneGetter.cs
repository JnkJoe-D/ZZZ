using System;
using System.Collections.Generic;
using ATEditor;
using UnityEngine;

namespace Game.Adapters
{
    public class SkillBoneGetter : ISkillBoneGetter
    {
        private readonly GameObject owner;
        private readonly Dictionary<string, Transform> _boneCache = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<BindPoint, string> BindPointBoneNames = new Dictionary<BindPoint, string>
        {
            { BindPoint.Bip001, "Bip001" },
            { BindPoint.Bip001_Pelvis, "Bip001 Pelvis" },
            { BindPoint.Bip001_Spine, "Bip001 Spine" },
            { BindPoint.Bip001_Spine1, "Bip001 Spine1" },
            { BindPoint.Bip001_Spine2, "Bip001 Spine2" },
            { BindPoint.Bip001_Neck, "Bip001 Neck" },
            { BindPoint.Bip001_Head, "Bip001 Head" },
            { BindPoint.Bip001_L_Clavicle, "Bip001 L Clavicle" },
            { BindPoint.Bip001_L_UpperArm, "Bip001 L UpperArm" },
            { BindPoint.Bip001_L_Forearm, "Bip001 L Forearm" },
            { BindPoint.Bip001_L_Hand, "Bip001 L Hand" },
            { BindPoint.Bip001_R_Clavicle, "Bip001 R Clavicle" },
            { BindPoint.Bip001_R_UpperArm, "Bip001 R UpperArm" },
            { BindPoint.Bip001_R_Forearm, "Bip001 R Forearm" },
            { BindPoint.Bip001_R_Hand, "Bip001 R Hand" },
            { BindPoint.Bip001_L_Thigh, "Bip001 L Thigh" },
            { BindPoint.Bip001_L_Calf, "Bip001 L Calf" },
            { BindPoint.Bip001_L_Foot, "Bip001 L Foot" },
            { BindPoint.Bip001_L_Toe0, "Bip001 L Toe0" },
            { BindPoint.Bip001_R_Thigh, "Bip001 R Thigh" },
            { BindPoint.Bip001_R_Calf, "Bip001 R Calf" },
            { BindPoint.Bip001_R_Foot, "Bip001 R Foot" },
            { BindPoint.Bip001_R_Toe0, "Bip001 R Toe0" },
            { BindPoint.Bip001_Prop1, "Bip001 Prop1" }
        };

        public SkillBoneGetter(GameObject owner)
        {
            this.owner = owner;
        }

        public Transform GetBone(BindPoint point, string customName = "")
        {
            if (owner == null) return null;

            if (point == BindPoint.LogicRoot)
            {
                return owner.transform;
            }

            if (point == BindPoint.CustomBone)
            {
                if (string.IsNullOrEmpty(customName)) return owner.transform;
                return GetOrFindBone(customName) ?? owner.transform;
            }

            if (BindPointBoneNames.TryGetValue(point, out string boneName))
            {
                return GetOrFindBone(boneName) ?? owner.transform;
            }

            return owner.transform;
        }

        private Transform GetOrFindBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName) || owner == null) return null;

            if (_boneCache.TryGetValue(boneName, out Transform cached) && cached != null)
            {
                return cached;
            }

            Transform found = FindChildRecursive(owner.transform, boneName);
            if (found != null)
            {
                _boneCache[boneName] = found;
            }
            return found;
        }

        public static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name)) return null;
            if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildRecursive(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
