using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace Game.Editor.Tools
{
    public class SkeletonPoseCopier : EditorWindow
    {
        private GameObject targetObject; // 异常的对象根骨骼
        private GameObject sourceObject; // 正常的源对象根骨骼

        [MenuItem("Tools/Skeleton Pose Copier (Fix T-Pose)")]
        public static void ShowWindow()
        {
            GetWindow<SkeletonPoseCopier>("Pose Copier");
        }

        void OnGUI()
        {
            GUILayout.Label("角色骨骼姿态修复工具", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("操作步骤：\n1. 将项目窗口中原始的 Prefab/Model 拖入场景（作为正常的源）。\n2. 将异常的角色根骨骼变换拖入 Target。\n3. 将新创建的正常角色根骨骼变换拖入 Source。\n4. 点击复制。", MessageType.Info);
            GUILayout.Space(10);

            targetObject = (GameObject)EditorGUILayout.ObjectField("1. 坏掉的角色 (Target)", targetObject, typeof(GameObject), true);
            sourceObject = (GameObject)EditorGUILayout.ObjectField("2. 正常的源 (Source)", sourceObject, typeof(GameObject), true);

            GUILayout.Space(20);

            if (GUILayout.Button("执行姿态克隆 (Copy Pose)"))
            {
                if (targetObject != null && sourceObject != null)
                {
                    CopyPose(sourceObject.transform, targetObject.transform);
                    // 修复后强制刷新 Animator
                    Animator anim = targetObject.GetComponentInParent<Animator>();
                    if (anim) { anim.Rebind(); anim.Update(0f); }
                    Debug.Log($"<color=green>修复成功！已将 {sourceObject.name} 的姿态应用到 {targetObject.name}</color>");
                }
            }
        }

        private void CopyPose(Transform source, Transform target)
        {
            Undo.RecordObjects(target.GetComponentsInChildren<Transform>(true), "Fix T-Pose");
            Dictionary<string, Transform> sourceMap = new Dictionary<string, Transform>();
            MapBones(source, sourceMap);
            ApplyBones(target, sourceMap);
        }

        private void MapBones(Transform t, Dictionary<string, Transform> map)
        {
            if (!map.ContainsKey(t.name)) map.Add(t.name, t);
            foreach (Transform child in t) MapBones(child, map);
        }

        private void ApplyBones(Transform target, Dictionary<string, Transform> sourceMap)
        {
            if (sourceMap.TryGetValue(target.name, out Transform source))
            {
                target.localPosition = source.localPosition;
                target.localRotation = source.localRotation;
                target.localScale = source.localScale;
            }
            foreach (Transform child in target) ApplyBones(child, sourceMap);
        }
    }
}
