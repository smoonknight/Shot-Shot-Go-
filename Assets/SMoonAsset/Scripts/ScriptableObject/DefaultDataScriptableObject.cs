using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Default Item", menuName = "")]
public class DefaultDataScriptableObject : ScriptableObject
{
    public List<DefaultItem<MagicSwordItem>> magicSwordDefaultItems;
    public StatProperty defaultCharacterStatProperty;
    public TypeStatPropertyCollector<MagicSwordItemType> defaultMagicSwordItemTypeStatPropertyCollector;
    public TypeStatPropertyCollector<EnemyType> enemyTypeStatPropertyCollector;
}

[System.Serializable]
public class DefaultItem<T> where T : ItemBase
{
    public Sprite sprite;
    public T itemBase;
}

