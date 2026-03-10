using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillEditor;
using Game.Pool;
namespace Game.Adapters
{
public class SkillVFXHandler : ISkillVFXHandler
{
    private MonoBehaviour _mono;
    public SkillVFXHandler(MonoBehaviour mono)
    {
        _mono = mono;
    }
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return GlobalPoolManager.Spawn(prefab, position, rotation, parent);
    }

    public void Return(GameObject instance)
    {
        GlobalPoolManager.Return(instance);
    }
    public void ReturnVFXDelay(GameObject inst, float delay)
    {
        _mono.StartCoroutine(DelayReturn(inst, delay));
    }
    private IEnumerator DelayReturn(GameObject inst, float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);
        Return(inst);
    }
}
}