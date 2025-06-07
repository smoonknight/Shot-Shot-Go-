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
    private CanvasGroup characterUpgradeCanvasGroup;
    [SerializeField]
    private ChooseUpgradeSpawner chooseUpgradeSpawner;
    [SerializeField]
    private List<UpgradeSpriteProperty> upgradeSpriteProperties;
    [SerializeField]
    private List<Vector2> chooseUpgradeAnchoredPositions;
    [Space]
    [SerializeField]
    private TextMeshProUGUI levelText;
    [Space]
    [SerializeField]
    private Image experienceImageBar;

    [Space]
    [SerializeField]
    private TextMeshProUGUITranslator utilityTitleText;
    [SerializeField]
    private ButtonView firstButtonView;
    [SerializeField]
    private ButtonView secondButtonView;
    [SerializeField]
    private ButtonView thirdButtonView;
    [SerializeField]
    private Canvas utilityCanvas;
    [SerializeField]
    private CanvasGroup utilityCanvasGroup;
    [SerializeField]
    private TextMeshProUGUI tooltipText;

    private List<HeartUIView> heartUIViews = new();

    const int maximumChooseUpgrade = 3;

    float latestHeart;
    float latestMaximumHeart;

    const float heartRate = 10;

    public bool IsPause { get; private set; }

    private bool isProcessingPause;

    private bool isProcessingGameOver;

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
        var tcs = new UniTaskCompletionSource<int>();
        characterUpgradeCanvas.enabled = true;

        List<ImageButtonView> chooseUpgradeImageButtonViews = new();

        if (!startImmidietly)
        {
            UniTask.Void(async () => await AnimationFadedInOut(characterUpgradeCanvasGroup, 0, 1, 0.4f));
        }

        for (int i = 0; i < maximumChooseUpgrade; i++)
        {
            ImageButtonView imageButtonView = chooseUpgradeSpawner.GetSpawned();
            imageButtonView.enabled = true;
            chooseUpgradeImageButtonViews.Add(imageButtonView);
        }

        for (int i = 0; i < maximumChooseUpgrade; i++)
        {
            ImageButtonView imageButtonView = chooseUpgradeImageButtonViews[i];
            Vector2 anchor = chooseUpgradeAnchoredPositions[i];

            if (!startImmidietly)
            {
                AnimationInChooseUpgrade(imageButtonView, anchor);
            }
            else
            {
                imageButtonView.rectTransform.anchoredPosition = anchor;
            }

            (UpgradeType upgradeType, UpgradeStat upgradeStat, int index) value = values[i];
            Sprite sprite = GetSpriteByType(value.upgradeType);
            string titleText = LanguageManager.Instance.GetAddressableStringId(value.upgradeType).ToCommonLanguage();
            string descriptionText = LanguageManager.Instance.GetAddressableStringId(value.upgradeType, value.upgradeStat.type).ToCommonLangaugeWithReplector(value.upgradeStat.value);

            imageButtonView.Initialize(sprite, titleText, descriptionText, async () =>
            {
                playerController.UpgradeStatProperty(value.upgradeType, value.upgradeStat);

                if (!endImmidietly)
                {
                    chooseUpgradeImageButtonViews.Remove(imageButtonView);
                    imageButtonView.enabled = false;
                    foreach (var chooseUpgradeImageButtonView in chooseUpgradeImageButtonViews)
                    {
                        await AnimationOutChooseUpgrade(chooseUpgradeImageButtonView);
                    }
                }
                tcs.TrySetResult(value.index);
            });
        }

        int selectedIndex = await tcs.Task;
        if (!endImmidietly)
        {
            await AnimationFadedInOut(characterUpgradeCanvasGroup, 1, 0, 0.4f);
        }
        characterUpgradeCanvas.enabled = false;
        return selectedIndex;
    }

    private async UniTask AnimationFadedInOut(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            if (canvasGroup == null)
            {
                return;
            }
            float easeValue = EaseFunctions.EaseOutQuad(time / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, easeValue);

            time += Time.unscaledDeltaTime;

            await UniTask.Yield();
        }
    }

    private async void AnimationInChooseUpgrade(ImageButtonView imageButtonView, Vector2 maxAnchorPosition)
    {
        imageButtonView.enabled = false;
        await AnimationInOutChooseUpgrade(imageButtonView, maxAnchorPosition, 0, 1, 0.4f);
        imageButtonView.enabled = true;
    }

    private async UniTask AnimationOutChooseUpgrade(ImageButtonView imageButtonView)
    {
        imageButtonView.enabled = false;
        Vector2 maxAnchorPosition = imageButtonView.rectTransform.anchoredPosition;
        await AnimationInOutChooseUpgrade(imageButtonView, maxAnchorPosition, 1, 0, 0.4f);
        imageButtonView.enabled = true;
    }

    private async UniTask AnimationInOutChooseUpgrade(ImageButtonView imageButtonView, Vector2 maxAnchorPosition, float from, float to, float duration)
    {
        float time = 0;
        Vector2 minAnchorPosition = new Vector2(maxAnchorPosition.x, maxAnchorPosition.y - 200);
        CanvasGroup imageButtonCanvasGroup = imageButtonView.GetCanvasGroup();
        RectTransform imageButtonRectTransform = imageButtonView.rectTransform;

        while (time < duration)
        {
            if (imageButtonCanvasGroup == null)
            {
                return;
            }

            float easeValue = EaseFunctions.EaseOutQuad(time / duration);
            float value = Mathf.Lerp(from, to, easeValue);
            imageButtonCanvasGroup.alpha = value;
            imageButtonRectTransform.anchoredPosition = Vector2.Lerp(minAnchorPosition, maxAnchorPosition, value);

            time += Time.unscaledDeltaTime;

            await UniTask.Yield();
        }
    }

    public async UniTask SetPause(bool condition)
    {
        if (isProcessingPause)
        {
            return;
        }
        isProcessingPause = true;

        if (condition)
        {
            IsPause = condition;
            utilityCanvas.enabled = true;

            utilityTitleText.Translate(StringId.Pause);

            firstButtonView.TryInitializeTranslator(StringId.Continue, async () => await SetPause(false));
            secondButtonView.TryInitializeTranslator(StringId.Settings, () => SetSettings(true));
            thirdButtonView.TryInitializeTranslator(StringId.MainMenu, () => TransitionManager.Instance.SetTransitionOnSceneManager(TransitionType.Loading, SceneEnum.MAINMENU));
            await AnimationFadedInOut(utilityCanvasGroup, 0, 1, 0.4f);
        }
        else
        {
            await AnimationFadedInOut(utilityCanvasGroup, 1, 0, 0.4f);
            utilityCanvas.enabled = false;
            IsPause = condition;
        }

        isProcessingPause = false;
    }

    public void SetSettings(bool condition)
    {
    }

    public async UniTask<PostGameOverOptions?> SetGameOver()
    {
        if (isProcessingGameOver)
        {
            return null;
        }

        utilityCanvas.enabled = true;
        isProcessingGameOver = true;

        var tcs = new UniTaskCompletionSource<PostGameOverOptions>();

        utilityTitleText.Translate(StringId.GameOver);

        firstButtonView.TryInitializeTranslator(StringId.Restart, () => tcs.TrySetResult(PostGameOverOptions.Restart));
        firstButtonView.SetHoverAction((position, condition) => SetToolTip(position, condition, StringId.PostGameOverOptionsToolTipRestart.ToCommonLanguage()));
        secondButtonView.TryInitializeTranslator(StringId.Resurrect, () => tcs.TrySetResult(PostGameOverOptions.Resurrect));
        firstButtonView.SetHoverAction((position, condition) => SetToolTip(position, condition, StringId.PostGameOverOptionsToolTipRessurect.ToCommonLanguage()));
        thirdButtonView.TryInitializeTranslator(StringId.MainMenu, () => tcs.TrySetResult(PostGameOverOptions.MainMenu));
        firstButtonView.SetHoverAction((position, condition) => SetToolTip(position, condition, StringId.PostGameOverOptionsToolTipMainMenu.ToCommonLanguage()));

        PostGameOverOptions postGameOverOptions = await tcs.Task;

        await AnimationFadedInOut(utilityCanvasGroup, 1, 0, 0.4f);

        firstButtonView.Clear();
        secondButtonView.Clear();
        thirdButtonView.Clear();

        utilityCanvas.enabled = false;
        isProcessingGameOver = false;

        return postGameOverOptions;
    }

    void SetToolTip(Vector2 screenPosition, bool condition, string text)
    {
        tooltipText.enabled = condition;
        if (!condition)
        {
            return;
        }

        tooltipText.text = text;
        RectTransform parentRect = tooltipText.rectTransform.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            null,
            out Vector2 localPoint
        );

        tooltipText.rectTransform.anchoredPosition = localPoint;
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


public enum PostGameOverOptions
{
    Restart, Resurrect, MainMenu
}

