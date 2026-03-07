using UnityEngine;
using Game.Input;
using Game.Audio;

namespace Game.Logic.Character.SubStates
{
    public class GroundDodgeSubState : GroundSubState
    {
        private bool _canTransitionToDash;
        private bool _isAnimFinished;

        public override void OnEnter()
        {
            _canTransitionToDash = false;
            _isAnimFinished = false;
            _ctx.Blackboard.LastDodgeTime = Time.time;

            // ---动画---
            AnimationClip playClip = null;
            float fadeDuration = 0.1f;

            var animSet = _ctx.HostEntity.CurrentAnimSet;
            if (animSet != null)
            {
                if (_ctx.Blackboard.CurrentDodgeType == DodgeType.Front)
                {
                    playClip = animSet.DodgeFront?.clip;
                    fadeDuration = animSet.DodgeFront?.fadeDuration ?? 0.1f;
                }
                else
                {
                    playClip = animSet.DodgeBack?.clip;
                    fadeDuration = animSet.DodgeBack?.fadeDuration ?? 0.1f;
                }
            }

            if (playClip != null)
            {
                // 使用力斩重置时间，确保闪避永远是从头干脆利落地触发
                _ctx.HostEntity.AnimController?.PlayAnim(playClip, fadeDuration, forceResetTime: true);

                // 绝区零核心体验：在动作即将结束时（取消窗口）打开派生冲刺的允许标记
                // 比如倒数 0.15 秒时允许派生 (考虑到混合可能，安全退让兜底0)
                float cancelWindowTime = .7f;
                _ctx.HostEntity.AnimController?.AddEventToCurrentAnim(
                    cancelWindowTime, 
                    () => { _canTransitionToDash = true; }
                );
                
                // 彻底结束的防死锁保障
                _ctx.HostEntity.AnimController?.AddEventToCurrentAnim(
                    playClip.length, 
                    () => { _isAnimFinished = true; }
                );
            }
            else
            {
                // 如果策划没配动画，直接结束避免卡死
                _isAnimFinished = true;
            }
            // ---音效---
            var audioBank = Game.Config.ConfigManager.Instance.GetConfigSO<Game.Audio.Config.CommonActionAudioSO>();
            if (audioBank != null)
            {
                var clip = _ctx.Blackboard.CurrentDodgeType == DodgeType.Front?
                audioBank.GetRandomClip(audioBank.DodgeFront):
                audioBank.GetRandomClip(audioBank.DodgeBack);
                if (clip != null)
                {
                    // 这里未来可能变成 AudioManager.PlayAudio 传坐标和 3D 设置
                    var arg = new AudioArgs
                    {
                        parent = _ctx.HostEntity.transform
                    };
                    Game.Audio.AudioManager.Instance?.PlayAudio(clip, Game.Audio.AudioChannel.SFX, arg);
                }
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null) return;

            // --- RootMotion 下的弱转向手感 ---
            if (provider.HasMovementInput())
            {
                Vector2 inputDir = provider.GetMovementDirection();
                Vector3 worldDir = _ctx.CalculateWorldDirection(inputDir);
                
                // 给予玩家极其缓慢的根节点转向能力 (Dodge 时的阻尼，比正常15慢)
                Quaternion targetRotation = Quaternion.LookRotation(worldDir);
                _ctx.HostEntity.transform.rotation = Quaternion.Slerp(
                    _ctx.HostEntity.transform.rotation, 
                    targetRotation, 
                    deltaTime * 5f
                );
            }

            // --- 行云流水的派生逻辑 ---
            if (_canTransitionToDash || _isAnimFinished)
            {
                // 只要玩家有方向输入 -> 闪避后默认毫无阻碍地进入 Dash 冲刺（从此不再检测 Shift 键长按）
                if (provider.HasMovementInput())
                {
                    ChangeState(_ctx.DashState);
                    return;
                }

                // 完全结束后，如果没有推摇杆，则归位待机
                if (_isAnimFinished)
                {
                    ChangeState(_ctx.IdleState);
                }
            }
            _ctx.HostEntity.AnimController?.SetSpeed(0, _ctx.HostEntity.Config.DodgeMultipier);
        }
    }
}
