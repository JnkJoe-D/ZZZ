using SkillEditor;
using UnityEngine;
namespace Game.Adapters
{
public class SkillHitHandler : ISkillHitHandler
{
	public void OnHitDetect(HitData damageData)
	{
        var colliders = damageData.targets;

        foreach (var c in colliders)
        {
            Debug.Log($"{c.gameObject.name}:<color=orange>Damage Triggered!</color>");
        }
	}
}
}