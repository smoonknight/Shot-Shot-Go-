using System;
using SMoonUniversalAsset;
using UnityEngine;

public class MagicSwordDroppedItemSpawnerManager : MultiSpawnerManager<MagicSwordDroppedItemSpawner, MagicSwordDroppedItemController, MagicSwordItemType>
{

}

[Serializable]
public class MagicSwordDroppedItemSpawner : MultiSpawnerBase<MagicSwordDroppedItemController, MagicSwordItemType>
{
    public override void OnSpawn(MagicSwordDroppedItemController component, MagicSwordItemType type, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
    {

    }
}