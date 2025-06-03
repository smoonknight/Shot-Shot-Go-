using UnityEngine;

[System.Serializable]
public abstract class ItemBase
{
    public abstract Sprite GetSprite();
    public int quantity;
}