using System;
using SMoonUniversalAsset;
using UnityEngine;

public class SpawnerManager<T, G, C> : SingletonWithDontDestroyOnLoad<SpawnerManager<T, G, C>> where T : SpawnerBase<G, C> where G : Component where C : Enum
{
    [SerializeField]
    protected T spawner;

    public G GetSpawned(C type, Vector3? position = null, Quaternion? rotation = null, Func<Vector3> onSetDeactiveOnDurationUpdate = null) => spawner.GetSpawned(type, position, rotation, onSetDeactiveOnDurationUpdate);

    protected override void Awake()
    {
        base.Awake();
        spawner.Initialize();
    }

    public int GetActiveSpawn() => spawner.GetActiveSpawn();
}