using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class LoginBackgroundView : UIView
    {
        public RawImage Background { get; private set; }
        public UnityEngine.Video.VideoPlayer VideoPlayer { get; private set; }

        public override void OnInit()
        {
            base.OnInit();
            Background = transform.Find("View/Background")?.GetComponent<RawImage>();
            VideoPlayer = transform.Find("View/VideoPlayer")?.GetComponent<UnityEngine.Video.VideoPlayer>();
        }
    }
}
