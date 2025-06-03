using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    public List<PointDataProperty<MagicSwordItemType>> magicSwordPointDataProperties;

    protected override void Awake()
    {
        base.Awake();

        magicSwordPointDataProperties.ForEach(SetupPointDataProperty);
    }

    private void SetupPointDataProperty(PointDataProperty<MagicSwordItemType> pointDataProperty)
    {
        MagicSwordDroppedItemSpawnerManager.Instance.GetSpawned(pointDataProperty.type, pointDataProperty.pointData.position, pointDataProperty.pointData.rotation);
    }
}