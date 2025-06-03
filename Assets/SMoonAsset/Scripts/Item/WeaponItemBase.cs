using System;

[Serializable]
public abstract class WeaponItemBase<T> : ItemBase where T : Enum
{
    public T type;
    public float damage;

}