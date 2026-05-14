using Game.Camera;
using Game.Input;
using UnityEngine;

namespace Game.Logic.Character
{
    public class RoleEntity : CharacterEntity
    {
        protected override bool AutoBindInputOnStart => false;
        protected override bool AutoAssignCameraOnStart => false;
        public override IInputProvider InputProvider => TeamContext?.InputProvider ?? base.InputProvider;
        public override TargetFinder TargetFinder => TeamContext?.TargetFinder ?? base.TargetFinder;

        protected override void InitRequiredComponents()
        {
            MovementController = GetComponent<MovementController>();
            if (MovementController == null) MovementController = gameObject.AddComponent<MovementController>();

            CharacterCameraController cameraController = GetComponent<CharacterCameraController>();
            if (cameraController == null) cameraController = gameObject.AddComponent<CharacterCameraController>();
            SetCameraController(cameraController);

            HitReactionModule = GetComponent<HitReactionModule>();
            if (HitReactionModule == null) HitReactionModule = gameObject.AddComponent<HitReactionModule>();

            CameraPointBinder cameraPointBinder = GetComponent<CameraPointBinder>();
            if (cameraPointBinder == null) cameraPointBinder = gameObject.AddComponent<CameraPointBinder>();
        }
    }
}
