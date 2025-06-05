using System.Collections.Generic;
using SMoonUniversalAsset;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    private HeartUIView heartUIViewTemplate;
    [SerializeField]
    private RectTransform healthPanel;

    private List<HeartUIView> heartUIViews = new();

    int latestHeart;
    int latestMaximumHeart;

    public void InitializePlayer(PlayerController playerController)
    {
        latestHeart = playerController.Health / 10;
        latestMaximumHeart = playerController.MaximumHealth / 10;

        for (int i = 0; i < latestMaximumHeart; i++)
        {
            var heartUIView = Instantiate(heartUIViewTemplate, healthPanel);
            heartUIViews.Add(heartUIView);
        }

        for (int i = 0; i < latestMaximumHeart; i++)
        {
            HeartUIType type = i < latestHeart ? HeartUIType.Fill : HeartUIType.Empty;
            heartUIViews[i].ChangeSprite(type);
        }
    }

    public void SetHealth(int heart)
    {
        if (latestHeart == heart)
        {
            return;
        }
        for (int i = 0; i < heartUIViews.Count; i++)
        {
            HeartUIType newType = i < heart ? HeartUIType.Fill : HeartUIType.Empty;
            if (heartUIViews[i].CurrentType != newType)
            {
                heartUIViews[i].ChangeSprite(newType);
            }
        }
    }

    public void SetMaximumHealth(int maximumHeart)
    {
        if (latestMaximumHeart == maximumHeart)
        {
            return;
        }
        int currentCount = heartUIViews.Count;

        if (maximumHeart > currentCount)
        {
            int toAdd = maximumHeart - currentCount;
            for (int i = 0; i < toAdd; i++)
            {
                var heartUIView = Instantiate(heartUIViewTemplate, healthPanel);
                heartUIViews.Add(heartUIView);
            }
        }
        else if (maximumHeart < currentCount)
        {
            int toRemove = currentCount - maximumHeart;
            for (int i = 0; i < toRemove; i++)
            {
                int lastIndex = heartUIViews.Count - 1;
                Destroy(heartUIViews[lastIndex].gameObject);
                heartUIViews.RemoveAt(lastIndex);
            }
        }
    }

}

