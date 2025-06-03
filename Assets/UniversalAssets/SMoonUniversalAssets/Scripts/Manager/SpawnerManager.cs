using SMoonUniversalAsset;
using UnityEngine;

public class SpawnerManager<T> : SingletonWithDontDestroyOnLoad<SpawnerManager<T>> where T : SpawnerBase<MagicSwordItemController, MagicSwordItemType>
{
    public T spawner;

    protected override void Awake()
    {
        base.Awake();
        spawner.Initialize();
    }
}