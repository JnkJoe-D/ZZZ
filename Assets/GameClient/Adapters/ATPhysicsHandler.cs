using ATEditor;
using Game.Logic;
using UnityEngine;

namespace Game.Adapters
{
    /// <summary>
    /// 技能物理控制适配器：桥接 ATEditor 物理接口与 Game.Logic 实体层
    /// </summary>
    public class ATPhysicsHandler : IPhysicsHandler
    {
        private readonly CharacterEntity entity;
        private readonly MovementController movementController;
        private readonly CharacterController cc;
        private readonly Collider col;

        public ATPhysicsHandler(GameObject owner)
        {
            if (owner != null)
            {
                entity = owner.GetComponent<CharacterEntity>();
                movementController = owner.GetComponent<MovementController>();
                cc = owner.GetComponent<CharacterController>();
                col = owner.GetComponent<Collider>();
            }
        }

        public ATPhysicsHandler(CharacterEntity entity)
        {
            this.entity = entity;
            if (entity != null)
            {
                movementController = entity.GetComponent<MovementController>();
                cc = entity.GetComponent<CharacterController>();
                col = entity.GetComponent<Collider>();
            }
        }

        public void SetExcludeLayers(LayerMask mask)
        {
            if (cc != null)
            {
                cc.excludeLayers = mask;
            }
            else if (movementController != null && movementController.CharacterController != null)
            {
                movementController.CharacterController.excludeLayers = mask;
            }
        }

        public LayerMask GetExcludeLayers()
        {
            if (cc != null)
            {
                return cc.excludeLayers;
            }
            if (movementController != null && movementController.CharacterController != null)
            {
                return movementController.CharacterController.excludeLayers;
            }
            return 0;
        }

        public void SetCollisionEnabled(bool enabled)
        {
            if (cc != null)
            {
                cc.detectCollisions = enabled;
            }
            if (col != null)
            {
                col.enabled = enabled;
            }
        }

        public bool GetCollisionEnabled()
        {
            if (cc != null)
            {
                return cc.detectCollisions;
            }
            if (col != null)
            {
                return col.enabled;
            }
            return true;
        }

        public void SetGravityScale(float scale)
        {
            if (movementController != null)
            {
                movementController.GravityScale = scale;
            }
        }

        public float GetGravityScale()
        {
            return movementController != null ? movementController.GravityScale : 1.0f;
        }

        public void ResetVerticalVelocity()
        {
            if (movementController != null)
            {
                movementController.ResetVerticalVelocity();
            }
        }

        public void SetPushResistance(float resistance)
        {
            if (movementController != null)
            {
                movementController.PushResistance = Mathf.Clamp01(resistance);
            }
        }

        public float GetPushResistance()
        {
            return movementController != null ? movementController.PushResistance : 0f;
        }
    }
}
