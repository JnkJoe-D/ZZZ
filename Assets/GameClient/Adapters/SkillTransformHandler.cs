using Game.Logic.Character;
using SkillEditor;
using UnityEngine;

namespace Game.Adapters
{
    public class SkillTransformHandler : ISkillTransformHandler
    {
        private CharacterEntity _entity;

        public SkillTransformHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void Move(Vector3 delta)
        {
            _entity.MovementController?.Move(delta);
        }

        public void SetPosition(Vector3 position)
        {
            _entity.transform.position = position;
        }

        public Vector3 GetPosition()
        {
            return _entity.transform.position;
        }

        public Transform GetTarget()
        {
            return _entity.TargetFinder?.GetEnemy();
        }

        public float GetRadius()
        {
            var cc = _entity.GetComponent<CharacterController>();
            return cc != null ? cc.radius + cc.skinWidth : 0.5f;
        }

        public float GetTargetRadius()
        {
            var target = GetTarget();
            if (target != null)
            {
                var cc = target.GetComponent<CharacterController>();
                if (cc != null) return cc.radius + cc.skinWidth;
            }
            return 0.5f;
        }

        public void SetExcludeLayers(LayerMask mask)
        {
            var cc = _entity.GetComponent<CharacterController>();
            if (cc != null) cc.excludeLayers = mask;
        }

        public LayerMask GetExcludeLayers()
        {
            var cc = _entity.GetComponent<CharacterController>();
            return cc != null ? cc.excludeLayers : (LayerMask)0;
        }

        public void SetRotation(Quaternion rotation)
        {
            _entity.transform.rotation = rotation;
        }

        public void RotateTowards(Quaternion targetRotation, float speed)
        {
            // 优先使用 MovementController 的 TurnSpeed 如果 speed 为默认
            float finalSpeed = speed > 0 ? speed : (_entity.MovementController as MovementController)?.TurnSpeed ?? 15f;
            _entity.transform.rotation = Quaternion.Slerp(_entity.transform.rotation, targetRotation, Time.deltaTime * finalSpeed);
        }

        public Quaternion GetRotation()
        {
            return _entity.transform.rotation;
        }

        public Vector3 GetInputDirection(bool withCamera)
        {
            if (_entity.InputProvider == null) return Vector3.zero;

            Vector2 input = _entity.InputProvider.GetMovementDirection();
            if (input.sqrMagnitude < 0.001f) return Vector3.zero;

            if (withCamera && _entity.MovementController != null)
            {
                return _entity.MovementController.CalculateWorldDirection(input);
            }

            return new Vector3(input.x, 0, input.y).normalized;
        }
    }
}
