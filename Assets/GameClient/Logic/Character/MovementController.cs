using Game.AI;
using UnityEngine;
namespace Game.Logic.Character
{
    [RequireComponent(typeof(CharacterEntity))]
    public class MovementController : MonoBehaviour, IMovementController
    {
        private CharacterController _cc;
        private CharacterEntity _entity;
        public float TurnSpeed => 15f; // 转身平滑度
        void Awake()
        {
            // 撤除刚体，换上专为玩家量身定制的运动胶囊体
            _cc = gameObject.GetComponent<CharacterController>();
            if (_cc == null)
            {
                _cc = gameObject.AddComponent<CharacterController>();

                _cc.height = 1.6f;
                _cc.radius = 0.3f; // 稍微窄一点防卡墙
                _cc.center = new Vector3(0, 0.8f, 0);
                _cc.excludeLayers = LayerMask.GetMask("Player"); 
            }
        }
        public void Init(CharacterEntity entity)
        {
            _entity = entity;
        }
        public void Move(Vector3 moveDelta)
        {

        }

        public void FaceTo(Vector3 inputDir,float speed = -1f)
        {
            var lookDirection = CalculateWorldDirection(inputDir);
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                speed = speed == -1f?TurnSpeed:speed>0?speed:TurnSpeed;
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            }
        }
        public void FaceToImmediately(Vector3 inputDir)
        {
            var lookDirection = CalculateWorldDirection(inputDir);
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                _entity.transform.forward = lookDirection.normalized;
            }
        }
        /// <summary>
        /// 将 2D 的手柄拉动或者 WASD 转换为考虑主相机的绝对世界朝向
        /// </summary>
        public Vector3 CalculateWorldDirection(Vector2 inputDir)
        {
            if (_entity.InputProvider is AIInputProvider aiInputProvider &&
                aiInputProvider.TryGetWorldMovementDirection(out Vector3 aiWorldDirection))
            {
                return aiWorldDirection.normalized;
            }

            if (_entity.CameraController != null)
            {
                Vector3 camForward = _entity.CameraController.GetForward();
                Vector3 camRight = _entity.CameraController.GetRight();
                return (camForward * inputDir.y + camRight * inputDir.x).normalized;
            }
            else
            {
                return new Vector3(inputDir.x, 0, inputDir.y).normalized;
            }
        }
        public bool IsGrounded
        {
            get
            {
                if (_cc != null) return _cc.isGrounded;
                return true; // Default to true if no character controller
            }
        }
    }
}