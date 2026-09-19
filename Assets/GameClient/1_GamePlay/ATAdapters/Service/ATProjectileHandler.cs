using ATEditor;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    public class ATProjectileHandler : MonoBehaviour, IProjectileHandler
    {
        protected ISpawnHandler handler;
        protected SpawnData spawnData;
        protected TimeClock ownerClock;

        [Header("Lifecycle")]
        [Tooltip("最大存活时间(秒)，<=0 则不自动回收")]
        public float maxLifeTime = 10f;
        protected float lifeTimer = 0f;

        public virtual void Initialize(SpawnData data, ISpawnHandler handler)
        {
            Initialize(data, handler, null);
        }

        public virtual void Initialize(SpawnData data, ISpawnHandler handler, TimeClock clock)
        {
            this.handler = handler;
            this.spawnData = data;
            this.ownerClock = clock;
            this.lifeTimer = 0f;
        }

        protected virtual void Update()
        {
            if (maxLifeTime > 0)
            {
                float dt = ownerClock != null ? ownerClock.DeltaTime : Time.deltaTime;
                lifeTimer += dt;
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