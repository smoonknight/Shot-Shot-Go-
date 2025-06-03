using System;
using System.Threading;
using UnityEngine;

public abstract class WeaponItemController<T> : ItemController<T> where T : ItemBase
{
    protected bool isAttacking;
    protected PlayableCharacterControllerBase playableCharacter;
    protected Transform master;
    protected bool isPlayerAsMaster;

    protected CancellationTokenSource attackingCancellationTokenSource;

    public virtual void Initialize(PlayableCharacterControllerBase playableCharacterControllerBase, bool isPlayerAsMaster)
    {
        playableCharacter = playableCharacterControllerBase;
        master = playableCharacterControllerBase.transform;
        this.isPlayerAsMaster = isPlayerAsMaster;
        transform.position = master.position;
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