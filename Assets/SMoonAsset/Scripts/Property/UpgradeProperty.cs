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

    const float sizeMultiplierRate = 0.1f;
    const float moveSpeedMultiplierRate = 0.2f;
    const float speedMultiplierRate = 0.15f;
    const float healthMultiplierRate = 1.25f;
    const float damageMultiplierRate = 1.1f;
    const float jumpMultiplierRate = 0.05f;
    const float expMultiplierRate = 1.5f;

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

    public void Multiplier(float value)
    {
        size *= value * sizeMultiplierRate;
        moveSpeed *= value * moveSpeedMultiplierRate;
        speed *= value * speedMultiplierRate;
        health = Mathf.CeilToInt(health * value * healthMultiplierRate);
        damage = Mathf.CeilToInt(damage * value * damageMultiplierRate);
        jump *= value * jumpMultiplierRate;
        exp = Mathf.CeilToInt(exp * value * expMultiplierRate);
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