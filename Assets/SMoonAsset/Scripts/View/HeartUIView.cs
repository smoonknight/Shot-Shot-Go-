using UnityEngine;
using UnityEngine.UI;

public class HeartUIView : ViewBase
{
    [SerializeField]
    private Sprite fillSprite;
    [SerializeField]
    private Sprite emptySprite;
    [SerializeField]
    private Image image;

    public HeartUIType CurrentType { get; private set; }

    public void ChangeSprite(HeartUIType type)
    {
        CurrentType = type;
        image.sprite = type switch
        {
            HeartUIType.Fill => fillSprite,
            HeartUIType.Empty => emptySprite,
            _ => throw new System.NotImplementedException(),
        };
    }
}

public enum HeartUIType
{
    Fill, Empty
}
