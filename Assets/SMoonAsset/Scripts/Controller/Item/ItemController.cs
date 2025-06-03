using UnityEngine;

public abstract class ItemController<T> : MonoBehaviour where T : ItemBase
{
    public T itemBase;
    public SpriteRenderer spriteRenderer;
    protected bool isSkipTransitionDestroy;

    public void SetItemBase(T itemBase)
    {
        this.itemBase = itemBase;
        spriteRenderer.sprite = itemBase.sprite;
    }

    public void Disable()
    {
        if (isSkipTransitionDestroy)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }
}