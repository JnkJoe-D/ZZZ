using System;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 命中效果 ID (HitEffectId) 属性标记。
    /// 配合 Editor Drawer 读取 zzz_tbhiteffect.json 配表，并在 Inspector 中以包含名称的下拉列表形式渲染。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HitEffectIdAttribute : PropertyAttribute
    {
    }
}
