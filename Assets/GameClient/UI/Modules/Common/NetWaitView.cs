using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class NetWaitView : UIView
    {
        // 自动生成的UI组件字段
        public Image SpinnerImage { get; private set; }
        public TMP_Text TipText { get; private set; }
        private Coroutine _rotateRoutine;
        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();
        }

        private void BindUIComponents()
        {
            // 自动绑定UI组件
            SpinnerImage = transform.Find("View/Content/SpinnerImage")?.GetComponent<Image>();
            TipText = transform.Find("View/Content/TipText")?.GetComponent<TMP_Text>();
        }

        public void StartRotate(float speed)
        {
            if (SpinnerImage == null) return;
            StopRotate();
            _rotateRoutine = StartCoroutine(RotateCoroutine(speed));
        }

        public void StopRotate()
        {
            if (_rotateRoutine != null)
            {
                StopCoroutine(_rotateRoutine);
                _rotateRoutine = null;
            }
        }

        private System.Collections.IEnumerator RotateCoroutine(float speed)
        {
            while (true)
            {
                SpinnerImage.transform.Rotate(0, 0, -speed * Time.deltaTime);
                yield return null;
            }
        }
    }
}
