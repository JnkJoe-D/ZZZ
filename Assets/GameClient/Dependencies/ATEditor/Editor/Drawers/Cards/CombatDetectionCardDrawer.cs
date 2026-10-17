using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 攻击检测卡片所持有的数据引用
    /// </summary>
    public class CombatDetectionCardData
    {
        public HitBoxScopeConfig HitBoxScope;
        public AttackDetectionPolicy AttackPolicy;
        public Action<bool> OnEnabledChangedCallback;

        public CombatDetectionCardData(HitBoxScopeConfig hitBoxScope, AttackDetectionPolicy attackPolicy, Action<bool> onEnabledChanged = null)
        {
            HitBoxScope = hitBoxScope;
            AttackPolicy = attackPolicy;
            OnEnabledChangedCallback = onEnabledChanged;
        }
    }

    /// <summary>
    /// 攻击检测通用卡片抽屉
    /// 支持在 HitClip (必要卡片) 与 SpawnClip (可选卡片) 中高度复用
    /// 集中管理参数 Inspector 与 SceneView 碰撞几何体的渲染
    /// </summary>
    public class CombatDetectionCardDrawer : CardDrawerBase<CombatDetectionCardData>
    {
        private readonly bool _isMandatory;
        public override bool IsMandatory => _isMandatory;
        public override string CardTitle => "攻击检测设置";

        private static readonly Dictionary<string, bool?> _boneValidationCache = new Dictionary<string, bool?>();

        public CombatDetectionCardDrawer(bool isMandatory = true)
        {
            _isMandatory = isMandatory;
        }

        protected override void OnEnabledChanged(bool isEnabled, CardDrawContext context)
        {
            Data?.OnEnabledChangedCallback?.Invoke(isEnabled);
        }

        protected override void OnDrawBody(CardDrawContext context)
        {
            if (Data.HitBoxScope != null)
            {
                EditorGUILayout.LabelField("── 检测盒范围 ──", EditorStyles.boldLabel);
                DrawSubField(Data.HitBoxScope, "shape");
                DrawSubField(Data.HitBoxScope, "bindPoint");

                if (Data.HitBoxScope.bindPoint == BindPoint.CustomBone)
                {
                    DrawCustomBoneField(Data.HitBoxScope, context);
                }

                DrawSubField(Data.HitBoxScope, "hitBoxFollowMode");
                DrawSubField(Data.HitBoxScope, "positionOffset");
                DrawSubField(Data.HitBoxScope, "rotationOffset");
                DrawSubField(Data.HitBoxScope, "showHitBoxGizmos");
            }

            if (Data.AttackPolicy != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("── 判定策略与效果 ──", EditorStyles.boldLabel);
                DrawSubField(Data.AttackPolicy, "detectFrequency");

                if (Data.AttackPolicy.detectFrequency == Frequency.Times)
                {
                    DrawSubField(Data.AttackPolicy, "times");
                }

                DrawSubField(Data.AttackPolicy, "maxHitTargets");
                if (Data.AttackPolicy.maxHitTargets > 0)
                {
                    DrawSubField(Data.AttackPolicy, "targetSortMode");
                }

                EditorGUILayout.Space(2);
                DrawSubField(Data.AttackPolicy, "hitDirectionMode");
                if (Data.AttackPolicy.hitDirectionMode == HitDirectionMode.OnEnterCustomRelative)
                {
                    DrawSubField(Data.AttackPolicy, "customHitDirection");
                }

                EditorGUILayout.Space(2);
                DrawSubField(Data.AttackPolicy, "hitLayerMask");
                DrawSubField(Data.AttackPolicy, "isSelfImpacted");
                EditorGUILayout.Space(4);
                DrawSubField(Data.AttackPolicy, "detects");
            }
        }

        protected override void OnDrawSceneGUI(CardDrawContext context)
        {
            if (Data.HitBoxScope == null || !Data.HitBoxScope.showHitBoxGizmos) return;

            // 1. 基于宿主 OriginPose 计算世界坐标位姿
            Vector3 worldPos = context.OriginPose.position;
            Quaternion worldRot = context.OriginPose.rotation;

            if (context.NeedsApplyCardOffset)
            {
                worldPos += context.OriginPose.rotation * Data.HitBoxScope.positionOffset;
                worldRot *= Quaternion.Euler(Data.HitBoxScope.rotationOffset);
            }

            // 2. 判断是否在当前时间指针有效区间内
            bool isActive = false;
            if (context.State != null && !context.State.isStopped && context.Clip != null)
            {
                isActive = context.State.timeIndicator >= context.Clip.StartTime &&
                           context.State.timeIndicator <= context.Clip.StartTime + context.Clip.Duration;
            }

            // 3. 统一使用 HitBoxHandlesRenderer 绘制全形状 Gizmos
            HitBoxHandlesRenderer.DrawGizmos(Data.HitBoxScope.shape, worldPos, worldRot, isActive);
        }

        private void DrawCustomBoneField(HitBoxScopeConfig hitBoxScope, CardDrawContext context)
        {
            EditorGUILayout.BeginHorizontal();

            string cacheKey = context.Clip != null && !string.IsNullOrEmpty(context.Clip.clipId) ? context.Clip.clipId : hitBoxScope.GetHashCode().ToString();
            _boneValidationCache.TryGetValue(cacheKey, out bool? isValid);

            Color oldBgColor = GUI.backgroundColor;
            if (isValid.HasValue)
            {
                GUI.backgroundColor = isValid.Value 
                    ? new Color(0.6f, 1f, 0.6f, 1f)   // 有效：绿色
                    : new Color(1f, 0.55f, 0.55f, 1f); // 无效：红色
            }

            string newName = EditorGUILayout.TextField("自定义骨骼名称", hitBoxScope.customBoneName);
            if (newName != hitBoxScope.customBoneName)
            {
                hitBoxScope.customBoneName = newName;
                _boneValidationCache.Remove(cacheKey);
            }

            GUI.backgroundColor = oldBgColor;

            if (GUILayout.Button(new GUIContent("检测", "检测当前预览角色层级中是否存在该自定义骨骼"), GUILayout.Width(50), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                bool found = CheckBoneExists(hitBoxScope.customBoneName, context, out string msg);
                _boneValidationCache[cacheKey] = found;
                if (found)
                {
                    ATLog.Info($"<color=green>[攻击检测卡片 骨骼检测通过]</color> {msg}");
                }
                else
                {
                    ATLog.Warning($"<color=red>[攻击检测卡片 骨骼检测失败]</color> {msg}");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static bool CheckBoneExists(string boneName, CardDrawContext context, out string message)
        {
            if (string.IsNullOrWhiteSpace(boneName))
            {
                message = "骨骼名称为空！";
                return false;
            }

            GameObject previewTarget = context?.PreviewTarget;
            if (previewTarget == null && context?.State != null)
            {
                if (context.State.PreviewContext != null && context.State.PreviewContext.Owner != null)
                {
                    previewTarget = context.State.PreviewContext.Owner;
                }
                else if (context.State.previewTarget != null)
                {
                    previewTarget = context.State.previewTarget;
                }
            }

            if (previewTarget == null)
            {
                message = "未找到预览角色，请先在时间轴编辑器指定或激活预览角色！";
                return false;
            }

            Transform found = FindBoneRecursive(previewTarget.transform, boneName.Trim());
            if (found != null)
            {
                message = $"在角色 [{previewTarget.name}] 中成功找到骨骼: {found.name}";
                return true;
            }

            message = $"在角色 [{previewTarget.name}] 层级中未找到名为 '{boneName}' 的骨骼！";
            return false;
        }

        private static Transform FindBoneRecursive(Transform parent, string boneName)
        {
            if (parent == null || string.IsNullOrEmpty(boneName)) return null;
            if (parent.name.Equals(boneName, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindBoneRecursive(parent.GetChild(i), boneName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
