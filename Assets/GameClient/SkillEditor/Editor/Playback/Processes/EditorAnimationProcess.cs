namespace SkillEditor.Editor
{
    /// <summary>
    /// Editor preview animation process.
    /// Uses AnimationUtils (Playables) and does not depend on ISkillAnimationHandler.
    /// </summary>
    [ProcessBinding(typeof(SkillAnimationClip), PlayMode.EditorPreview)]
    public class EditorAnimationProcess : ProcessBase<SkillAnimationClip>
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
                AnimationUtils.EnsureInitialized(context.Owner);
            });

            context.RegisterTickAction(tickActionKey, (currentTime, deltaTime) =>
            {
                AnimationUtils.Tick(context.Owner, currentTime, deltaTime, context.GlobalPlaySpeed);
            });

            context.RegisterCleanup(cleanupActionKey, () =>
            {
                AnimationUtils.Dispose(context.Owner);
            });

            if (clip.animationClip != null && !string.IsNullOrEmpty(clip.clipId))
            {
                AnimationUtils.RegisterClip(context.Owner, clip);
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
                AnimationUtils.UnregisterClip(context.Owner, clip.clipId);
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
