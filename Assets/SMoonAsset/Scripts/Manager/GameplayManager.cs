using System;
using System.Collections.Generic;
using UnityEngine;

public class GameplayManager : Singleton<GameplayManager>
{
    public GameModeType currentGameModeType;
    public RateCollector<CollectableType> collectableTypeRateCollector;
    public List<PlayerUpgradePlanPorperty> playerUpgradePlanPorperties;

    protected override void OnAwake()
    {
        base.OnAwake();
        collectableTypeRateCollector.Calculate();
    }

    public Vector3 GetOutOfBoundByGameMode()
    {
        return currentGameModeType switch
        {
            GameModeType.Normal => LevelManager.Instance.latestCheckpoint,
            GameModeType.Rogue => RogueManager.Instance.GetSampleSpawnPosition(),
            _ => throw new NotImplementedException(),
        };
    }

    public void DropCollectable(PlayableCharacterControllerBase playableCharacter)
    {
        var spawnedCollectable = CollectableSpawnerManager.Instance.GetSpawned(collectableTypeRateCollector.GetRandomData());
        spawnedCollectable.value = playableCharacter.GetCharacterUpgradeProperty().exp;
        spawnedCollectable.transform.position = playableCharacter.transform.position;
    }

    public List<List<PlayerUpgradePlanPorperty>> GetUpgradeStatsRandomly(int amount)
    {
        int count = playerUpgradePlanPorperties.Count;
        const int itemPerList = 3;

        if (amount <= 0 || count == 0)
            return new List<List<PlayerUpgradePlanPorperty>>();

        var rnd = new System.Random();
        var results = new List<List<PlayerUpgradePlanPorperty>>(amount);

        for (int n = 0; n < amount; n++)
        {
            var pool = new List<PlayerUpgradePlanPorperty>(playerUpgradePlanPorperties);
            int takeCount = Math.Min(itemPerList, pool.Count);

            // Partial Fisher-Yates shuffle untuk ambil 3 item acak unik dari pool
            for (int i = 0; i < takeCount; i++)
            {
                int j = rnd.Next(i, pool.Count);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            results.Add(pool.GetRange(0, takeCount));
        }

        return results;
    }


}

[Serializable]
public class PlayerUpgradePlanPorperty
{
    public UpgradeType type;
    public List<UpgradeStat> upgradeStats;
}

[Serializable]
public struct UpgradeStat
{
    public UpgradeStatType type;
    public float value;
}

public enum UpgradeStatType
{
    size,
    attackInterval,
    speed,
    health,
    damage,
    jump,
    quantity,
}

public enum UpgradeType
{
    Character,
    MagicSword_Common,
    MagicSword_OnTarget,
    MagicSword_Slashing
}