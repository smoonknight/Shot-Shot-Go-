using System;
using System.Collections.Generic;
using System.Linq;
using SMoonUniversalAsset;
using UnityEngine;

public class MagicSwordSpawnerManager : SpawnerManager<MagicSwordSpawner>
{

}

[Serializable]
public class MagicSwordSpawner : SpawnerBase<MagicSwordItemController, MagicSwordItemType>
{
    public List<MagicSwordItem> defaultMagicSwordItems;
    public override void OnSpawn(MagicSwordItemController component, Func<Vector3> onActivePositionUpdate = null)
    {
        var selectedMagicSwordItem = defaultMagicSwordItems.FirstOrDefault(magicSwordItem => magicSwordItem.type == component.itemBase.type);
        if (selectedMagicSwordItem != null)
        {
            component.SetItemBase(selectedMagicSwordItem);
        }
    }
}