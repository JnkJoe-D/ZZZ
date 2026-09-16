using System.Diagnostics;

namespace ATEditor.Editor
{
    /// <summary>
    /// 编辑器预览动画处理进程。
    /// 基于 AnimationUtils (Playables) 直接驱动动画采样，不依赖运行时 ISkillAnimationHandler。
    /// </summary>
    [ProcessBinding(typeof(AnimationClip), PlayMode.EditorPreview)]
    public class EditorAnimationProcess : ProcessBase<AnimationClip>
    {
        private string tickActionKey;
        private string startActionKey;
        private string cleanupActionKey;
        private bool clipRegistered;

        public override void OnEnable()
        {
            if (context == null || clip == null || context.Owner == null)
            {
                return;
            }

            int ownerId = context.Owner.GetInstanceID();
            tickActionKey = $"EditorPreview.Animation.Tick.{ownerId}";
            startActionKey = $"EditorPreview.Animation.Start.{ownerId}";
            cleanupActionKey = $"EditorPreview.Animation.Cleanup.{ownerId}";

            context.RegisterStartAction(startActionKey, () =>
            {
                EditorAnimationUtils.EnsureInitialized(context.Owner);
            });

            context.RegisterTickAction(tickActionKey, (currentTime, deltaTime) =>
            {
                EditorAnimationUtils.Tick(context.Owner, currentTime, deltaTime, context.GlobalPlaySpeed);
            });

            context.RegisterCleanup(cleanupActionKey, () =>
            {
                EditorAnimationUtils.Dispose(context.Owner);
            });

            if (clip.animationClip != null && !string.IsNullOrEmpty(clip.clipId))
            {
                EditorAnimationUtils.RegisterClip(context.Owner, clip);
                clipRegistered = true;
            }
                    }

        public override void OnEnter()
        {
                    }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
                    }

        public override void OnExit()
        {
                    }

        public override void OnDisable()
        {
            context?.UnregisterStartAction(startActionKey);
            context?.UnregisterTickAction(tickActionKey);

            if (clipRegistered && context != null && context.Owner != null && !string.IsNullOrEmpty(clip?.clipId))
            {
                EditorAnimationUtils.UnregisterClip(context.Owner, clip.clipId);
            }

            clipRegistered = false;
        }

        public override void Reset()
        {
            base.Reset();
            tickActionKey = null;
            startActionKey = null;
            cleanupActionKey = null;
            clipRegistered = false;
        }
    }
}
