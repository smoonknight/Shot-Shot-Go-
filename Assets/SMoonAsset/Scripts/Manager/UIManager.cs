using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    private HeartUIView heartUIViewTemplate;
    [SerializeField]
    private RectTransform healthPanel;
    [Space]
    [SerializeField]
    private Canvas characterUpgradeCanvas;
    [SerializeField]
    private ChooseUpgradeSpawner chooseUpgradeSpawner;
    [SerializeField]
    private List<UpgradeSpriteProperty> upgradeSpriteProperties;
    [Space]
    [SerializeField]
    private TextMeshProUGUI levelText;
    [Space]
    [SerializeField]
    private Image experienceImageBar;

    private List<HeartUIView> heartUIViews = new();

    float latestHeart;
    float latestMaximumHeart;

    const float heartRate = 10;

    protected override void OnAwake()
    {
        base.OnAwake();
        chooseUpgradeSpawner.Initialize();
    }

    public void InitializePlayer(PlayerController playerController)
    {
        latestMaximumHeart = playerController.MaximumHealth / heartRate;

        for (int i = 0; i < latestMaximumHeart; i++)
        {
            var heartUIView = Instantiate(heartUIViewTemplate, healthPanel);
            heartUIViews.Add(heartUIView);
        }

        SetHealth(playerController);
    }

    public void SetHealth(PlayerController playerController, bool checkLatest = true)
    {
        float totalHearts = playerController.Health / heartRate;
        if (latestHeart == totalHearts && checkLatest)
            return;

        latestHeart = totalHearts;

        int fullHearts = Mathf.FloorToInt(totalHearts);
        float partialFill = playerController.Health % heartRate / heartRate;

        for (int i = 0; i < heartUIViews.Count; i++)
        {
            float value = i < fullHearts ? 1f :
                          i == fullHearts ? partialFill : 0f;

            heartUIViews[i].ChangeFillAmount(value);
        }
    }

    public void SetMaximumHealth(PlayerController playerController)
    {
        float maximumHeart = playerController.MaximumHealth / heartRate;
        if (latestMaximumHeart == maximumHeart)
        {
            return;
        }
        latestMaximumHeart = maximumHeart;
        float currentCount = heartUIViews.Count;

        if (maximumHeart > currentCount)
        {
            float toAdd = maximumHeart - currentCount;
            for (int i = 0; i < toAdd; i++)
            {
                var heartUIView = Instantiate(heartUIViewTemplate, healthPanel);
                heartUIViews.Add(heartUIView);
            }
        }
        else if (maximumHeart < currentCount)
        {
            float toRemove = currentCount - maximumHeart;
            for (int i = 0; i < toRemove; i++)
            {
                int lastIndex = heartUIViews.Count - 1;
                Destroy(heartUIViews[lastIndex].gameObject);
                heartUIViews.RemoveAt(lastIndex);
            }
        }
    }

    public void SetExperience(float percentage)
    {
        experienceImageBar.fillAmount = percentage;
    }

    public void SetLevel(int level)
    {
        levelText.text = $"Level {level}";
    }

    /// <summary>
    /// Displays a list of upgrade options and waits for the player to select one.
    /// Returns the index of the selected upgrade asynchronously.
    /// </summary>
    /// <param name="playerController">The player controller initiating the upgrade selection.</param>
    /// <param name="values">A list of upgrade candidates, each containing the type, stat, and index.</param>
    /// <param name="startImmidietly">If true, the selection UI will appear immediately.</param>
    /// <param name="endImmidietly">If true, the selection UI will disappear immediately after selection.</param>
    /// <returns>A UniTask that completes with the index of the selected upgrade.</returns>
    public async UniTask<int> StartChooseUpgrade(PlayerController playerController, List<(UpgradeType upgradeType, UpgradeStat upgradeStat, int index)> values, bool startImmidietly, bool endImmidietly)
    {
        chooseUpgradeSpawner.HideSpawn();
        bool isSelected = false;
        int selectedIndex = 0;
        characterUpgradeCanvas.enabled = true;

        List<ImageButtonView> chooseUpgradeImageButtonViews = new();

        for (int i = 0; i < values.Count; i++)
        {
            ImageButtonView imageButtonView = chooseUpgradeSpawner.GetSpawned();
            chooseUpgradeImageButtonViews.Add(imageButtonView);
        }
        for (int i = 0; i < values.Count; i++)
        {
            ImageButtonView imageButtonView = chooseUpgradeImageButtonViews[i];
            (UpgradeType upgradeType, UpgradeStat upgradeStat, int index) value = values[i];
            Sprite sprite = GetSpriteByType(value.upgradeType);
            string titleText = LanguageManager.Instance.GetAddressableStringId(value.upgradeType).ToCommonLanguage();
            string descriptionText = LanguageManager.Instance.GetAddressableStringId(value.upgradeType, value.upgradeStat.type).ToCommonLangaugeWithReplector(value.upgradeStat.value);
            imageButtonView.Initialize(sprite, titleText, descriptionText, () => OnChooseItem(playerController, chooseUpgradeImageButtonViews, imageButtonView, value, ref isSelected, ref selectedIndex, endImmidietly));
        }

        await UniTask.WaitUntil(() => isSelected);
        characterUpgradeCanvas.enabled = false;
        return selectedIndex;
    }

    private void OnChooseItem(PlayerController playerController, List<ImageButtonView> chooseUpgradeImageButtonViews, ImageButtonView selectedImageButtonView, (UpgradeType upgradeType, UpgradeStat upgradeStat, int index) value, ref bool isSelected, ref int selectedIndex, bool endImmidietly)
    {
        playerController.UpgradeStatProperty(value.upgradeType, value.upgradeStat);
        isSelected = true;
        selectedIndex = value.index;
    }

    public Sprite GetSpriteByType(UpgradeType type) => upgradeSpriteProperties.Find(find => find.type == type).sprite;

    [Serializable]
    public class ChooseUpgradeSpawner : SingleSpawnerBase<ImageButtonView>
    {
        public override void OnSpawn(ImageButtonView component, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
        {

        }
    }
}

[Serializable]
public struct UpgradeSpriteProperty
{
    public Sprite sprite;
    public UpgradeType type;
}

