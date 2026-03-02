using UnityEngine;
namespace Game.Logic.Player
{
    [RequireComponent(typeof(PlayerEntity))]
    public class MovementController : MonoBehaviour, IMovementController
    {
        private CharacterController _cc;

        void Awake()
        {
            // 撤除刚体，换上专为玩家量身定制的运动胶囊体
            _cc = gameObject.GetComponent<CharacterController>();
            if (_cc == null)
            {
                _cc = gameObject.AddComponent<CharacterController>();
                // 初始化符合您所说的大致尺寸
                _cc.height = 1.6f;
                _cc.radius = 0.3f; // 稍微窄一点防卡墙
                _cc.center = new Vector3(0, 0.8f, 0);
                _cc.excludeLayers = LayerMask.GetMask("Player"); 
            }
        }

        public void Move(Vector3 moveDelta)
        {
            // if (_cc != null && _cc.enabled)
            // {
            //     // 人为施加一个微茫的垂直向下重力牵引，保证下坡时不腾空贴合地面
            //     moveDelta.y -= 2f * Time.deltaTime; 
                
            //     // 使用自带平滑步幅属性的 CC.Move
            //     _cc.Move(moveDelta);
            // }
            // else
            // {
            //     transform.position += moveDelta;
            // }
        }

        public float TurnSpeed = 15f; // 转身平滑度

        public void FaceTo(Vector3 lookDirection)
        {
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * TurnSpeed);
            }
        }
    }
}