using System.Threading.Tasks;
using Cinemachine;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using TMPro;
using UnityEngine;

public class MainMenuManager : Singleton<MainMenuManager>
{
    public ButtonView startButtonView;
    public ButtonView settingsButtonView;
    public ButtonView exitButtonView;
    public TextMeshProUGUI controlGuideText;

    async void OnEnable()
    {
        await UniTask.WaitUntil(() => SettingManager.Instance != null);
        startButtonView.Initialize(() => TransitionManager.Instance.SetTransitionOnSceneManager(TransitionType.Loading, SceneEnum.GAMEPLAY_ROGUE));
        settingsButtonView.Initialize(() => SettingManager.Instance.Raise(false));
        exitButtonView.Initialize(Application.Quit);

        Time.timeScale = 1;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        controlGuideText.enabled = true;
#else
        controlGuideText.enabled = false;
#endif
        GameManager.Instance.SetCursor(true);
    }

    void OnDisable()
    {
        startButtonView.Clear();
        settingsButtonView.Clear();
        exitButtonView.Clear();
    }

    public int maximumEnemy = 5;
    public async void AddEnemys(CinemachineVirtualCamera cinemachineVirtualCamera)
    {
        await UniTask.WaitUntil(() => EnemySpawnerManager.Instance != null);
        EnemyController latestEnemyController = null;
        for (int i = 0; i < maximumEnemy; i++)
        {
            latestEnemyController = EnemySpawnerManager.Instance.GetSpawned(RandomHelper.GetRandomEnum<EnemyType>());
        }
        cinemachineVirtualCamera.Follow = latestEnemyController.transform;
    }
}