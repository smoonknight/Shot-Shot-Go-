
using UnityEngine;

public interface IDamageable
{
    public bool EnableTakeDamage();
    public void TakeDamage(int damage);
    public void TakeDamage(int damage, Vector2 sourcePosition);
}