using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class UpgradeProperty
{
    public float size;
    public float moveSpeed;
    public float speed;
    public int health;
    public int damage;
    public float jump;
    public int exp;

    public UpgradeProperty Copy() => new()
    {
        size = size,
        moveSpeed = moveSpeed,
        speed = speed,
        health = health,
        damage = damage,
        jump = jump,
        exp = exp,
    };

    public void multiplier(float value)
    {
        size *= value;
        moveSpeed *= value;
        speed *= value;
        health = Mathf.CeilToInt(health * value);
        damage = Mathf.CeilToInt(damage * value);
        jump *= value;
        exp = Mathf.CeilToInt(exp * value);
    }
}

[Serializable]
public class TypeUpgradeProperty<T> where T : Enum
{
    public T type;
    public UpgradeProperty upgradeProperty;

    public TypeUpgradeProperty<T> Copy() => new()
    {
        type = type,
        upgradeProperty = upgradeProperty.Copy()
    };
}

[Serializable]
public class TypeUpgradePropertyCollector<T> where T : Enum
{
    public List<TypeUpgradeProperty<T>> typeUpgradeProperties;
    public TypeUpgradeProperty<T> GetUpgradeProperty(T type) => typeUpgradeProperties.Find(weaponUpgradeProperty => weaponUpgradeProperty.type.Equals(type));
    public IEnumerable<TypeUpgradeProperty<T>> GetCopyOfTypeUpgradeProperties() => typeUpgradeProperties.Select(typeUpgradeProperty => typeUpgradeProperty.Copy());

    public void SetTypeUpgradeProperties(List<TypeUpgradeProperty<T>> typeUpgradeProperties) => this.typeUpgradeProperties = typeUpgradeProperties;
}