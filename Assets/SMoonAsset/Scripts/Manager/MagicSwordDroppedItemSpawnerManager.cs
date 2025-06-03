using System;
using SMoonUniversalAsset;
using UnityEngine;

public class MagicSwordDroppedItemSpawnerManager : SpawnerManager<MagicSwordDroppedItemSpawner, MagicSwordDroppedItemController, MagicSwordItemType>
{

}

[Serializable]
public class MagicSwordDroppedItemSpawner : SpawnerBase<MagicSwordDroppedItemController, MagicSwordItemType>
{
    public override void OnSpawn(MagicSwordDroppedItemController component, MagicSwordItemType type, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
    {

    }
}