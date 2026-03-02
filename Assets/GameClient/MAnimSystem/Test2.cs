using System.Collections;
using UnityEngine;
namespace Game.MAnimSystem
{
public class Test2 : MonoBehaviour
{
    public AnimComponent animComponent;
    [Range(0,3f)]
    public float speed = 1;
    [Header("动画片段 (Clip)")]
    public AnimationClip clip1;
    public float blendInDuration1 = 0.2f; // 淡入时间
    public AnimationClip clip2;
    public float blendInDuration2 = 0.2f; // 淡入时间
    public AnimationClip clip3;
    public float blendInDuration3 = 0.2f; // 淡入时间
    public AnimationClip clip4;
    public float blendInDuration4 = 0.2f; // 淡入时间
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        animComponent?.SetLayerSpeed(0, speed);
        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
        {
            AnimState state = animComponent.Play(clip1, blendInDuration1);
            state.AddScheduledEvent(1.6999998092651368f, (_) =>
            {
                Debug.Log("事件触发: " + blendInDuration2 + "秒");
                animComponent.Play(clip2, blendInDuration2);
            });
        }
        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
        {
            animComponent.Play(clip2, blendInDuration2);
        }
        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
        {
            animComponent.Play(clip3, blendInDuration3);
        }
        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4))
        {
            animComponent.Play(clip4, blendInDuration4);
        }
    }
}
}