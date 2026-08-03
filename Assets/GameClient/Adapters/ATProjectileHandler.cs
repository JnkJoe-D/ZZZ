using System.Collections;
using System.Collections.Generic;
using ATEditor;
using UnityEngine;
namespace Game.Adapters{
public class ATProjectileHandler : MonoBehaviour,IProjectileHandler
{
    protected ISpawnHandler handler;
    protected SpawnData spawnData;

    [Header("Lifecycle")]
    [Tooltip("最大存活时间(秒)，<=0 则不自动回收")]
    public float maxLifeTime = 10f;
    protected float lifeTimer = 0f;

    public virtual void Initialize(SpawnData data, ISpawnHandler handler)
    {
        this.handler = handler;
        this.spawnData = data;
        this.lifeTimer = 0f;
    }

    protected virtual void Update()
    {
        if (maxLifeTime > 0)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= maxLifeTime)
            {
                Recycle();
            }
        }
    }

    public virtual void Terminate()
    {
        
    }

    public void Recycle()
    {
        Terminate();
        
        if (handler != null)
        {
            handler.DestroySpawnedObject(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
}