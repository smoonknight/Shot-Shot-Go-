using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StatProperty
{
    public float size;
    public float attackInterval;
    public float speed;
    public int health;
    public int damage;
    public float jump;
    public int exp;

    const float sizeMultiplierRate = 0.1f;
    const float speedMultiplierRate = 0.25f;
    const float healthMultiplierRate = 3.5f;
    const float damageMultiplierRate = 1.1f;
    const float jumpMultiplierRate = 0.05f;
    const float expMultiplierRate = 1.5f;

    public StatProperty Copy() => new()
    {
        size = size,
        attackInterval = attackInterval,
        speed = speed,
        health = health,
        damage = damage,
        jump = jump,
        exp = exp,
    };

    public void Multiplier(float value)
    {
        size += value * sizeMultiplierRate;
        speed += value * speedMultiplierRate;
        health = Mathf.CeilToInt(health + (value * healthMultiplierRate));
        damage = Mathf.CeilToInt(damage + (value * damageMultiplierRate));
        jump += value * jumpMultiplierRate;
        exp = Mathf.CeilToInt(exp * value * expMultiplierRate);
    }
}

[Serializable]
public class TypeStatProperty<T> where T : Enum
{
    public T type;
    public StatProperty upgradeProperty;

    public TypeStatProperty<T> Copy() => new()
    {
        type = type,
        upgradeProperty = upgradeProperty.Copy()
    };
}

[Serializable]
public class TypeStatPropertyCollector<T> where T : Enum
{
    public List<TypeStatProperty<T>> typeUpgradeProperties;
    public TypeStatProperty<T> GetUpgradeProperty(T type) => typeUpgradeProperties.Find(weaponUpgradeProperty => weaponUpgradeProperty.type.Equals(type));
    public IEnumerable<TypeStatProperty<T>> GetCopyOfTypeUpgradeProperties() => typeUpgradeProperties.Select(typeUpgradeProperty => typeUpgradeProperty.Copy());

    public void SetTypeUpgradeProperties(List<TypeStatProperty<T>> typeUpgradeProperties) => this.typeUpgradeProperties = typeUpgradeProperties;
}