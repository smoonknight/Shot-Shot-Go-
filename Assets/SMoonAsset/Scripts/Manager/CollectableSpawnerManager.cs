using System;
using SMoonUniversalAsset;
using UnityEngine;

public class CollectableSpawnerManager : SpawnerManager<CollectableSpawner, CollectableController, CollectableType>
{

}

[System.Serializable]
public class CollectableSpawner : SpawnerBase<CollectableController, CollectableType>
{
    public override void OnSpawn(CollectableController component, CollectableType type, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
    {
        SetDeactiveOnDuration(component, 60);
    }
}

public enum CollectableType
{
    Coin
}