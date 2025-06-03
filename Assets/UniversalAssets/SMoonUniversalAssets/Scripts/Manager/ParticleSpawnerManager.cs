using System;
using UnityEngine;
using UnityEngine.Events;

namespace SMoonUniversalAsset
{
    public class ParticleSpawnerManager : SingletonWithDontDestroyOnLoad<ParticleSpawnerManager>
    {
        [SerializeField] private ParticleSpawner particleSpawner;

        protected override void Awake()
        {
            base.Awake();
            particleSpawner.Initialize();
        }

        public void AddParticle(ParticleType type, Vector3 position, Quaternion? rotation = null, Func<Vector3> onActivePositionUpdate = null)
        {
            particleSpawner.GetSpawned(type, position, rotation, onActivePositionUpdate);
        }
    }

    [System.Serializable]
    public class ParticleSpawner : SpawnerBase<ParticleSystem, ParticleType>
    {
        public override void OnSpawn(ParticleSystem component, Func<Vector3> onSetDeactiveOnDurationUpdate)
        {
            SetDeactiveOnDuration(component, component.main.duration + component.main.startLifetime.constantMax, onSetDeactiveOnDurationUpdate);
        }
    }
}

public enum ParticleType
{
    ImpactGroundHit
}