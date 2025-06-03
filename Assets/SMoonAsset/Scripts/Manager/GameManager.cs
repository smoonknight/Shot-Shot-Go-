using UnityEngine;

public class GameManager : SingletonWithDontDestroyOnLoad<GameManager>
{
    public DefaultItemScriptableObject defaultItem;

    public DefaultItem<MagicSwordItem> GetDefaultItem(MagicSwordItemType type) => defaultItem.magicSwordDefaultItems.Find(match => match.itemBase.type == type);
    public Sprite GetDefaultItemSprite(MagicSwordItemType type) => GetDefaultItem(type).sprite;
}