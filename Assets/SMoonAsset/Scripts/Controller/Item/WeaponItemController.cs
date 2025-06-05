using System;
using System.Threading;
using UnityEngine;

public abstract class WeaponItemController<T> : ItemController<T> where T : ItemBase
{
    protected bool isAttacking;
    protected PlayableCharacterControllerBase playableCharacter;
    protected Transform master;
    protected bool isPlayerAsMaster;
    protected StatProperty statProperty;

    protected CancellationTokenSource attackingCancellationTokenSource;

    protected LayerMask targetMask;

    public virtual void Initialize(PlayableCharacterControllerBase playableCharacterControllerBase, bool isPlayerAsMaster, Vector3 initialPosition, StatProperty statProperty)
    {
        playableCharacter = playableCharacterControllerBase;
        master = playableCharacterControllerBase.transform;
        transform.position = initialPosition;
        this.isPlayerAsMaster = isPlayerAsMaster;
        targetMask = isPlayerAsMaster ? LayerMaskManager.Instance.enemyMask : LayerMaskManager.Instance.playerMask;
        this.statProperty = statProperty;
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