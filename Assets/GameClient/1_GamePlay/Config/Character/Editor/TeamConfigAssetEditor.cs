#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.GamePlay
{
    [CustomEditor(typeof(TeamConfigAsset))]
    public class TeamConfigAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _membersProp;
        private SerializedProperty _initialSlotIndexProp;

        private void OnEnable()
        {
            _membersProp = serializedObject.FindProperty("Members");
            _initialSlotIndexProp = serializedObject.FindProperty("InitialSlotIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            TeamConfigAsset asset = (TeamConfigAsset)target;

            EditorGUILayout.LabelField("队伍固定卡槽配置 (Fixed 3 Slots)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "队伍固定为 3 个角色卡槽。任意槽位留空时，运行时均会自动靠左对齐。\n" +
                "例如只配了 3 号位，运行时其实际即为 1 号位；只配了 1、3 号位，运行时为 1、2 号位。", 
                MessageType.Info);

            // 强制保证 SerializedProperty 内部数组长度为 3
            if (_membersProp.arraySize != TeamConfigAsset.MaxTeamCapacity)
            {
                _membersProp.arraySize = TeamConfigAsset.MaxTeamCapacity;
            }

            EditorGUI.indentLevel++;
            string[] slotLabels = { "1号位 (Slot 1 - 首发主控)", "2号位 (Slot 2 - 备用顺位)", "3号位 (Slot 3 - 备用顺位)" };
            for (int i = 0; i < TeamConfigAsset.MaxTeamCapacity; i++)
            {
                SerializedProperty elementProp = _membersProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(elementProp, new GUIContent(slotLabels[i]));
            }
            EditorGUI.indentLevel--;

            // 运行时自动排序映射预览
            CharacterConfigAsset[] ordered = asset.GetOrderedMembers();
            int validCount = asset.GetValidMemberCount();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("运行时实际槽位映射预览 (Auto Left-Aligned Preview):", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < TeamConfigAsset.MaxTeamCapacity; i++)
                {
                    string roleDesc = ordered[i] != null ? ordered[i].name : "<未配置 (空卡槽)>";
                    string status = i < validCount ? "【有效上阵】" : "【已隐藏】";
                    EditorGUILayout.LabelField($"运行时 {i + 1} 号位: {roleDesc}  {status}");
                }
            }

            if (GUILayout.Button("在资产中物理靠左对齐 (Sort To Left)", GUILayout.Height(24)))
            {
                Undo.RecordObject(asset, "Sort Team Members Left");
                asset.SortMembersLeft();
                EditorUtility.SetDirty(asset);
                serializedObject.Update();
            }

            EditorGUILayout.Space(8);

            // 绘制其它属性（排除 Members 和 m_Script）
            DrawPropertiesExcluding(serializedObject, "Members", "m_Script");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
