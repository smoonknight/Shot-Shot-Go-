using System;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class TransitionManager : SingletonWithDontDestroyOnLoad<TransitionManager>
{
    public TransitionSpawner transitionSpawner;

    TransitionType lastTransitionType;
    SceneEnum lastSceneManagerEnum;

    public bool isOnProgressTransitionScene { get; private set; }

    private void Start()
    {
        SceneHelper.CheckCurrentSceneRequire();
    }

    public void SetTransition(TransitionType type, UnityAction midTransitionCallback, UnityAction endTransitionCallback, float delayDuration = 0)
    {
        if (type == TransitionType.None)
        {
            midTransitionCallback?.Invoke();
            endTransitionCallback?.Invoke();
            return;
        }
        TransitionController transitionController = transitionSpawner.GetSpawned(type);

        LeanTween.value(0, 1, 0.5f).setOnUpdate(transitionController.Change).setEaseInExpo().setOnComplete(() =>
        {
            midTransitionCallback?.Invoke();

            var sequence = LeanTween.sequence();

            sequence.append(delayDuration);
            sequence.append(() =>
            {
                LeanTween.value(1, 0, 0.5f).setOnUpdate(transitionController.Change).setEaseInExpo().setOnComplete(() =>
                {
                    endTransitionCallback?.Invoke();
                });
            });
        });
    }

    public void SetTransitionOnSceneManagerPrevious() => SetTransitionOnSceneManager(lastTransitionType, lastSceneManagerEnum);
    public void SetTransitionOnSceneManager(TransitionType type, SceneEnum sceneManagerEnum) => SetTransitionOnSceneManager(type, sceneManagerEnum, () => { }, () => { });
    public void SetTransitionOnSceneManager(TransitionType type, SceneEnum sceneEnum, UnityAction midTransitionCallback, UnityAction endTransitionCallback)
    {
        lastSceneManagerEnum = sceneEnum;
        isOnProgressTransitionScene = true;

        lastTransitionType = type;
        TransitionController transitionController = transitionSpawner.GetSpawned(type);

        midTransitionCallback?.Invoke();

        LeanTween.value(0, 1, 0.5f).setOnUpdate(transitionController.Change).setEaseInExpo().setOnComplete(() =>
        {
            endTransitionCallback += () => isOnProgressTransitionScene = false;
            StartTransition(transitionController, sceneEnum, endTransitionCallback);
        }).setIgnoreTimeScale(true);
    }

    async void StartTransition(TransitionController transitionController, SceneEnum sceneEnum, UnityAction endTransitionCallback)
    {
        var sceneName = SceneHelper.GetSceneBySceneEnum(sceneEnum);
        var operationAsync = SceneManager.LoadSceneAsync(sceneName);

        SceneHelper.CheckSceneRequire(sceneEnum);

        Screen.sleepTimeout = lastSceneManagerEnum != SceneEnum.MAINMENU ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

        operationAsync.completed += (async) =>
        {
            LeanTween.value(1, 0, 0.5f).setOnUpdate(transitionController.Change).setEaseInExpo();
            endTransitionCallback?.Invoke();
            Destroy(transitionController.gameObject);
        };
        await UniTask.WaitUntil(() => operationAsync.isDone);
    }
}

[System.Serializable]
public class SceneManagerData
{
    public SceneManagerData(string sceneName, bool isScreenNeverSleep)
    {
        this.sceneName = sceneName;
        this.isScreenNeverSleep = isScreenNeverSleep;
    }
    public string sceneName;
    public bool isScreenNeverSleep;
}

[System.Serializable]
public class TransitionSpawner : MultiSpawnerBase<TransitionController, TransitionType>
{
    public override void OnSpawn(TransitionController component, TransitionType type, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
    {
    }
}

public enum TransitionType
{
    None,
    Black,
    White,
    Loading,
}