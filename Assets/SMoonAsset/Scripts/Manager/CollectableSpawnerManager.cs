using System;
using SMoonUniversalAsset;
using UnityEngine;

public class CollectableSpawnerManager : MultiSpawnerManager<CollectableSpawner, CollectableController, CollectableType>
{

}

[System.Serializable]
public class CollectableSpawner : MultiSpawnerBase<CollectableController, CollectableType>
{
    public override void OnSpawn(CollectableController component, CollectableType type, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
    {
        SetDeactiveOnDuration(component, 60);
    }
}

public enum CollectableType
{
    Coin, Heart
}