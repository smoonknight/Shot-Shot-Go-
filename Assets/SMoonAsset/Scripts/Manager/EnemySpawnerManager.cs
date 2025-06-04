using System;
using SMoonUniversalAsset;
using UnityEngine;

public class EnemySpawnerManager : SpawnerManager<EnemySpawner, EnemyController, EnemyType>
{

}

[System.Serializable]
public class EnemySpawner : SpawnerBase<EnemyController, EnemyType>
{
    public override void OnSpawn(EnemyController component, EnemyType type, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
    {
        component.SetupPlayable();
    }
}