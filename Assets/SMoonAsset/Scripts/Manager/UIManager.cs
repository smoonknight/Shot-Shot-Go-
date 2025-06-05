using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
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

    public void SetHealth(PlayerController playerController)
    {
        float totalHearts = playerController.Health / heartRate;
        if (latestHeart == totalHearts)
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

    public async UniTask StartChooseUpgrade(List<PlayerUpgradePlanPorperty> playerUpgradePlanPorperties, bool startImmidietly, bool endImmidietly)
    {
        chooseUpgradeSpawner.HideSpawn();
        bool isSelected = false;
        characterUpgradeCanvas.enabled = true;

        List<ImageButtonView> chooseUpgradeImageButtonViews = new();

        for (int i = 0; i < playerUpgradePlanPorperties.Count; i++)
        {
            PlayerUpgradePlanPorperty playerUpgradePlanPorperty = playerUpgradePlanPorperties[i];
            ImageButtonView imageButtonView = chooseUpgradeSpawner.GetSpawned();
            chooseUpgradeImageButtonViews.Add(imageButtonView);
        }
        for (int i = 0; i < playerUpgradePlanPorperties.Count; i++)
        {
            ImageButtonView imageButtonView = chooseUpgradeImageButtonViews[i];
            PlayerUpgradePlanPorperty playerUpgradePlanPorperty = playerUpgradePlanPorperties[i];
            Sprite sprite = GetSpriteByType(playerUpgradePlanPorperty.type);
            string titleText = LanguageManager.Instance.GetAddressableStringId(playerUpgradePlanPorperty.type).ToCommonLanguage();
            UpgradeStat upgradeStat = playerUpgradePlanPorperty.upgradeStats.GetRandom();
            string descriptionText = upgradeStat.type.ToString();
            imageButtonView.Initialize(sprite, titleText, descriptionText, () => OnChooseItem(chooseUpgradeImageButtonViews, imageButtonView, playerUpgradePlanPorperty.type, upgradeStat, ref isSelected, endImmidietly));
        }

        await UniTask.WaitUntil(() => isSelected);
        characterUpgradeCanvas.enabled = false;
    }

    private void OnChooseItem(List<ImageButtonView> chooseUpgradeImageButtonViews, ImageButtonView selectedImageButtonView, UpgradeType upgradeType, UpgradeStat upgradeStat, ref bool isSelected, bool endImmidietly)
    {

        isSelected = true;
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

