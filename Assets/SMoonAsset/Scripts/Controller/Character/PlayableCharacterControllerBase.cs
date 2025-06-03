using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class PlayableCharacterControllerBase : CharacterControllerBase, IDamageable
{
    [SerializeField]
    private List<InitialMagicSwordProperty> initialMagicSwordProperties;
    [ReadOnly]
    protected List<MagicSwordItemController> magicSwordItemControllers = new();



    protected int health;

    protected override void Awake()
    {
        base.Awake();
        SetupInitialMagicSwordProperties();
        magicSwordItemControllers.ForEach(magicSwordItemController => magicSwordItemController.Initialize(this, IsPlayer()));

    }

    void SetupInitialMagicSwordProperties()
    {
        initialMagicSwordProperties.ForEach(SetupInitialMagicSwordProperty);
    }

    void SetupInitialMagicSwordProperty(InitialMagicSwordProperty initialMagicSwordProperty)
    {
        for (int i = 0; i < initialMagicSwordProperty.amount; i++)
        {
            var magicSword = MagicSwordSpawnerManager.Instance.spawner.GetSpawned(initialMagicSwordProperty.type);
            magicSwordItemControllers.Add(magicSword);
        }
    }

    public void Fire()
    {
        var selectedMagicSword = magicSwordItemControllers.FirstOrDefault(magicSwordItemController => magicSwordItemController.EnabledAttack());
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