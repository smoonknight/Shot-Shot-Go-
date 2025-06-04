using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : SingletonWithDontDestroyOnLoad<GameManager>
{
    public DefaultDataScriptableObject defaultItem;

    public DefaultItem<MagicSwordItem> GetDefaultItem(MagicSwordItemType type) => defaultItem.magicSwordDefaultItems.Find(match => match.itemBase.type == type);
    public Sprite GetDefaultItemSprite(MagicSwordItemType type) => GetDefaultItem(type).sprite;

    public UpgradeProperty GetCopyOfDefaultCharacterUpgradeProperty() => defaultItem.defaultCharacterUpgradeProperty.Copy();
    public List<TypeUpgradeProperty<MagicSwordItemType>> GetCopyOfDefaultMagicSwordTypeUpgradeProperties() => defaultItem.defaultMagicSwordItemTypeUpgradePropertyCollector.GetCopyOfTypeUpgradeProperties().ToList();

    internal TypeUpgradeProperty<EnemyType> GetCopyOfDefaultEnemyCharacterUpgradeProperty(EnemyType type) => defaultItem.enemyTypeUpgradePropertyCollector.GetUpgradeProperty(type).Copy();
}