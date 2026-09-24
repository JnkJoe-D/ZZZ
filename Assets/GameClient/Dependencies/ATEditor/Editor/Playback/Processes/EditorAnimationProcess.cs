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

            var owner = context.Owner;
            int ownerId = owner.GetInstanceID();
            tickActionKey = $"EditorPreview.Animation.Tick.{ownerId}";
            startActionKey = $"EditorPreview.Animation.Start.{ownerId}";
            cleanupActionKey = $"EditorPreview.Animation.Cleanup.{ownerId}";

            context.RegisterStartAction(startActionKey, () =>
            {
                if (owner != null)
                {
                    EditorAnimationUtils.EnsureInitialized(owner);
                }
            });

            context.RegisterTickAction(tickActionKey, (currentTime, deltaTime) =>
            {
                if (owner != null)
                {
                    EditorAnimationUtils.Tick(owner, currentTime, deltaTime, context?.PresentationPlaySpeed ?? 1.0f);
                }
            });

            context.RegisterCleanup(cleanupActionKey, () =>
            {
                if (owner != null)
                {
                    EditorAnimationUtils.Dispose(owner);
                }
            });

            if (clip.animationClip != null && !string.IsNullOrEmpty(clip.clipId))
            {
                EditorAnimationUtils.RegisterClip(owner, clip);
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

        public override void OnStop()
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
