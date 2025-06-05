using UnityEngine;
using UnityEngine.UI;

public class HeartUIView : ViewBase
{
    [SerializeField]
    private Image image;

    public void ChangeFillAmount(float fillAmount)
    {
        image.fillAmount = fillAmount;
    }
}