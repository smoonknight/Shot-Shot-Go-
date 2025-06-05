using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : SingletonWithDontDestroyOnLoad<GameManager>
{
    public GameModeType currentGameModeType;
    public DefaultDataScriptableObject defaultItem;

    public DefaultItem<MagicSwordItem> GetDefaultItem(MagicSwordItemType type) => defaultItem.magicSwordDefaultItems.Find(match => match.itemBase.type == type);
    public Sprite GetDefaultItemSprite(MagicSwordItemType type) => GetDefaultItem(type).sprite;

    public StatProperty GetCopyOfDefaultCharacterUpgradeProperty() => defaultItem.defaultCharacterStatProperty.Copy();
    public List<TypeStatProperty<MagicSwordItemType>> GetCopyOfDefaultMagicSwordTypeUpgradeProperties() => defaultItem.defaultMagicSwordItemTypeStatPropertyCollector.GetCopyOfTypeUpgradeProperties().ToList();

    public TypeStatProperty<EnemyType> GetCopyOfDefaultEnemyCharacterUpgradeProperty(EnemyType type) => defaultItem.enemyTypeStatPropertyCollector.GetUpgradeProperty(type).Copy();

    public Vector3 GetOutOfBoundByGameMode()
    {
        return currentGameModeType switch
        {
            GameModeType.Normal => LevelManager.Instance.latestCheckpoint,
            GameModeType.Rogue => RogueManager.Instance.GetSampleSpawnPosition(),
            _ => throw new NotImplementedException(),
        };
    }

    public void RaiseGameOver()
    {
        Debug.Log("Game Over");
    }
}

public enum GameModeType
{
    Normal, Rogue,
}