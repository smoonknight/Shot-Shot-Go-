using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public abstract class DroppedItemController<T> : TriggerBox2D where T : Enum
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    public T type;

    protected override void Awake()
    {
        base.Awake();
        SetLayerMask(LayerMaskManager.Instance.playerMask);
    }

    protected override void OnTrigger(Collider2D collision)
    {
        if (collision.TryGetComponent(out PlayableCharacterControllerBase playableCharacter))
        {
            OnPlayerAction(playableCharacter);
            gameObject.SetActive(false);
        }
    }

    protected abstract void OnPlayerAction(PlayableCharacterControllerBase playableCharacter);
}