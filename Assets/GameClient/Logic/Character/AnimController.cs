using Game.MAnimSystem;
using UnityEngine;
namespace Game.Logic.Character
{
[RequireComponent(typeof(CharacterEntity))]
public class AnimController:MonoBehaviour, IAnimController
{
    AnimComponent _animComponent;
    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        _animComponent = gameObject.GetComponent<AnimComponent>();
        if(_animComponent==null)
        {
            _animComponent= gameObject.AddComponent<AnimComponent>();
        }
    }
    private Game.MAnimSystem.AnimState _lastPlayedState;

    public void PlayAnim(AnimationClip clip, float fadeDuration = 0.2f, System.Action onFadeComplete = null, System.Action onAnimEnd = null, bool forceResetTime = false)
    {
        if (clip != null)
        {
            // 注意：已移除原先的 _lastAnim 字符串拦截。
            // 因为混合结构下，上层触发器、AnimChain 延迟器会导致缓存严重不同步！
            // 我们的 HFSM 保证了 PlayAnim 仅在 OnEnter 单次触发，无需在此多此一举设卡。

            Game.MAnimSystem.AnimState state = _animComponent.Play(clip, fadeDuration, forceResetTime);
            _lastPlayedState = state;

            // 闭包适配器：隔离 MAnimSystem 污染
            if (state != null)
            {
                if (onFadeComplete != null)
                {
                    state.OnFadeComplete += (s) => onFadeComplete.Invoke();
                }
                if (onAnimEnd != null)
                {
                    state.OnEnd += (s) => onAnimEnd.Invoke();
                }
            }
                
            Debug.Log($"[动画测试桩] 角色动画已切换为 ---> {clip.name}");
        }
    }
    
    public void AddEventToCurrentAnim(float time, System.Action callback)
    {
        if (_lastPlayedState != null && callback != null)
        {
            _lastPlayedState.AddScheduledEvent(time, (s) => callback.Invoke());
        }
    }

    public void SetSpeed(int layerIndex, float speed)
    {
        _animComponent.SetLayerSpeed(layerIndex,speed);
    }
}
}