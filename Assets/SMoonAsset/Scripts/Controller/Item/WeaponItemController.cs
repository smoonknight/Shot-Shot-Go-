using System;
using UnityEngine;

public abstract class WeaponItemController<T> : ItemController<T> where T : ItemBase
{
    protected bool isAttacking;
    protected PlayableCharacterControllerBase playableCharacter;
    protected Transform master;
    protected bool isPlayerAsMaster;

    public virtual void Initialize(PlayableCharacterControllerBase playableCharacterControllerBase, bool isPlayerAsMaster)
    {
        this.playableCharacter = playableCharacterControllerBase;
        master = playableCharacterControllerBase.transform;
        this.isPlayerAsMaster = isPlayerAsMaster;
    }

    private void OnEnable()
    {
        isAttacking = false;
    }

    public bool TryAttackAction()
    {
        if (!EnabledAttack())
        {
            return false;
        }
        AttackAction();
        return true;
    }

    public bool EnabledAttack() => !isAttacking;

    public abstract void AttackAction();
}