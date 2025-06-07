using System.Collections.Generic;
using System.Linq;
using SMoonUniversalAsset;
using UnityEngine;

public partial class GameManager : SingletonWithDontDestroyOnLoad<GameManager>
{
    public DefaultDataScriptableObject defaultItem;

    public DefaultItem<MagicSwordItem> GetDefaultItem(MagicSwordItemType type) => defaultItem.magicSwordDefaultItems.Find(match => match.itemBase.type == type);
    public Sprite GetDefaultItemSprite(MagicSwordItemType type) => GetDefaultItem(type).sprite;

    public StatProperty GetCopyOfDefaultCharacterUpgradeProperty() => defaultItem.defaultCharacterStatProperty.Copy();
    public List<TypeStatProperty<MagicSwordItemType>> GetCopyOfDefaultMagicSwordTypeUpgradeProperties() => defaultItem.defaultMagicSwordItemTypeStatPropertyCollector.GetCopyOfTypeUpgradeProperties().ToList();

    public TypeStatProperty<EnemyType> GetCopyOfDefaultEnemyCharacterUpgradeProperty(EnemyType type) => defaultItem.enemyTypeStatPropertyCollector.GetStatProperty(type).Copy();
}

public enum GameModeType
{
    Normal, Rogue, MainMenu
}