#if UNITY_EDITOR
using UnityEngine;

namespace ATEditor.Test
{
    public sealed class ActionEffectActivationProbe : MonoBehaviour
    {
        public Vector3 PositionAtEnable;
        public Vector3 ScaleAtEnable;
        private void OnEnable()
        {
            PositionAtEnable = transform.position;
            ScaleAtEnable = transform.localScale;
        }
    }
}
#endif
