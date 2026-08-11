using Game.Framework;
using Game.Scene;

namespace Game.UI
{
    /// <summary>
    /// 全局场景加载进度条界面
    /// 位于 Loading 层级，用于遮挡下层的界面切换过程并向玩家展示进度
    /// </summary>
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/Loading/LoadingPanel.prefab", Layer = UILayer.Loading, IsFullScreen = true)]
    public class LoadingModule : UIModule<LoadingView, LoadingModel>
    {
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnShow(object data)
        {
            Model.Progress = 0f;
            Model.LoadingText = "准备加载...";
            
            // 监听进度事件与结束事件
            EventCenter.Subscribe<SceneLoadProgressEvent>(OnProgressUpdate);
            EventCenter.Subscribe<SceneChangeEndEvent>(OnSceneLoadEnd);
            
            View?.UpdateProgress(Model.Progress, Model.LoadingText);
        }

        protected override void OnHide()
        {
            EventCenter.Unsubscribe<SceneLoadProgressEvent>(OnProgressUpdate);
            EventCenter.Unsubscribe<SceneChangeEndEvent>(OnSceneLoadEnd);
        }

        private void OnSceneLoadEnd(SceneChangeEndEvent e)
        {
            // 在底层的场景彻底加载完毕后，通知 UIManager 把自己关掉
            UIManager.Instance.Close(this);
        }

        private void OnProgressUpdate(SceneLoadProgressEvent e)
        {
            Model.Progress = e.Progress;
            if (!string.IsNullOrEmpty(e.LoadingText))
            {
                Model.LoadingText = e.LoadingText;
            }
            
            View?.UpdateProgress(Model.Progress, Model.LoadingText);
        }
    }
}
