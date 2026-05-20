using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua.Cast;
namespace Game.Logic
{
[Serializable]
public class AnimUnitConfig
{
    public AnimationClip clip;
    [Range(0,1)]
    public float fadeDuration;
}
}