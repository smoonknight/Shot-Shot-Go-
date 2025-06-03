using System;
using System.Collections.Generic;
using System.Linq;
using SMoonUniversalAsset;
using UnityEngine;

public class MagicSwordSpawnerManager : SpawnerManager<MagicSwordSpawner, MagicSwordItemController, MagicSwordItemType>
{

}

[Serializable]
public class MagicSwordSpawner : SpawnerBase<MagicSwordItemController, MagicSwordItemType>
{
    public override void OnSpawn(MagicSwordItemController component, MagicSwordItemType type, Func<Vector3> onActivePositionUpdate = null)
    {
        var selectedMagicSwordItem = GameManager.Instance.GetDefaultItem(type);
        if (selectedMagicSwordItem != null)
        {
            component.SetItemBase(selectedMagicSwordItem.itemBase);
        }
    }
}