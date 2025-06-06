using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using UnityEngine;
using UnityEngine.Events;

public abstract class PlayableCharacterControllerBase : CharacterControllerBase, IDamageable, IOutOfBoundable
{
    [SerializeField]
    private List<InitialMagicSwordProperty> initialMagicSwordProperties;
    [ReadOnly]
    [SerializeField]
    protected StatProperty characterStatProperty;
    [SerializeField]
    protected TypeStatPropertyCollector<MagicSwordItemType> magicSwordTypeStatPropertyCollector;
    [SerializeField]
    protected AudioClipSamples damagedAudioClipSamples;
    [SerializeField]
    protected AudioClipSamples attackAudioClipSamples;

    [ReadOnly]
    protected List<MagicSwordItemController> magicSwordItemControllers = new();

    protected bool isImmuneDamage;

    protected bool isProcessTrampoline;

    const int HeartRate = 10;

    public int Health { get; protected set; }
    public int MaximumHealth => characterStatProperty.health;

    private CancellationTokenSource immuneCancellationTokenSource;

    protected override void Awake()
    {
        base.Awake();
        SetupPlayable();
    }

    protected virtual void OnEnable()
    {
        isProcessTrampoline = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        immuneCancellationTokenSource?.Cancel();
    }

    public virtual void SetupPlayable()
    {
        IsDead = false;
        ColorChange(Color.white);
        AlphaChange(1);
        magicSwordTypeStatPropertyCollector.SetTypeUpgradeProperties(GameManager.Instance.GetCopyOfDefaultMagicSwordTypeUpgradeProperties());
        SetCharacterStatProperty(GetCharacterUpgradeProperty());
        SetupInitialMagicSwordProperties();
    }

    void SetCharacterStatProperty(StatProperty upgradeProperty)
    {
        characterStatProperty = upgradeProperty;
        jumpForce = upgradeProperty.jump;
        moveSpeed = upgradeProperty.speed;
        Health = upgradeProperty.health;
        OnSetCharacterStatProperty(upgradeProperty);
    }

    public void UpgradeStatProperty(UpgradeType upgradeType, UpgradeStat upgradeStat)
    {
        switch (upgradeType)
        {
            case UpgradeType.Character:
                UpgradeCharacterStatProperty(upgradeStat);
                break;
            case UpgradeType.MagicSword_Common:
            case UpgradeType.MagicSword_OnTarget:
            case UpgradeType.MagicSword_Slashing:
                UpgradeWeaponStatPorperty(upgradeType, upgradeStat);
                break;
            default: throw new NotImplementedException();
        }
    }

    void UpgradeCharacterStatProperty(UpgradeStat upgradeStat)
    {
        switch (upgradeStat.type)
        {
            case UpgradeStatType.size:
                characterStatProperty.size += upgradeStat.value;
                break;
            case UpgradeStatType.attackInterval:
                characterStatProperty.attackInterval += upgradeStat.value;
                break;
            case UpgradeStatType.speed:
                characterStatProperty.speed += upgradeStat.value;
                break;
            case UpgradeStatType.health:
                characterStatProperty.health += Mathf.RoundToInt(upgradeStat.value);
                break;
            case UpgradeStatType.damage:
                characterStatProperty.damage += Mathf.RoundToInt(upgradeStat.value);
                break;
            case UpgradeStatType.jump:
                characterStatProperty.jump += upgradeStat.value;
                break;
        }

        OnUpgradeCharacterStatProperty(upgradeStat);

        SetCharacterStatProperty(characterStatProperty);
    }

    void UpgradeWeaponStatPorperty(UpgradeType upgradeType, UpgradeStat upgradeStat)
    {
        MagicSwordItemType type = upgradeType switch
        {
            UpgradeType.MagicSword_Common => MagicSwordItemType.Common,
            UpgradeType.MagicSword_OnTarget => MagicSwordItemType.OnTarget,
            UpgradeType.MagicSword_Slashing => MagicSwordItemType.Slashing,
            _ => throw new NotImplementedException(),
        };

        UpgradeMagicSwordItemTypeProperty(magicSwordTypeStatPropertyCollector.GetStatProperty(type), upgradeStat);
    }

    void UpgradeMagicSwordItemTypeProperty(TypeStatProperty<MagicSwordItemType> magicSwordItemTypeStatProperty, UpgradeStat upgradeStat)
    {
        switch (upgradeStat.type)
        {
            case UpgradeStatType.size:
                magicSwordItemTypeStatProperty.statProperty.size += upgradeStat.value;
                break;
            case UpgradeStatType.attackInterval:
                magicSwordItemTypeStatProperty.statProperty.attackInterval += upgradeStat.value;
                break;
            case UpgradeStatType.speed:
                magicSwordItemTypeStatProperty.statProperty.speed += upgradeStat.value;
                break;
            case UpgradeStatType.damage:
                magicSwordItemTypeStatProperty.statProperty.damage += Mathf.RoundToInt(upgradeStat.value);
                break;
            case UpgradeStatType.jump:
                magicSwordItemTypeStatProperty.statProperty.jump += upgradeStat.value;
                break;
            case UpgradeStatType.quantity:
                for (int i = 0; i < upgradeStat.value; i++)
                {
                    var magicSword = MagicSwordSpawnerManager.Instance.GetSpawned(magicSwordItemTypeStatProperty.type);
                    AddMagicSword(magicSword);
                }
                break;
        }
    }

    void SetupInitialMagicSwordProperties()
    {
        magicSwordItemControllers.ForEach(magicSwordItemController =>
        {
            if (magicSwordItemController != null)
            {
                magicSwordItemController.gameObject.SetActive(false);
            }
        });
        magicSwordItemControllers.Clear();
        initialMagicSwordProperties.ForEach(SetupInitialMagicSwordProperty);
    }

    void SetupInitialMagicSwordProperty(InitialMagicSwordProperty initialMagicSwordProperty)
    {
        for (int i = 0; i < initialMagicSwordProperty.amount; i++)
        {
            AddAndGetMagicSword(initialMagicSwordProperty.type);
        }
    }

    public MagicSwordItemController AddAndGetMagicSword(MagicSwordItemType type)
    {
        var magicSword = MagicSwordSpawnerManager.Instance.GetSpawned(type);
        AddMagicSword(magicSword);
        return magicSword;
    }

    public void AddMagicSword(MagicSwordItemController magicSword)
    {
        magicSword.Initialize(this, IsPlayer(), transform.position, magicSwordTypeStatPropertyCollector.GetStatProperty(magicSword.itemBase.type).statProperty);
        magicSwordItemControllers.Add(magicSword);
    }

    protected void TakeJump()
    {
        jumpBufferCounter = jumpBufferTime;
    }


    protected void ApplyBetterJump()
    {
        if (rigidBody.linearVelocity.y < 0)
            rigidBody.linearVelocityY += fallMultiplier * Physics2D.gravity.y * Time.deltaTime;
        else if (rigidBody.linearVelocity.y > 0 && !HoldingJump())
            rigidBody.linearVelocityY += lowJumpMultiplier * Physics2D.gravity.y * Time.deltaTime;
    }

    protected void ValidateJump()
    {
        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            Jump();
            jumpBufferCounter = 0;
        }
    }

    private async void Trampoline(float TrampolineValue)
    {
        await UniTask.Yield();
        jumpForce = TrampolineValue;
        TakeJump();

        await UniTask.WaitUntil(() => rigidBody.linearVelocity.y < 0);
        await UniTask.WaitUntil(() => isGrounded);

        jumpForce = characterStatProperty.jump;
        isProcessTrampoline = false;
    }

    public bool CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundRadius, LayerMaskManager.Instance.groundableLayer);
        if (hit.collider?.gameObject.layer == gameObject.layer)
        {
            return hit.collider != null;
        }
        if (!isProcessTrampoline && hit.collider != null && hit.collider.TryGetComponent(out ITrampolineable component))
        {
            isProcessTrampoline = true;
            component.OnTakeTrampoline();
            Trampoline(component.JumpValue());
            if (component.IsDamaging())
            {
                component.TrampolineTakeDamage(characterStatProperty.damage);
            }
        }
        return hit.collider != null;
    }

    public void Fire()
    {
        var enableAttackMagicSwords = magicSwordItemControllers.FindAll(magicSwordItemController => magicSwordItemController.EnabledAttack());
        if (enableAttackMagicSwords.Count == 0)
        {
            return;
        }
        var selectedMagicSword = enableAttackMagicSwords.GetRandom();
        if (selectedMagicSword != null)
        {
            if (!selectedMagicSword.IsOwner(this))
            {
                magicSwordItemControllers.Remove(selectedMagicSword);
                selectedMagicSword = AddAndGetMagicSword(selectedMagicSword.itemBase.type);
            }
            selectedMagicSword.AttackAction();
            if (attackAudioClipSamples.IsHaveSample())
                audioSource.PlayOneShot(attackAudioClipSamples.GetClip());
        }
    }

    public void ModifierUpgradeProperty(float multiplier)
    {
        characterStatProperty.Multiplier(multiplier);
        SetCharacterStatProperty(characterStatProperty);
    }

    protected async void SetImmune(float duration, UnityAction onImmuneUpdateAction = null)
    {
        CancellationToken cancellationToken = immuneCancellationTokenSource.ResetToken();

        isImmuneDamage = true;
        await UniTask.Yield();

        float time = 0;
        while (time < duration)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            onImmuneUpdateAction?.Invoke();
            time += Time.deltaTime;
            await UniTask.Yield();
        }

        isImmuneDamage = false;
    }

    public virtual bool EnableTakeDamage() => !IsDead && !isImmuneDamage;
    public virtual void TakeDamage(int damage)
    {
        Health -= damage;
        if (Health <= 0 && !IsDead)
        {
            IsDead = true;
            OnZeroHealth();
        }
        else
        {
            DamagedAudio();
        }
    }
    public virtual void TakeDamage(int damage, Vector2 sourcePosition)
    {
        TakeDamage(damage);
    }

    public void ValidateTakeDamage(int damage)
    {
        if (EnableTakeDamage())
        {
            TakeDamage(damage);
        }
    }

    public void ValidateTakeDamage(int damage, Vector2 sourcePosition)
    {
        if (EnableTakeDamage())
        {
            TakeDamage(damage, sourcePosition);
        }
    }

    public virtual void OutOfBoundChangeLocation()
    {
        TakeDamage(10);
        Vector3 location = GameplayManager.Instance.GetOutOfBoundByGameMode();
        ForceChangePosition(location);
    }

    public void DamagedAudio()
    {
        if (!damagedAudioClipSamples.IsHaveSample())
            return;
        audioSource.clip = damagedAudioClipSamples.GetClip();
        audioSource.Play();
    }

    public abstract bool IsPlayer();
    public abstract bool HoldingJump();
    public abstract void OnZeroHealth();
    public abstract StatProperty GetCharacterUpgradeProperty();
    public abstract bool CheckOutOfBound();
    public abstract void OnSetCharacterStatProperty(StatProperty upgradeProperty);
    public abstract void OnUpgradeCharacterStatProperty(UpgradeStat upgradeStat);
}

[System.Serializable]
public struct InitialMagicSwordProperty
{
    public MagicSwordItemType type;
    public int amount;
}