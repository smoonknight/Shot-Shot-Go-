using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Default Item", menuName = "")]
public class DefaultItemScriptableObject : ScriptableObject
{
    public List<DefaultItem<MagicSwordItem>> magicSwordDefaultItems;
}

[System.Serializable]
public class DefaultItem<T> where T : ItemBase
{
    public Sprite sprite;
    public T itemBase;
}