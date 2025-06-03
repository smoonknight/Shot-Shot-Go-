using UnityEngine;

[System.Serializable]
public class MagicSwordItem : WeaponItemBase<MagicSwordItemType>
{
    public override Sprite GetSprite() => GameManager.Instance.GetDefaultItemSprite(type);
}

public enum MagicSwordItemType
{
    Common, OnTarget, Slashing

}