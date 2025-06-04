using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class PlayableCharacterControllerBase : CharacterControllerBase, IDamageable, IOutOfBoundable
{
    [SerializeField]
    private List<InitialMagicSwordProperty> initialMagicSwordProperties;
    [ReadOnly]
    [SerializeField]
    protected UpgradeProperty characterUpgradeProperty;
    [SerializeField]
    protected TypeUpgradePropertyCollector<MagicSwordItemType> magicSwordTypeUpgradePropertyCollector;

    [ReadOnly]
    protected List<MagicSwordItemController> magicSwordItemControllers = new();

    protected Vector3 initialScale;

    protected bool isGrounded;
    protected float coyoteCounter;
    protected float jumpBufferCounter;

    protected bool isProcessTrampoline;

    protected override void Awake()
    {
        base.Awake();
        SetInitial();
        SetupPlayable();
    }

    protected virtual void OnEnable()
    {
        isProcessTrampoline = false;
    }

    public void SetInitial()
    {
        initialScale = transform.localScale;
    }

    public virtual void SetupPlayable()
    {
        isDead = false;
        ColorChange(Color.white);
        AlphaChange(1);
        magicSwordTypeUpgradePropertyCollector.SetTypeUpgradeProperties(GameManager.Instance.GetCopyOfDefaultMagicSwordTypeUpgradeProperties());
        UpdateCharacterUpgrade(GetCharacterUpgradeProperty());
        SetupInitialMagicSwordProperties();
    }

    void UpdateCharacterUpgrade(UpgradeProperty upgradeProperty)
    {
        characterUpgradeProperty = upgradeProperty;
        jumpForce = upgradeProperty.jump;
        moveSpeed = upgradeProperty.speed;
    }

    void SetupInitialMagicSwordProperties()
    {
        magicSwordItemControllers.ForEach(magicSwordItemController =>
        {
            if (magicSwordItemController != null)
            {
                Destroy(magicSwordItemController.gameObject);
            }
        });
        magicSwordItemControllers.Clear();
        initialMagicSwordProperties.ForEach(SetupInitialMagicSwordProperty);
    }

    void SetupInitialMagicSwordProperty(InitialMagicSwordProperty initialMagicSwordProperty)
    {
        for (int i = 0; i < initialMagicSwordProperty.amount; i++)
        {
            var magicSword = MagicSwordSpawnerManager.Instance.GetSpawned(initialMagicSwordProperty.type);
            AddMagicSword(magicSword);
        }
    }

    public void AddMagicSword(MagicSwordItemController magicSword)
    {
        magicSword.Initialize(this, IsPlayer(), transform.position, magicSwordTypeUpgradePropertyCollector.GetUpgradeProperty(magicSword.itemBase.type).upgradeProperty);
        magicSwordItemControllers.Add(magicSword);
    }

    protected void TakeJump()
    {
        jumpBufferCounter = jumpBufferTime;
    }

    protected void Jump()
    {
        rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocity.x, jumpForce);
        coyoteCounter = 0;
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

        jumpForce = characterUpgradeProperty.jump;
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
                component.TrampolineTakeDamage(characterUpgradeProperty.damage);
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
            selectedMagicSword.AttackAction();
        }
    }

    public void ModifierUpgradeProperty(float multiplier)
    {
        characterUpgradeProperty.Multiplier(multiplier);
        UpdateCharacterUpgrade(characterUpgradeProperty);
    }

    public virtual bool EnableTakeDamage() => !isDead;
    public virtual void TakeDamage(int damage)
    {
        characterUpgradeProperty.health -= damage;
        if (characterUpgradeProperty.health <= 0 && !isDead)
        {
            isDead = true;
            OnZeroHealth();
        }
    }

    public virtual void OutOfBoundChangeLocation()
    {
        TakeDamage(10);
        Vector3 location = GameManager.Instance.GetOutOfBoundByGameMode();
        transform.position = location;
        rigidBody.linearVelocity = Vector3.zero;
    }

    public abstract bool IsPlayer();
    public abstract bool HoldingJump();
    public abstract void OnZeroHealth();
    public abstract UpgradeProperty GetCharacterUpgradeProperty();
    public abstract bool CheckOutOfBound();
}

[System.Serializable]
public struct InitialMagicSwordProperty
{
    public MagicSwordItemType type;
    public int amount;
}