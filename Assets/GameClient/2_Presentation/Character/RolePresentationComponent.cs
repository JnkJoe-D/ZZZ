using System.Collections.Generic;
using UnityEngine;
using Game.GamePlay;

namespace Game.Presentation
{
    /// <summary>
    /// 角色网格表现与碰撞体层级适配组件。
    /// 负责管理角色模型的显隐、阴影投射以及换人时的碰撞体分层开闭与相机绑定。
    /// 属于表现层实现，贯彻 IRolePresentation 契约。
    /// </summary>
    [DisallowMultipleComponent]
    public class RolePresentationComponent : MonoBehaviour, IRolePresentation
    {
        private CharacterEntity _owner;
        public CharacterEntity OwnerEntity => _owner;

        private readonly Dictionary<Renderer, bool> _rendererVisibleStates = new();
        private readonly Dictionary<Collider, bool> _colliderEnabledStates = new();

        public bool IsPresentationVisible { get; private set; } = true;
        public ICameraController CameraController { get; private set; }
        public CameraPointBinder CameraPointBinder { get; private set; }
        public IVisualOffsetPresenter VisualOffsetPresenter { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegisterPresentationBinder()
        {
            RolePresentationRegistry.Register((go, entity) =>
            {
                var comp = go.GetComponent<RolePresentationComponent>() 
                    ?? go.AddComponent<RolePresentationComponent>();
                return comp;
            });
        }

        private void Awake()
        {
            var role = GetComponent<RoleEntity>();
            if (role != null)
            {
                role.BindPresentation(this);
            }

            VisualOffsetPresenter = GetComponent<CharacterVisualOffsetComponent>() 
                ?? gameObject.AddComponent<CharacterVisualOffsetComponent>();
        }

        public void OnComponentInit(CharacterEntity owner)
        {
            _owner = owner;
            CachePresentationState();

            CameraController = GetComponent<CharacterCameraController>() 
                ?? gameObject.AddComponent<CharacterCameraController>();
            CameraPointBinder = GetComponent<CameraPointBinder>() 
                ?? gameObject.AddComponent<CameraPointBinder>();

            VisualOffsetPresenter = GetComponent<CharacterVisualOffsetComponent>() 
                ?? gameObject.AddComponent<CharacterVisualOffsetComponent>();

            if (_owner is RoleEntity role)
            {
                CameraController.Init(role);
            }
        }

        public void OnComponentSpawn()
        {
            SetPresentationVisible(true);
            SetColliderActive(true);
        }

        public void OnComponentDespawn()
        {
            SetPresentationVisible(false);
            SetColliderActive(false);
        }

        public void CachePresentationState()
        {
            _rendererVisibleStates.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    _rendererVisibleStates[r] = r.enabled;
                }
            }

            _colliderEnabledStates.Clear();
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                if (c != null)
                {
                    _colliderEnabledStates[c] = c.enabled;
                }
            }
        }

        public void SetPresentationVisible(bool visible)
        {
            IsPresentationVisible = visible;

            foreach (KeyValuePair<Renderer, bool> pair in _rendererVisibleStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = visible && pair.Value;
                }
            }
        }

        public void SetColliderActive(bool active)
        {
            LayerMask excludeMask = 0;
            if (!active)
            {
                excludeMask = LayerMask.GetMask("LocalRole", "Character", "CharHit");
            }

            foreach (KeyValuePair<Collider, bool> pair in _colliderEnabledStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = active && pair.Value;
                    pair.Key.excludeLayers = excludeMask;
                }
            }
        }

        public void SetCameraActive(bool active, bool enableInput = true)
        {
            CameraController?.SetCameraActive(active);
            CameraController?.EnableInput(active && enableInput);
        }
    }
}
