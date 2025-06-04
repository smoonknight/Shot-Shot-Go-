using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Default Item", menuName = "")]
public class DefaultDataScriptableObject : ScriptableObject
{
    public List<DefaultItem<MagicSwordItem>> magicSwordDefaultItems;
    public UpgradeProperty defaultCharacterUpgradeProperty;
    public TypeUpgradePropertyCollector<MagicSwordItemType> defaultMagicSwordItemTypeUpgradePropertyCollector;
    public TypeUpgradePropertyCollector<EnemyType> enemyTypeUpgradePropertyCollector;
}

[System.Serializable]
public class DefaultItem<T> where T : ItemBase
{
    public Sprite sprite;
    public T itemBase;
}

