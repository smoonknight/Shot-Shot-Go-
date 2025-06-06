using UnityEngine;

public abstract class ItemController<T> : MonoBehaviour where T : ItemBase
{
    public T itemBase;
    public SpriteRenderer spriteRenderer;
    protected bool isSkipTransitionDestroy;

    public void SetItemBase(T itemBase)
    {
        this.itemBase.quantity = itemBase.quantity;
        spriteRenderer.sprite = itemBase.GetSprite();
    }

    public virtual void Disable()
    {
        if (isSkipTransitionDestroy)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }
}