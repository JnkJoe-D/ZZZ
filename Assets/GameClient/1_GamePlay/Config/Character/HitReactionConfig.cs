using System;
using System.Collections.Generic;
using UnityEngine;
using cfg.ZZZ;
using Game.Framework;
namespace Game.GamePlay
{
    /// <summary>
    /// 水平受击相对方位
    /// </summary>
    public enum HitHorizontalZone
    {
        /// <summary>
        /// 正面受击 [-45°, +45°] (或 2 向下的 [-90°, +90°])
        /// </summary>
        Front = 0,

        /// <summary>
        /// 背后受击 [>135° 或 <-135°] (或 2 向下的背后半球)
        /// </summary>
        Back = 1,

        /// <summary>
        /// 左侧受击 [-135°, -45°]
        /// </summary>
        Left = 2,

        /// <summary>
        /// 右侧受击 [+45°, +135°]
        /// </summary>
        Right = 3
    }

    /// <summary>
    /// 垂直受击方位（高低位，主要用于大型怪物细分受击）
    /// </summary>
    public enum HitVerticalZone
    {
        /// <summary>
        /// 中段平击
        /// </summary>
        Center = 0,

        /// <summary>
        /// 仰角/自下而上
        /// </summary>
        Up = 1,

        /// <summary>
        /// 俯角/自上而下
        /// </summary>
        Down = 2
    }

    /// <summary>
    /// 受击转向决策策略
    /// </summary>
    public enum HitTurnaroundPolicy
    {
        /// <summary>
        /// [推荐默认] 自适应：若有对应方向动作则不转向；若只有正面动作则自动面向攻击袭来方向
        /// </summary>
        AutoByAvailability = 0,

        /// <summary>
        /// 强制面向攻击者（无论有什么动画，受击前均强行转向攻击袭来方向）
        /// </summary>
        AlwaysFaceAttacker = 1,

        /// <summary>
        /// 坚决不转向（保持当前朝向）
        /// </summary>
        NeverTurn = 2
    }

    /// <summary>
    /// 单个受击类型的多方向与转向配置条目
    /// </summary>
    [Serializable]
    public class HitReactionTypeEntry
    {
        [Tooltip("受击反馈类型")]
        public HitReactionType reactionType;

        [Tooltip("转向策略")]
        public HitTurnaroundPolicy turnaroundPolicy = HitTurnaroundPolicy.AutoByAvailability;

        [Header("基础方向动作插槽")]
        [Tooltip("正面受击动作 (核心必填，作为保底动作)")]
        public ActionConfigAsset frontAction;

        [Tooltip("背面受击动作 (选填。有此动作且背后受击时，角色将不转向直接播放前倾踉跄受击)")]
        public ActionConfigAsset backAction;

        [Tooltip("左侧受击动作 (选填)")]
        public ActionConfigAsset leftAction;

        [Tooltip("右侧受击动作 (选填)")]
        public ActionConfigAsset rightAction;

        [Header("高精细高低位动作插槽 (可选，用于怪物的 8 向细分受击)")]
        public ActionConfigAsset frontUpAction;
        public ActionConfigAsset frontDownAction;
        public ActionConfigAsset backUpAction;
        public ActionConfigAsset backDownAction;

        [Header("缺省回退")]
        [Tooltip("当该受击类型所有插槽均为空时（例如怪物无击飞 Launch），回退到的备选受击类型")]
        public HitReactionType fallbackType = HitReactionType.None;

        public bool HasAnyAction => frontAction != null || backAction != null ||
                                    leftAction != null || rightAction != null ||
                                    frontUpAction != null || frontDownAction != null ||
                                    backUpAction != null || backDownAction != null;

        public IEnumerable<ActionConfigAsset> GetAllActions()
        {
            if (frontAction != null) yield return frontAction;
            if (backAction != null) yield return backAction;
            if (leftAction != null) yield return leftAction;
            if (rightAction != null) yield return rightAction;
            if (frontUpAction != null) yield return frontUpAction;
            if (frontDownAction != null) yield return frontDownAction;
            if (backUpAction != null) yield return backUpAction;
            if (backDownAction != null) yield return backDownAction;
        }
    }

    /// <summary>
    /// 受击反应配置。定义角色与怪物级别的多方向受击参数与动画动作路由。
    /// </summary>
    [CreateAssetMenu(fileName = "HitReactionConfig", menuName = "Config/Role/Hit Reaction Config")]
    public class HitReactionConfig : GameConfigAsset
    {
        [Header("多方向受击动作分组配置")]
        [SerializeField]
        public List<HitReactionTypeEntry> reactionEntries = new List<HitReactionTypeEntry>();

        [Header("通用受击物理参数（接口占位）")]
        [Tooltip("击退力")]
        public float knockbackForce = 0f;

        [Tooltip("击飞力")]
        public float launchForce = 0f;

        [Tooltip("霸体阈值")]
        public float superArmorThreshold = 0f;

        [Header("兼容旧版本单一字段（自动迁移）")]
        [HideInInspector] public ActionConfigAsset hitAnimLight;
        [HideInInspector] public ActionConfigAsset hitAnimHeavy;
        [HideInInspector] public ActionConfigAsset hitAnimKnowAway;
        [HideInInspector] public ActionConfigAsset hitAnimShake;
        [HideInInspector] public ActionConfigAsset hitAnimStay;
        [HideInInspector] public ActionConfigAsset hitAnimKnockDown;
        [HideInInspector] public ActionConfigAsset hitAnimParry;

        [NonSerialized]
        private Dictionary<HitReactionType, HitReactionTypeEntry> _entryMap;

        private void OnEnable()
        {
            _entryMap = null;
            MigrateLegacyFieldsIfNeeded();
        }

        private void OnValidate()
        {
            _entryMap = null;
            MigrateLegacyFieldsIfNeeded();
        }

        /// <summary>
        /// 将旧的单一字段无损迁移至新的分组列表中
        /// </summary>
        private void MigrateLegacyFieldsIfNeeded()
        {
            if (reactionEntries == null)
            {
                reactionEntries = new List<HitReactionTypeEntry>();
            }

            // 若 reactionEntries 为空且旧字段有值，执行自动迁移
            if (reactionEntries.Count == 0)
            {
                AddLegacyEntryIfNotNull(HitReactionType.Light, hitAnimLight);
                AddLegacyEntryIfNotNull(HitReactionType.Heavy, hitAnimHeavy);
                AddLegacyEntryIfNotNull(HitReactionType.Launch, hitAnimKnowAway);
                AddLegacyEntryIfNotNull(HitReactionType.Shake, hitAnimShake, HitTurnaroundPolicy.NeverTurn);
                AddLegacyEntryIfNotNull(HitReactionType.Stay, hitAnimStay, HitTurnaroundPolicy.NeverTurn);
                AddLegacyEntryIfNotNull(HitReactionType.KnockDown, hitAnimKnockDown);
                AddLegacyEntryIfNotNull(HitReactionType.Parried, hitAnimParry);
            }
        }

        private void AddLegacyEntryIfNotNull(HitReactionType type, ActionConfigAsset action, HitTurnaroundPolicy policy = HitTurnaroundPolicy.AutoByAvailability)
        {
            if (action == null) return;
            reactionEntries.Add(new HitReactionTypeEntry
            {
                reactionType = type,
                turnaroundPolicy = policy,
                frontAction = action
            });
        }

        public void InitializeCache()
        {
            if (_entryMap != null) return;

            _entryMap = new Dictionary<HitReactionType, HitReactionTypeEntry>();
            if (reactionEntries != null)
            {
                foreach (var entry in reactionEntries)
                {
                    if (entry != null && !_entryMap.ContainsKey(entry.reactionType))
                    {
                        _entryMap.Add(entry.reactionType, entry);
                    }
                }
            }
        }

        /// <summary>
        /// 核心受击裁决接口：根据受击类型、水平夹角、垂直夹角，解析出目标动作与转向决断
        /// </summary>
        /// <param name="requestType">期望受击类型</param>
        /// <param name="signedHorizontalAngle">攻击袭来方向与角色朝向的水平夹角 [-180°, 180°]</param>
        /// <param name="verticalAngle">垂直相对夹角 [-90°, 90°]</param>
        /// <param name="resolvedAction">最终匹配出的动作资产</param>
        /// <param name="needFaceAttacker">是否需要让电机面向攻击袭来矢量</param>
        /// <returns>是否成功解析出可用动作</returns>
        public bool TryResolveHitAction(
            HitReactionType requestType,
            float signedHorizontalAngle,
            float verticalAngle,
            out ActionConfigAsset resolvedAction,
            out bool needFaceAttacker)
        {
            InitializeCache();
            resolvedAction = null;
            needFaceAttacker = false;

            if (requestType == HitReactionType.None)
            {
                return false;
            }

            // 1. 查找配置条目，若缺失则尝试回退
            if (!_entryMap.TryGetValue(requestType, out var entry) || !entry.HasAnyAction)
            {
                // 声明式回退
                if (entry != null && entry.fallbackType != HitReactionType.None && entry.fallbackType != requestType)
                {
                    return TryResolveHitAction(entry.fallbackType, signedHorizontalAngle, verticalAngle, out resolvedAction, out needFaceAttacker);
                }

                // 规则保底自动回退链：KnockDown -> Launch -> Heavy -> Light
                var autoFallback = requestType switch
                {
                    HitReactionType.KnockDown => HitReactionType.Launch,
                    HitReactionType.Launch => HitReactionType.Heavy,
                    HitReactionType.Heavy => HitReactionType.Light,
                    _ => HitReactionType.None
                };

                if (autoFallback != HitReactionType.None && autoFallback != requestType && _entryMap.ContainsKey(autoFallback))
                {
                    return TryResolveHitAction(autoFallback, signedHorizontalAngle, verticalAngle, out resolvedAction, out needFaceAttacker);
                }

                // 尝试从旧字段保底兜底
                resolvedAction = GetLegacyActionFallback(requestType);
                if (resolvedAction != null)
                {
                    needFaceAttacker = Mathf.Abs(signedHorizontalAngle) > 90f;
                    return true;
                }

                return false;
            }

            // 2. 方位与动作细分裁决
            resolvedAction = ResolveDirectionalAction(entry, signedHorizontalAngle, verticalAngle, out bool isHitFromBack);

            // 3. 转向策略裁决
            switch (entry.turnaroundPolicy)
            {
                case HitTurnaroundPolicy.AlwaysFaceAttacker:
                    needFaceAttacker = true;
                    break;
                case HitTurnaroundPolicy.NeverTurn:
                    needFaceAttacker = false;
                    break;
                case HitTurnaroundPolicy.AutoByAvailability:
                default:
                    if (isHitFromBack)
                    {
                        // 若命中背后：
                        // 如果成功匹配到了专用的 Back 动作 -> 保持朝向，不扭头（丝滑前倾受击）
                        // 如果只能回退降级使用 Front 动作 -> 必须面向攻击袭来矢量（确保受力退避自洽）
                        bool isPlayingBackAnim = (resolvedAction == entry.backAction ||
                                                  resolvedAction == entry.backUpAction ||
                                                  resolvedAction == entry.backDownAction);
                        needFaceAttacker = !isPlayingBackAnim;
                    }
                    else
                    {
                        needFaceAttacker = false;
                    }
                    break;
            }

            return resolvedAction != null;
        }

        private ActionConfigAsset ResolveDirectionalAction(
            HitReactionTypeEntry entry,
            float signedHorizontalAngle,
            float verticalAngle,
            out bool isHitFromBack)
        {
            isHitFromBack = Mathf.Abs(signedHorizontalAngle) > 90f;

            // 1. 高低位细分（仰角/俯角）
            if (verticalAngle > 25f)
            {
                if (isHitFromBack && entry.backUpAction != null) return entry.backUpAction;
                if (!isHitFromBack && entry.frontUpAction != null) return entry.frontUpAction;
            }
            else if (verticalAngle < -25f)
            {
                if (isHitFromBack && entry.backDownAction != null) return entry.backDownAction;
                if (!isHitFromBack && entry.frontDownAction != null) return entry.frontDownAction;
            }

            // 2. 四向侧向判定（若配置了左右侧受击）
            if (entry.leftAction != null && signedHorizontalAngle < -45f && signedHorizontalAngle > -135f)
            {
                return entry.leftAction;
            }
            if (entry.rightAction != null && signedHorizontalAngle > 45f && signedHorizontalAngle < 135f)
            {
                return entry.rightAction;
            }

            // 3. 前后二向判定
            if (isHitFromBack && entry.backAction != null)
            {
                return entry.backAction;
            }

            // 4. 核心保底：优先 Front，若 Front 缺失则退到 Back
            return entry.frontAction ?? entry.backAction;
        }

        private ActionConfigAsset GetLegacyActionFallback(HitReactionType type)
        {
            return type switch
            {
                HitReactionType.Light => hitAnimLight,
                HitReactionType.Heavy => hitAnimHeavy,
                HitReactionType.Launch => hitAnimKnowAway,
                HitReactionType.Shake => hitAnimShake,
                HitReactionType.Stay => hitAnimStay,
                HitReactionType.KnockDown => hitAnimKnockDown,
                HitReactionType.Parried => hitAnimParry,
                _ => null
            };
        }

        /// <summary>
        /// 兼容旧版调用（默认 0 度正面）
        /// </summary>
        public ActionConfigAsset GetHitAction(HitReactionType type)
        {
            if (TryResolveHitAction(type, 0f, 0f, out var action, out _))
            {
                return action;
            }
            return null;
        }

        /// <summary>
        /// 便捷带方向查询重载
        /// </summary>
        public ActionConfigAsset GetHitAction(HitReactionType type, float signedHorizontalAngle, float verticalAngle = 0f)
        {
            if (TryResolveHitAction(type, signedHorizontalAngle, verticalAngle, out var action, out _))
            {
                return action;
            }
            return null;
        }

        public IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            var yielded = new HashSet<ActionConfigAsset>();

            if (reactionEntries != null)
            {
                foreach (var entry in reactionEntries)
                {
                    if (entry == null) continue;
                    foreach (var act in entry.GetAllActions())
                    {
                        if (act != null && yielded.Add(act))
                        {
                            yield return act;
                        }
                    }
                }
            }

            // 兼容旧字段
            if (hitAnimLight != null && yielded.Add(hitAnimLight)) yield return hitAnimLight;
            if (hitAnimHeavy != null && yielded.Add(hitAnimHeavy)) yield return hitAnimHeavy;
            if (hitAnimKnowAway != null && yielded.Add(hitAnimKnowAway)) yield return hitAnimKnowAway;
            if (hitAnimShake != null && yielded.Add(hitAnimShake)) yield return hitAnimShake;
            if (hitAnimStay != null && yielded.Add(hitAnimStay)) yield return hitAnimStay;
            if (hitAnimKnockDown != null && yielded.Add(hitAnimKnockDown)) yield return hitAnimKnockDown;
            if (hitAnimParry != null && yielded.Add(hitAnimParry)) yield return hitAnimParry;
        }
    }
}
