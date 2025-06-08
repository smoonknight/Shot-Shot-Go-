using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameplayManager : Singleton<GameplayManager>
{
    public GameModeType currentGameModeType;
    public MusicName musicName;
    public MusicName endMusicName;
    public RateCollector<CollectableType> collectableTypeRateCollector;
    public List<PlayerUpgradePlanPorperty> playerUpgradePlanPorperties;
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    public PlayerController playerControllerPrefab;

    protected override void OnAwake()
    {
        base.OnAwake();
        if (currentGameModeType != GameModeType.MainMenu)
        {
            collectableTypeRateCollector.Calculate();
            SetPlayerByGameMode(currentGameModeType);
        }

        SetGameMode(currentGameModeType);

        AudioExtendedManager.Instance.SetMusic(musicName);
    }

    void SetPlayerByGameMode(GameModeType gameModeType)
    {
        PlayerController playerController = Instantiate(playerControllerPrefab);
        Vector2 position = gameModeType switch
        {
            GameModeType.Normal => throw new NotImplementedException(),
            GameModeType.Rogue => RogueManager.Instance.GetSampleSpawnPosition(),
            _ => throw new NotImplementedException(),
        };
        playerController.ForceChangePosition(position);
        cinemachineVirtualCamera.Follow = playerController.transform;
    }

    void SetGameMode(GameModeType type)
    {
        switch (type)
        {
            case GameModeType.Rogue: RogueManager.Instance.EnvironmentUpdate(); break;
            case GameModeType.MainMenu: MainMenuManager.Instance.AddEnemys(cinemachineVirtualCamera); break;
        }
    }

    public Vector3 GetOutOfBoundByGameMode()
    {
        return currentGameModeType switch
        {
            GameModeType.Normal => LevelManager.Instance.latestCheckpoint,
            GameModeType.Rogue => RogueManager.Instance.GetSampleSpawnPosition(),
            GameModeType.MainMenu => Vector3.zero,
            _ => throw new NotImplementedException(),
        };
    }

    public void DropCollectable(PlayableCharacterControllerBase playableCharacter)
    {
        var spawnedCollectable = CollectableSpawnerManager.Instance.GetSpawned(collectableTypeRateCollector.GetRandomData());
        spawnedCollectable.value = playableCharacter.GetCharacterUpgradeProperty().exp;
        spawnedCollectable.transform.position = playableCharacter.transform.position;
    }

    public List<List<PlayerUpgradePlanPorperty>> GetUpgradeStatsRandomly(int amount)
    {
        int count = playerUpgradePlanPorperties.Count;
        const int itemPerList = 3;

        if (amount <= 0 || count == 0)
            return new List<List<PlayerUpgradePlanPorperty>>();

        var rnd = new System.Random();
        var results = new List<List<PlayerUpgradePlanPorperty>>(amount);

        for (int n = 0; n < amount; n++)
        {
            var pool = new List<PlayerUpgradePlanPorperty>(playerUpgradePlanPorperties);
            int takeCount = Math.Min(itemPerList, pool.Count);

            for (int i = 0; i < takeCount; i++)
            {
                int j = rnd.Next(i, pool.Count);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            results.Add(pool.GetRange(0, takeCount));
        }

        return results;
    }

    public async void RaisePause(PlayerController playerController)
    {
        if (UIManager.Instance.IsPause)
        {
            return;
        }

        playerController.playerStateMachine.SetStateWhenDifference(PlayerStateType.Pause);
        await UIManager.Instance.SetPause(true);
        await UniTask.WaitUntil(() => !UIManager.Instance.IsPause);
        if (playerController != null)
            playerController.playerStateMachine.SetPrevState(PlayerStateType.Play);
    }

    public async void RaiseGameOver(PlayerController playerController)
    {
        PostGameOverOptions? postGameOverOptions = await UIManager.Instance.SetGameOver();

        switch (postGameOverOptions)
        {
            case PostGameOverOptions.Restart: TransitionManager.Instance.SetTransitionOnSceneManager(TransitionType.Black, GetSceneEnumByGameMode()); break;
            case PostGameOverOptions.Resurrect:
                playerController.AddHealth(playerController.MaximumHealth);
                playerController.playerStateMachine.SetState(PlayerStateType.Play);
                AudioExtendedManager.Instance.SetMusic(musicName);
                break;
            case PostGameOverOptions.MainMenu: TransitionManager.Instance.SetTransitionOnSceneManager(TransitionType.Loading, SceneEnum.MAINMENU); break;
        }
    }

    SceneEnum GetSceneEnumByGameMode() => currentGameModeType switch
    {
        GameModeType.Normal => throw new NotImplementedException(),
        GameModeType.Rogue => SceneEnum.GAMEPLAY_ROGUE,
        _ => throw new NotImplementedException(),
    };
}

[Serializable]
public class PlayerUpgradePlanPorperty
{
    public UpgradeType type;
    public List<UpgradeStat> upgradeStats;

    public int randomizeIndex;

    public UpgradeStat GetUpgradeStatPlan()
    {
        if (CurrentPlanIndex == randomizeIndex) upgradeStats.Randomize();
        return upgradeStats[CurrentPlanIndex];
    }
    private int currentPlanIndex = 0;
    public int CurrentPlanIndex
    {
        get => currentPlanIndex;
        set
        {
            currentPlanIndex = value;
            if (currentPlanIndex >= upgradeStats.Count)
            {
                currentPlanIndex = 0;
            }
        }
    }
}

[Serializable]
public struct UpgradeStat
{
    public UpgradeStatType type;
    public float value;
}

public enum UpgradeStatType
{
    size,
    attackInterval,
    speed,
    health,
    damage,
    jump,
    quantity,
}

public enum UpgradeType
{
    Character,
    MagicSword_Common,
    MagicSword_OnTarget,
    MagicSword_Slashing
}