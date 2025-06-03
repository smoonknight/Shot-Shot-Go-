using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class PlayableCharacterControllerBase : CharacterControllerBase, IDamageable
{
    [SerializeField]
    private List<InitialMagicSwordProperty> initialMagicSwordProperties;
    [ReadOnly]
    protected List<MagicSwordItemController> magicSwordItemControllers = new();

    protected const float attackInterval = 0.2f;

    protected int health;

    protected override void Awake()
    {
        base.Awake();
        SetupInitialMagicSwordProperties();
    }

    void SetupInitialMagicSwordProperties()
    {
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
        magicSword.Initialize(this, IsPlayer(), transform.position);
        magicSwordItemControllers.Add(magicSword);
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

    public void TakeDamage(int damage)
    {
        health -= damage;
    }

    public abstract bool IsPlayer();
}

[System.Serializable]
public struct InitialMagicSwordProperty
{
    public MagicSwordItemType type;
    public int amount;
}