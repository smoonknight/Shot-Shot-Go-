using System;
using System.Collections.Generic;
using System.Linq;
using SMoonUniversalAsset;
using UnityEngine;

public class MagicSwordSpawnerManager : MultiSpawnerManagerWithDDOL<MagicSwordSpawner, MagicSwordItemController, MagicSwordItemType>
{

}

[Serializable]
public class MagicSwordSpawner : MultiSpawnerBase<MagicSwordItemController, MagicSwordItemType>
{
    public override void OnSpawn(MagicSwordItemController component, MagicSwordItemType type, Func<Vector3> onActivePositionUpdate = null)
    {
        var selectedMagicSwordItem = GameManager.Instance.GetDefaultItem(type);
        if (selectedMagicSwordItem != null)
        {
            component.Reinitialize(selectedMagicSwordItem.itemBase);
        }
    }
}