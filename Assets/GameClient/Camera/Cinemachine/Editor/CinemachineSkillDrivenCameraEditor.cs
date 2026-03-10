using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSkillDrivenCamera))]
    internal sealed class CinemachineSkillDrivenCameraEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineSkillDrivenCamera>
    {
        EmbeddedAssetEditorCompat<CinemachineBlenderSettings> _blendsEditor;
        ReorderableList _childList;
        ReorderableList _instructionList;
        string[] _cameraCandidates;
        Dictionary<CinemachineVirtualCameraBase, int> _cameraIndexLookup;

        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_CustomBlends));
            excluded.Add(FieldPath(x => x.m_Instructions));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _blendsEditor = new EmbeddedAssetEditorCompat<CinemachineBlenderSettings>(
                FieldPath(x => x.m_CustomBlends), this);
            _blendsEditor.OnChanged = _ => InspectorUtility.RepaintGameView();
            _childList = null;
            _instructionList = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _blendsEditor?.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (_instructionList == null)
                SetupInstructionList();
            if (_childList == null)
                SetupChildList();

            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            EditorGUILayout.HelpBox(
                "Leave State empty for the default mapping. Use dotted names such as Skill.Attack.Heavy.Startup to enable parent fallback.",
                MessageType.Info);
            DrawRemainingPropertiesInInspector();

            _blendsEditor.DrawEditorCombo(
                "Create New Blender Asset",
                Target.gameObject.name + " Blends",
                "asset",
                string.Empty,
                "Custom Blends",
                false);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            _instructionList.DoLayoutList();
            EditorGUILayout.Space();
            _childList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Target.ValidateInstructions();
            }

            DrawExtensionsWidgetInInspector();
        }

        void UpdateCameraCandidates()
        {
            var vcams = new List<string>();
            _cameraIndexLookup = new Dictionary<CinemachineVirtualCameraBase, int>();
            vcams.Add("(none)");

            var children = Target.ChildCameras;
            foreach (var child in children)
            {
                _cameraIndexLookup[child] = vcams.Count;
                vcams.Add(child.Name);
            }

            _cameraCandidates = vcams.ToArray();
        }

        int GetCameraIndex(Object obj)
        {
            if (obj == null || _cameraIndexLookup == null)
                return 0;

            var vcam = obj as CinemachineVirtualCameraBase;
            if (vcam == null || !_cameraIndexLookup.ContainsKey(vcam))
                return 0;

            return _cameraIndexLookup[vcam];
        }

        void SetupInstructionList()
        {
            _instructionList = new ReorderableList(
                serializedObject,
                serializedObject.FindProperty(() => Target.m_Instructions),
                true, true, true, true);

            var instruction = new CinemachineSkillDrivenCamera.Instruction();
            const float vSpace = 2f;
            const float hSpace = 3f;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;
            float hBigSpace = EditorGUIUtility.singleLineHeight * 2f / 3f;

            _instructionList.drawHeaderCallback = rect =>
            {
                float sharedWidth = rect.width - EditorGUIUtility.singleLineHeight
                    - 2f * (hBigSpace + floatFieldWidth) - hSpace;
                rect.x += EditorGUIUtility.singleLineHeight;
                rect.width = sharedWidth / 2f;
                EditorGUI.LabelField(rect, "State");

                rect.x += rect.width + hSpace;
                EditorGUI.LabelField(rect, "Camera");

                rect.x += rect.width + hBigSpace;
                rect.width = floatFieldWidth;
                EditorGUI.LabelField(rect, "Wait");

                rect.x += rect.width + hBigSpace;
                EditorGUI.LabelField(rect, "Min");
            };

            _instructionList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                UpdateCameraCandidates();

                SerializedProperty instProp = _instructionList.serializedProperty.GetArrayElementAtIndex(index);
                float sharedWidth = rect.width - 2f * (hBigSpace + floatFieldWidth) - hSpace;
                rect.y += vSpace;
                rect.height = EditorGUIUtility.singleLineHeight;

                rect.width = sharedWidth / 2f;
                SerializedProperty stateProp = instProp.FindPropertyRelative(() => instruction.m_State);
                stateProp.stringValue = EditorGUI.DelayedTextField(rect, stateProp.stringValue);

                rect.x += rect.width + hSpace;
                SerializedProperty vcamProp = instProp.FindPropertyRelative(() => instruction.m_VirtualCamera);
                int currentVcam = GetCameraIndex(vcamProp.objectReferenceValue);
                int vcamSelection = EditorGUI.Popup(rect, currentVcam, _cameraCandidates);
                if (currentVcam != vcamSelection)
                    vcamProp.objectReferenceValue = vcamSelection == 0 ? null : Target.ChildCameras[vcamSelection - 1];

                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = hBigSpace;

                rect.x += rect.width;
                rect.width = floatFieldWidth + hBigSpace;
                EditorGUI.PropertyField(
                    rect,
                    instProp.FindPropertyRelative(() => instruction.m_ActivateAfter),
                    new GUIContent(" "));

                rect.x += rect.width;
                EditorGUI.PropertyField(
                    rect,
                    instProp.FindPropertyRelative(() => instruction.m_MinDuration),
                    new GUIContent(" "));

                EditorGUIUtility.labelWidth = oldLabelWidth;
            };

            _instructionList.onAddCallback = list =>
            {
                ++list.serializedProperty.arraySize;
                serializedObject.ApplyModifiedProperties();
                Target.ValidateInstructions();
            };
        }

        void SetupChildList()
        {
            const float vSpace = 2f;
            const float hSpace = 3f;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;
            float hBigSpace = EditorGUIUtility.singleLineHeight * 2f / 3f;

            _childList = new ReorderableList(
                serializedObject,
                serializedObject.FindProperty("m_ChildCameras"),
                true, true, true, true);

            _childList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Virtual Camera Children");
                GUIContent priorityText = new GUIContent("Priority");
                var textDimensions = GUI.skin.label.CalcSize(priorityText);
                rect.x += rect.width - textDimensions.x;
                rect.width = textDimensions.x;
                EditorGUI.LabelField(rect, priorityText);
            };

            _childList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.y += vSpace;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= floatFieldWidth + hBigSpace;

                SerializedProperty element = _childList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, GUIContent.none);

                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = hBigSpace;
                var childObject = element.objectReferenceValue;
                if (childObject != null)
                {
                    var childSerializedObject = new SerializedObject(childObject);
                    rect.x += rect.width + hSpace;
                    rect.width = floatFieldWidth + hBigSpace;
                    SerializedProperty priorityProp = childSerializedObject.FindProperty(() => Target.m_Priority);
                    EditorGUI.PropertyField(rect, priorityProp, new GUIContent(" "));
                    childSerializedObject.ApplyModifiedProperties();
                }
                EditorGUIUtility.labelWidth = oldLabelWidth;
            };

            _childList.onChangedCallback = list =>
            {
                if (list.index < 0 || list.index >= list.serializedProperty.arraySize)
                    return;

                Object o = list.serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue;
                if (o is CinemachineVirtualCameraBase vcam)
                    vcam.transform.SetSiblingIndex(list.index);
            };

            _childList.onAddCallback = list =>
            {
                int index = list.serializedProperty.arraySize;
                var vcam = CreateDefaultVirtualCamera(parentObject: Target.gameObject);
                Undo.SetTransformParent(vcam.transform, Target.transform, string.Empty);
                vcam.transform.SetSiblingIndex(index);
                Target.ValidateInstructions();
            };

            _childList.onRemoveCallback = list =>
            {
                Object o = list.serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue;
                if (o is CinemachineVirtualCameraBase vcam)
                    Undo.DestroyObjectImmediate(vcam.gameObject);
            };
        }

        static LensSettings MatchSceneViewCamera(Transform sceneObject)
        {
            var lens = LensSettings.Default;
            var brain = GetOrCreateBrain();
            if (brain != null && brain.OutputCamera != null)
                lens = LensSettings.FromCamera(brain.OutputCamera);

            if (SceneView.lastActiveSceneView != null)
            {
                var src = SceneView.lastActiveSceneView.camera;
                sceneObject.SetPositionAndRotation(src.transform.position, src.transform.rotation);
                if (lens.Orthographic == src.orthographic)
                {
                    if (src.orthographic)
                        lens.OrthographicSize = src.orthographicSize;
                    else
                        lens.FieldOfView = src.fieldOfView;
                }
            }

            return lens;
        }

        static CinemachineVirtualCamera CreateDefaultVirtualCamera(
            string name = "Virtual Camera", GameObject parentObject = null)
        {
            var vcam = CreateCinemachineObject<CinemachineVirtualCamera>(name, parentObject);
            vcam.m_Lens = MatchSceneViewCamera(vcam.transform);
            AddCinemachineComponent<CinemachineComposer>(vcam);
            AddCinemachineComponent<CinemachineTransposer>(vcam);
            return vcam;
        }

        static T CreateCinemachineObject<T>(string name, GameObject parentObject) where T : Component
        {
            GetOrCreateBrain();
            var go = ObjectFactory.CreateGameObject(name);
            T component = go.AddComponent<T>();

            if (parentObject != null)
                Undo.SetTransformParent(go.transform, parentObject.transform, "Set parent of " + name);

            GameObjectUtility.EnsureUniqueNameForSibling(go);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;

            Selection.activeGameObject = go;
            return component;
        }

        static CinemachineBrain GetOrCreateBrain()
        {
            if (CinemachineCore.Instance.BrainCount > 0)
                return CinemachineCore.Instance.GetActiveBrain(0);

            var cam = Camera.main;
            if (cam == null)
            {
#if UNITY_2023_1_OR_NEWER
                cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
#else
                cam = Object.FindObjectOfType<Camera>();
#endif
            }

            if (cam != null)
                return Undo.AddComponent<CinemachineBrain>(cam.gameObject);

            return ObjectFactory.CreateGameObject("CinemachineBrain").AddComponent<CinemachineBrain>();
        }

        static T AddCinemachineComponent<T>(CinemachineVirtualCamera vcam) where T : CinemachineComponentBase
        {
            var componentOwner = vcam.GetComponentOwner().gameObject;
            if (componentOwner == null)
                return null;

            var component = Undo.AddComponent<T>(componentOwner);
            vcam.InvalidateComponentPipeline();
            return component;
        }
    }

    internal sealed class EmbeddedAssetEditorCompat<T> where T : ScriptableObject
    {
        readonly string _propertyName;
        readonly UnityEditor.Editor _owner;
        UnityEditor.Editor _editor;

        public System.Action<T> OnChanged;

        public EmbeddedAssetEditorCompat(string propertyName, UnityEditor.Editor owner)
        {
            _propertyName = propertyName;
            _owner = owner;
        }

        public void OnDisable()
        {
            DestroyEditor();
        }

        public void DrawEditorCombo(
            string title,
            string defaultName,
            string extension,
            string message,
            string showLabel,
            bool indent)
        {
            SerializedProperty property = _owner.serializedObject.FindProperty(_propertyName);
            UpdateEditor(property);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, new GUIContent(showLabel));
            if (EditorGUI.EndChangeCheck())
            {
                _owner.serializedObject.ApplyModifiedProperties();
                UpdateEditor(property);
            }

            if (_editor == null)
            {
                if (GUILayout.Button("Create Asset"))
                {
                    string newAssetPath = EditorUtility.SaveFilePanelInProject(
                        title, defaultName, extension, message);
                    if (!string.IsNullOrEmpty(newAssetPath))
                    {
                        T asset = ScriptableObjectUtility.CreateAt<T>(newAssetPath);
                        property.objectReferenceValue = asset;
                        _owner.serializedObject.ApplyModifiedProperties();
                        UpdateEditor(property);
                    }
                }
            }
            else
            {
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, "Edit Asset", true);
                if (property.isExpanded)
                {
                    EditorGUI.BeginChangeCheck();
                    if (indent)
                        ++EditorGUI.indentLevel;
                    _editor.OnInspectorGUI();
                    if (indent)
                        --EditorGUI.indentLevel;
                    if (EditorGUI.EndChangeCheck())
                        OnChanged?.Invoke(property.objectReferenceValue as T);
                }
            }

            EditorGUILayout.EndVertical();
        }

        void UpdateEditor(SerializedProperty property)
        {
            Object target = property.objectReferenceValue;
            if (_editor != null && _editor.target != target)
                DestroyEditor();

            if (_editor == null && target != null)
                _editor = UnityEditor.Editor.CreateEditor(target);
        }

        void DestroyEditor()
        {
            if (_editor == null)
                return;

            Object.DestroyImmediate(_editor);
            _editor = null;
        }
    }
}
