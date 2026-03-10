using Cinemachine.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// A StateDrivenCamera variant that is driven by external skill state changes
    /// instead of an Animator state machine.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [ExcludeFromPreset]
    [AddComponentMenu("Cinemachine/CinemachineSkillDrivenCamera")]
    public class CinemachineSkillDrivenCamera : CinemachineVirtualCameraBase
    {
        [Tooltip("Default object for the camera children to look at (the aim target), "
            + "if not specified in a child camera.  May be empty if all of the children "
            + "define targets of their own.")]
        [NoSaveDuringPlay]
        [VcamTargetProperty]
        public Transform m_LookAt = null;

        [Tooltip("Default object for the camera children wants to move with (the body target), "
            + "if not specified in a child camera.  May be empty if all of the children "
            + "define targets of their own.")]
        [NoSaveDuringPlay]
        [VcamTargetProperty]
        public Transform m_Follow = null;

        [Tooltip("When enabled, the current child camera and blend will be indicated in the game window, for debugging")]
        public bool m_ShowDebugText = false;

        [SerializeField]
        [HideInInspector]
        [NoSaveDuringPlay]
        internal CinemachineVirtualCameraBase[] m_ChildCameras = null;

        [Serializable]
        public struct Instruction
        {
            [Tooltip("The skill state key. Leave empty for the default camera mapping.")]
            public string m_State;

            [HideInInspector]
            public int m_FullHash;

            [Tooltip("The virtual camera to activate when the skill state becomes active")]
            public CinemachineVirtualCameraBase m_VirtualCamera;

            [Tooltip("How long to wait (in seconds) before activating the virtual camera. This filters out very short state durations")]
            public float m_ActivateAfter;

            [Tooltip("The minimum length of time (in seconds) to keep a virtual camera active")]
            public float m_MinDuration;
        }

        [Tooltip("The set of instructions associating skill states with virtual cameras. These instructions are used to choose the live child at any given moment")]
        public Instruction[] m_Instructions;

        [CinemachineBlendDefinitionProperty]
        [Tooltip("The blend which is used if you don't explicitly define a blend between two Virtual Camera children")]
        public CinemachineBlendDefinition m_DefaultBlend
            = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.5f);

        [Tooltip("This is the asset which contains custom settings for specific child blends")]
        public CinemachineBlenderSettings m_CustomBlends = null;

        [Serializable]
        internal struct ParentHash
        {
            public int m_Hash;
            public int m_ParentHash;

            public ParentHash(int hash, int parentHash)
            {
                m_Hash = hash;
                m_ParentHash = parentHash;
            }
        }

        [HideInInspector]
        [SerializeField]
        internal ParentHash[] m_ParentHash = null;

        struct RuntimeStateRequest
        {
            public int Token;
            public int Priority;
            public int Sequence;
            public string State;
            public int Hash;
        }

        readonly List<RuntimeStateRequest> m_ActiveStateRequests = new List<RuntimeStateRequest>();
        int m_NextStateToken = 1;
        int m_RequestSequence = 0;
        bool m_HasManualState = false;
        string m_ManualState = string.Empty;
        int m_ManualStateHash = 0;

        public override string Description
        {
            get
            {
                if (mActiveBlend != null)
                    return mActiveBlend.Description;

                ICinemachineCamera vcam = LiveChild;
                if (vcam == null)
                    return "(none)";

                var sb = CinemachineDebug.SBFromPool();
                sb.Append("[");
                sb.Append(vcam.Name);
                sb.Append("]");
                string text = sb.ToString();
                CinemachineDebug.ReturnToPool(sb);
                return text;
            }
        }

        public ICinemachineCamera LiveChild { get; private set; }

        public override bool IsLiveChild(ICinemachineCamera vcam, bool dominantChildOnly = false)
        {
            return vcam == LiveChild || (mActiveBlend != null && mActiveBlend.Uses(vcam));
        }

        public override CameraState State => m_State;

        public override Transform LookAt
        {
            get => ResolveLookAt(m_LookAt);
            set => m_LookAt = value;
        }

        public override Transform Follow
        {
            get => ResolveFollow(m_Follow);
            set => m_Follow = value;
        }

        public CinemachineVirtualCameraBase[] ChildCameras
        {
            get
            {
                UpdateListOfChildren();
                return m_ChildCameras;
            }
        }

        public bool IsBlending => mActiveBlend != null;

        public CinemachineBlend ActiveBlend => mActiveBlend;

        public string CurrentState
        {
            get
            {
                if (TryGetEffectiveState(out var stateHash, out var stateName))
                    return stateName;
                if (mInstructionDictionary != null && mInstructionDictionary.ContainsKey(0))
                    return string.Empty;
                return string.Empty;
            }
        }

        public static int StateNameToHash(string stateName)
        {
            string normalized = NormalizeStateName(stateName);
            return string.IsNullOrEmpty(normalized) ? 0 : Animator.StringToHash(normalized);
        }

        public int AcquireState(string stateName, int priority = 0)
        {
            var request = new RuntimeStateRequest
            {
                Token = m_NextStateToken++,
                Priority = priority,
                Sequence = ++m_RequestSequence,
                State = NormalizeStateName(stateName),
                Hash = StateNameToHash(stateName)
            };
            m_ActiveStateRequests.Add(request);
            return request.Token;
        }

        public void UpdateState(int token, string stateName, int priority = 0)
        {
            string normalized = NormalizeStateName(stateName);
            int hash = StateNameToHash(normalized);
            for (int i = 0; i < m_ActiveStateRequests.Count; ++i)
            {
                if (m_ActiveStateRequests[i].Token != token)
                    continue;

                var request = m_ActiveStateRequests[i];
                request.State = normalized;
                request.Hash = hash;
                request.Priority = priority;
                request.Sequence = ++m_RequestSequence;
                m_ActiveStateRequests[i] = request;
                return;
            }

            AcquireState(normalized, priority);
        }

        public void ReleaseState(int token)
        {
            for (int i = m_ActiveStateRequests.Count - 1; i >= 0; --i)
            {
                if (m_ActiveStateRequests[i].Token == token)
                {
                    m_ActiveStateRequests.RemoveAt(i);
                    break;
                }
            }
        }

        public void SetState(string stateName)
        {
            m_ManualState = NormalizeStateName(stateName);
            m_ManualStateHash = StateNameToHash(m_ManualState);
            m_HasManualState = !string.IsNullOrEmpty(m_ManualState);
        }

        public void ClearState()
        {
            m_ManualState = string.Empty;
            m_ManualStateHash = 0;
            m_HasManualState = false;
        }

        public void ClearAllStates()
        {
            ClearState();
            m_ActiveStateRequests.Clear();
        }

        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            UpdateListOfChildren();
            foreach (var vcam in m_ChildCameras)
                vcam.OnTargetObjectWarped(target, positionDelta);
            base.OnTargetObjectWarped(target, positionDelta);
        }

        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            UpdateListOfChildren();
            foreach (var vcam in m_ChildCameras)
                vcam.ForceCameraPosition(pos, rot);
            base.ForceCameraPosition(pos, rot);
        }

        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            InvokeOnTransitionInExtensions(fromCam, worldUp, deltaTime);
            m_TransitioningFrom = fromCam;
            InternalUpdateCameraState(worldUp, deltaTime);
        }

        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            UpdateListOfChildren();
            CinemachineVirtualCameraBase best = ChooseCurrentCamera();
            if (best != null && !best.gameObject.activeInHierarchy)
            {
                best.gameObject.SetActive(true);
                best.UpdateCameraState(worldUp, deltaTime);
            }

            ICinemachineCamera previousCam = LiveChild;
            LiveChild = best;

            if (previousCam != LiveChild && LiveChild != null)
            {
                LiveChild.OnTransitionFromCamera(previousCam, worldUp, deltaTime);
                CinemachineCore.Instance.GenerateCameraActivationEvent(LiveChild, previousCam);

                if (previousCam != null)
                {
                    mActiveBlend = CreateBlend(
                        previousCam, LiveChild,
                        LookupBlend(previousCam, LiveChild), mActiveBlend);

                    if (mActiveBlend == null || !mActiveBlend.Uses(previousCam))
                        CinemachineCore.Instance.GenerateCameraCutEvent(LiveChild);
                }
            }

            if (mActiveBlend != null)
            {
                mActiveBlend.TimeInBlend += (deltaTime >= 0)
                    ? deltaTime : mActiveBlend.Duration;
                if (mActiveBlend.IsComplete)
                    mActiveBlend = null;
            }

            if (mActiveBlend != null)
            {
                mActiveBlend.UpdateCameraState(worldUp, deltaTime);
                m_State = mActiveBlend.State;
            }
            else if (LiveChild != null)
            {
                if (m_TransitioningFrom != null)
                    LiveChild.OnTransitionFromCamera(m_TransitioningFrom, worldUp, deltaTime);
                m_State = LiveChild.State;
            }

            m_TransitioningFrom = null;
            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
            PreviousStateIsValid = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InvalidateListOfChildren();
            ClearAllStates();
            mActiveBlend = null;
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineDebug.OnGUIHandlers += OnGuiHandler;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            InvalidateListOfChildren();
            ValidateInstructions();
        }

        public void OnTransformChildrenChanged()
        {
            InvalidateListOfChildren();
        }

        void OnGuiHandler()
        {
            if (!m_ShowDebugText)
            {
                CinemachineDebug.ReleaseScreenPos(this);
                return;
            }

            var sb = CinemachineDebug.SBFromPool();
            sb.Append(Name);
            sb.Append(": ");
            sb.Append(Description);
            string text = sb.ToString();
            Rect rect = CinemachineDebug.GetScreenPos(this, text, GUI.skin.box);
            GUI.Label(rect, text, GUI.skin.box);
            CinemachineDebug.ReturnToPool(sb);
        }

        CameraState m_State = CameraState.Default;
        ICinemachineCamera m_TransitioningFrom;
        float mActivationTime = 0f;
        Instruction mActiveInstruction;
        float mPendingActivationTime = 0f;
        Instruction mPendingInstruction;
        CinemachineBlend mActiveBlend = null;
        Dictionary<int, int> mInstructionDictionary;
        Dictionary<int, int> mStateParentLookup;

        void InvalidateListOfChildren()
        {
            m_ChildCameras = null;
            LiveChild = null;
        }

        void UpdateListOfChildren()
        {
            if (m_ChildCameras != null && mInstructionDictionary != null && mStateParentLookup != null)
                return;

            var list = new List<CinemachineVirtualCameraBase>();
            var kids = GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
            foreach (var child in kids)
            {
                if (child.transform.parent == transform)
                    list.Add(child);
            }

            m_ChildCameras = list.ToArray();
            ValidateInstructions();
        }

        public void ValidateInstructions()
        {
            if (m_Instructions == null)
                m_Instructions = Array.Empty<Instruction>();

            mInstructionDictionary = new Dictionary<int, int>();
            var parents = new Dictionary<int, int>();

            for (int i = 0; i < m_Instructions.Length; ++i)
            {
                var instruction = m_Instructions[i];
                instruction.m_State = NormalizeStateName(instruction.m_State);
                instruction.m_FullHash = StateNameToHash(instruction.m_State);

                if (instruction.m_VirtualCamera != null
                    && instruction.m_VirtualCamera.transform.parent != transform)
                {
                    instruction.m_VirtualCamera = null;
                }

                m_Instructions[i] = instruction;
                mInstructionDictionary[instruction.m_FullHash] = i;

                string parentState = instruction.m_State;
                while (!string.IsNullOrEmpty(parentState))
                {
                    string nextParent = GetParentState(parentState);
                    if (string.IsNullOrEmpty(nextParent))
                        break;

                    int childHash = StateNameToHash(parentState);
                    int parentHash = StateNameToHash(nextParent);
                    if (childHash != 0 && parentHash != 0 && !parents.ContainsKey(childHash))
                        parents[childHash] = parentHash;

                    parentState = nextParent;
                }
            }

            var parentHashes = new ParentHash[parents.Count];
            int index = 0;
            foreach (var pair in parents)
                parentHashes[index++] = new ParentHash(pair.Key, pair.Value);
            m_ParentHash = parentHashes;

            mStateParentLookup = new Dictionary<int, int>();
            foreach (var pair in parents)
                mStateParentLookup[pair.Key] = pair.Value;

            mActivationTime = 0f;
            mPendingActivationTime = 0f;
            mActiveBlend = null;
        }

        CinemachineVirtualCameraBase ChooseCurrentCamera()
        {
            if (m_ChildCameras == null || m_ChildCameras.Length == 0)
            {
                mActivationTime = 0f;
                return null;
            }

            CinemachineVirtualCameraBase defaultCam = m_ChildCameras[0];

            bool hasState = TryGetEffectiveState(out int hash, out _);
            if (!hasState)
            {
                if (!mInstructionDictionary.ContainsKey(0))
                {
                    mActivationTime = 0f;
                    mPendingActivationTime = 0f;
                    return defaultCam;
                }
                hash = 0;
            }

            while (hash != 0 && !mInstructionDictionary.ContainsKey(hash))
                hash = mStateParentLookup.ContainsKey(hash) ? mStateParentLookup[hash] : 0;

            float now = CinemachineCore.CurrentTime;
            if (mActivationTime != 0f)
            {
                if (mActiveInstruction.m_FullHash == hash)
                {
                    mPendingActivationTime = 0f;
                    return mActiveInstruction.m_VirtualCamera;
                }

                if (PreviousStateIsValid
                    && mPendingActivationTime != 0f
                    && mPendingInstruction.m_FullHash == hash)
                {
                    if ((now - mPendingActivationTime) > mPendingInstruction.m_ActivateAfter
                        && ((now - mActivationTime) > mActiveInstruction.m_MinDuration
                            || mPendingInstruction.m_VirtualCamera.Priority
                            > mActiveInstruction.m_VirtualCamera.Priority))
                    {
                        mActiveInstruction = mPendingInstruction;
                        mActivationTime = now;
                        mPendingActivationTime = 0f;
                    }
                    return mActiveInstruction.m_VirtualCamera;
                }
            }

            mPendingActivationTime = 0f;

            if (!mInstructionDictionary.ContainsKey(hash))
            {
                if (mActivationTime != 0f)
                    return mActiveInstruction.m_VirtualCamera;
                return defaultCam;
            }

            Instruction newInstruction = m_Instructions[mInstructionDictionary[hash]];
            if (newInstruction.m_VirtualCamera == null)
                newInstruction.m_VirtualCamera = defaultCam;

            if (PreviousStateIsValid && mActivationTime > 0f)
            {
                if (newInstruction.m_ActivateAfter > 0f
                    || ((now - mActivationTime) < mActiveInstruction.m_MinDuration
                        && newInstruction.m_VirtualCamera.Priority
                        <= mActiveInstruction.m_VirtualCamera.Priority))
                {
                    mPendingInstruction = newInstruction;
                    mPendingActivationTime = now;
                    if (mActivationTime != 0f)
                        return mActiveInstruction.m_VirtualCamera;
                    return defaultCam;
                }
            }

            mActiveInstruction = newInstruction;
            mActivationTime = now;
            return mActiveInstruction.m_VirtualCamera;
        }

        bool TryGetEffectiveState(out int hash, out string stateName)
        {
            RuntimeStateRequest? bestRequest = null;
            for (int i = 0; i < m_ActiveStateRequests.Count; ++i)
            {
                var request = m_ActiveStateRequests[i];
                if (!bestRequest.HasValue
                    || request.Priority > bestRequest.Value.Priority
                    || (request.Priority == bestRequest.Value.Priority
                        && request.Sequence > bestRequest.Value.Sequence))
                {
                    bestRequest = request;
                }
            }

            if (bestRequest.HasValue)
            {
                var request = bestRequest.Value;
                hash = request.Hash;
                stateName = request.State;
                return true;
            }

            if (m_HasManualState)
            {
                hash = m_ManualStateHash;
                stateName = m_ManualState;
                return true;
            }

            hash = 0;
            stateName = string.Empty;
            return false;
        }

        static string NormalizeStateName(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
                return string.Empty;

            string normalized = stateName.Trim();
            if (normalized.Equals("default", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("(default)", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return normalized;
        }

        static string GetParentState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return string.Empty;

            int dotIndex = stateName.LastIndexOf('.');
            int slashIndex = stateName.LastIndexOf('/');
            int index = Math.Max(dotIndex, slashIndex);
            if (index <= 0)
                return string.Empty;

            return stateName.Substring(0, index);
        }

        CinemachineBlendDefinition LookupBlend(ICinemachineCamera fromKey, ICinemachineCamera toKey)
        {
            CinemachineBlendDefinition blend = m_DefaultBlend;
            if (m_CustomBlends != null)
            {
                string fromCameraName = fromKey != null ? fromKey.Name : string.Empty;
                string toCameraName = toKey != null ? toKey.Name : string.Empty;
                blend = m_CustomBlends.GetBlendForVirtualCameras(fromCameraName, toCameraName, blend);
            }

            if (CinemachineCore.GetBlendOverride != null)
                blend = CinemachineCore.GetBlendOverride(fromKey, toKey, blend, this);

            return blend;
        }
    }
}
