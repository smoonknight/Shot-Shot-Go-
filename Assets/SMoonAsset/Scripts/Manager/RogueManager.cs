using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using UnityEngine;

public class RogueManager : Singleton<RogueManager>
{
    [SerializeField]
    private ExperienceStat rogueExperienceStat;
    [SerializeField]
    private List<LevelEnemyProperty> levelEnemyProperties;
    [SerializeField]
    private List<TilemapProperty> tilemapProperties;
    [SerializeField]
    private List<EnvironmentProperty> environmentProperties;

    private TimeChecker nextSpawnChecker;
    private LevelEnemyProperty latestLevelEnemyProperty;
    private EnvironmentProperty currentEnvironmentProperty;

    private TilemapController GetSampleTilemapController() => currentEnvironmentProperty.allowedTilemapControllers.FindAll(match => match.IsSpawnAreasExist).GetRandom();

    const float minimumEnvironmentChangeDuration = 15;
    const float maximumEnvironmentChangeDuration = 30;

    const float transitionEnvironmentChangeDuration = 5;

    CancellationTokenSource cancellationTokenSource;

    protected override void Awake()
    {
        base.Awake();

        latestLevelEnemyProperty = GetLevelEnemyProperty(rogueExperienceStat.Level);
        nextSpawnChecker = new(latestLevelEnemyProperty.nextEnemySpawnDuration, false);

        EnvironmentAwake();
    }

    private void OnEnable()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new();
        EnvironmentUpdate(cancellationTokenSource.Token);
    }

    private void OnDisable()
    {

    }

    private void FixedUpdate()
    {
        if (!nextSpawnChecker.IsDurationEnd())
        {
            return;
        }

        if (latestLevelEnemyProperty.maximumEnemy > EnemySpawnerManager.Instance.GetActiveSpawn())
        {
            Debug.Log(EnemySpawnerManager.Instance.GetActiveSpawn());
            for (int i = 0; i < latestLevelEnemyProperty.amountEnemy; i++)
            {
                var randomEnemy = latestLevelEnemyProperty.enemyTypeRateCollector.GetRandomData();
                Vector2 position = GetSampleTilemapController().GetRandomPointInSpawnAreas();
                EnemySpawnerManager.Instance.GetSpawned(randomEnemy, position);
            }
        }

        nextSpawnChecker.UpdateTime(latestLevelEnemyProperty.nextEnemySpawnDuration);
    }

    private void EnvironmentAwake()
    {
        SetupEnvironmentProperties();
        SetRandomEnvironment();
    }

    private void SetupEnvironmentProperties()
    {
        foreach (var tilemapProperty in tilemapProperties)
        {
            tilemapProperty.tilemapController.gameObject.SetActive(false);
        }
        foreach (var environmentProperty in environmentProperties)
        {
            foreach (var type in environmentProperty.allowedTilemapTypes)
            {
                TilemapController tilemapController = tilemapProperties.Find(match => match.type == type).tilemapController;
                environmentProperty.allowedTilemapControllers.Add(tilemapController);
            }
        }
    }

    private async void EnvironmentUpdate(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            float changeDuration = Random.Range(minimumEnvironmentChangeDuration, maximumEnvironmentChangeDuration);
            await UniTask.WaitForSeconds(changeDuration);

            float time = 0;
            while (time < transitionEnvironmentChangeDuration)
            {
                time += Time.deltaTime;
            }

            SetRandomEnvironment();
        }
    }

    private void SetRandomEnvironment() => SetEnvironment(environmentProperties.GetRandom());

    private void SetEnvironment(EnvironmentProperty environmentProperty)
    {
        if (currentEnvironmentProperty != null)
        {
            ResetEnvironment(environmentProperty);
        }
        currentEnvironmentProperty = environmentProperty;

        foreach (var allowedTilemapController in currentEnvironmentProperty.allowedTilemapControllers)
        {
            allowedTilemapController.gameObject.SetActive(true);
            allowedTilemapController.tilemap.enabled = true;
            allowedTilemapController.tilemapCollider2D.enabled = true;
            allowedTilemapController.tilemapRenderer.enabled = true;
        }
    }

    private void ResetEnvironment(EnvironmentProperty environmentProperty)
    {
        foreach (var allowedTilemapControllers in environmentProperty.allowedTilemapControllers)
        {
            allowedTilemapControllers.gameObject.SetActive(false);
        }
    }

    private void AddExperience(int exp)
    {
        rogueExperienceStat.AddExperience(exp, out bool isLevelUp, out (int currentLevel, int levelUpCount) result);
        if (isLevelUp)
            latestLevelEnemyProperty = GetLevelEnemyProperty(result.currentLevel);
    }

    private LevelEnemyProperty GetLevelEnemyProperty(int level) => levelEnemyProperties.Where(levelEnemyProperty => levelEnemyProperty.level <= level)
        .OrderByDescending(levelEnemyProperty => levelEnemyProperty.level)
        .FirstOrDefault();
}

[System.Serializable]
public class LevelEnemyProperty
{
    public int level;
    public int amountEnemy = 2;
    public int maximumEnemy = 10;
    public int nextEnemySpawnDuration = 5;
    public int multiplierUpgradeProperty = 1;
    public RateCollector<EnemyType> enemyTypeRateCollector;
}

[System.Serializable]
public class EnvironmentProperty
{
    public List<TilemapType> allowedTilemapTypes;
    [ReadOnly]
    public List<TilemapController> allowedTilemapControllers;
}

[System.Serializable]
public class TilemapProperty
{
    public TilemapController tilemapController;
    public TilemapType type;
}

public enum TilemapType
{
    GroundFlat, WallToWallJump, FloatingMiddle
}