using UnityEngine;

namespace ATEditor
{
    /// <summary>独立示例的观察计数与重播按钮；不实现伤害规则。</summary>
    public sealed class AttackDetectionSample : MonoBehaviour
    {
        private ActionDirector _director;
        private int _count;
        private string _lastTarget = "无";
        public int ObservationCount => _count;

        private void OnEnable()
        {
            _director = GetComponentInChildren<ActionDirector>();
            if (_director != null) _director.AttackObserved += Observe;
        }
        private void OnDisable() { if (_director != null) _director.AttackObserved -= Observe; }
        private void Observe(AttackObservation observation)
        {
            _count++; _lastTarget = observation.Target != null ? observation.Target.name : "已销毁";
            Debug.DrawLine(observation.QueryCenter, observation.ContactPoint, Color.red, 1f);
        }
        private void OnGUI()
        {
            GUI.Label(new Rect(16, 16, 480, 30), $"ATEditor 攻击观察：{_count}　最近目标：{_lastTarget}");
            if (GUI.Button(new Rect(16, 48, 120, 30), "重播检测") && _director != null)
            {
                _director.Stop(); _director.transform.localPosition = Vector3.zero;
                _count = 0; _lastTarget = "无"; _director.Play();
            }
        }
    }
}
