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
    [Space]
    [SerializeField]
    private List<LevelEnemyProperty> levelEnemyProperties;
    [SerializeField]
    private List<TilemapProperty> tilemapProperties;
    [SerializeField]
    private List<EnvironmentProperty> environmentProperties;
    [SerializeField]
    private int spawnExperience = 5;

    private TimeChecker nextSpawnChecker;
    private LevelEnemyProperty latestLevelEnemyProperty;
    private EnvironmentProperty currentEnvironmentProperty;

    private TilemapController GetSampleTilemapController() => currentEnvironmentProperty.allowedTilemapControllers.FindAll(match => match.IsSpawnAreasExist).GetRandom();

    public Vector3 GetSampleSpawnPosition() => GetSampleTilemapController().GetRandomPointInSpawnAreas();

    public Vector3 GetSampleSpawnPosition(Vector2 targetPosition)
    {
        var closest = currentEnvironmentProperty.allowedTilemapControllers
            .FindAll(match => match.IsSpawnAreasExist)
            .OrderBy(t => Vector2.Distance(t.transform.position, targetPosition))
            .FirstOrDefault();

        return closest != null ? closest.transform.position : targetPosition;
    }

    const float minimumEnvironmentChangeDuration = 15;
    const float maximumEnvironmentChangeDuration = 33;

    const float transitionEnvironmentChangeDuration = 7;

    const float modifierRate = 0.3f;

    CancellationTokenSource cancellationTokenSource;

    int currentLevel = 1;


    Dictionary<int, AudioName> countdownSFX = new()
    {
        { 3, AudioName.SFX_THREE },
        { 2, AudioName.SFX_TWO },
        { 1, AudioName.SFX_ONE },
    };

    readonly HashSet<int> played = new();


    protected override void OnAwake()
    {
        base.OnAwake();

        latestLevelEnemyProperty = GetLevelEnemyProperty(rogueExperienceStat.Level);
        nextSpawnChecker = new(latestLevelEnemyProperty.nextEnemySpawnDuration, false);
        levelEnemyProperties.ForEach(levelEnemyProperty => levelEnemyProperty.enemyTypeRateCollector.Calculate());
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
        cancellationTokenSource?.Cancel();
    }

    private void FixedUpdate()
    {
        if (!nextSpawnChecker.IsDurationEnd())
        {
            return;
        }

        if (latestLevelEnemyProperty.maximumEnemy > EnemySpawnerManager.Instance.GetActiveSpawn())
        {
            for (int i = 0; i < latestLevelEnemyProperty.amountEnemy; i++)
            {
                var randomEnemy = latestLevelEnemyProperty.enemyTypeRateCollector.GetRandomData();
                Vector2 position = GetSampleTilemapController().GetRandomPointInSpawnAreas();
                EnemyController enemyController = EnemySpawnerManager.Instance.GetSpawned(randomEnemy, position);
                enemyController.ModifierUpgradeProperty(1 + currentLevel * modifierRate);
            }
        }

        AddExperience(latestLevelEnemyProperty.amountEnemy * spawnExperience);

        nextSpawnChecker.UpdateTime(latestLevelEnemyProperty.nextEnemySpawnDuration);
    }

    private void EnvironmentAwake()
    {
        SetupEnvironmentProperties();
        SetRandomEnvironment();
    }

    public void EnvironmentStop()
    {
        cancellationTokenSource?.Cancel();
    }

    private void SetupEnvironmentProperties()
    {
        foreach (var tilemapProperty in tilemapProperties)
        {
            tilemapProperty.tilemapController.gameObject.SetActive(false);
            tilemapProperty.tilemapController.Reset();
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

            var nextProperties = environmentProperties.GetRandomWithExcept(currentEnvironmentProperty);

            foreach (var ctrl in nextProperties.allowedTilemapControllers)
            {
                ctrl.gameObject.SetActive(true);
            }

            var nextSet = new HashSet<TilemapController>(nextProperties.allowedTilemapControllers);

            var currentExclusive = new List<TilemapController>();
            var nextExclusive = new List<TilemapController>();
            var overlapping = new List<TilemapController>();

            AudioExtendedManager.Instance.SetAudioMixerBGMLowpassSmoothly(0.1f);

            foreach (var ctrl in currentEnvironmentProperty.allowedTilemapControllers)
            {
                if (nextSet.Contains(ctrl))
                    overlapping.Add(ctrl);
                else
                    currentExclusive.Add(ctrl);
            }
            foreach (var ctrl in nextProperties.allowedTilemapControllers)
            {
                if (!currentEnvironmentProperty.allowedTilemapControllers.Contains(ctrl))
                    nextExclusive.Add(ctrl);
            }

            foreach (var ctrl in nextExclusive)
            {
                ctrl.tilemapCollider2D.enabled = false;
            }

            // Transisi
            played.Clear();
            float time = 0f;
            while (time < transitionEnvironmentChangeDuration)
            {
                float value = Mathf.PingPong(time, 1f);
                float inverse = 1f - value;

                foreach (var ctrl in currentExclusive)
                {
                    var col = ctrl.tilemap.color;
                    col.a = inverse;
                    ctrl.tilemap.color = col;
                }

                foreach (var ctrl in nextExclusive)
                {
                    var col = ctrl.tilemap.color;
                    col.a = value;
                    ctrl.tilemap.color = col;
                }

                time += Time.deltaTime;

                int remaining = (int)transitionEnvironmentChangeDuration - Mathf.FloorToInt(time);
                if (countdownSFX.TryGetValue(remaining, out var sfx) && !played.Contains(remaining))
                {
                    AudioExtendedManager.Instance.Play(sfx);
                    played.Add(remaining);
                }

                await UniTask.Yield();
            }

            AudioExtendedManager.Instance.SetAudioMixerBGMLowpassSmoothly(1);

            SetEnvironment(nextProperties);
        }
    }

    private void SetRandomEnvironment() => SetEnvironment(environmentProperties.GetRandomWithExcept(currentEnvironmentProperty));

    private void SetEnvironment(EnvironmentProperty environmentProperty)
    {
        if (currentEnvironmentProperty != null)
        {
            DisableEnvironment(currentEnvironmentProperty);
        }
        currentEnvironmentProperty = environmentProperty;

        foreach (var allowedTilemapController in currentEnvironmentProperty.allowedTilemapControllers)
        {
            allowedTilemapController.gameObject.SetActive(true);
            allowedTilemapController.Reset();
        }
    }

    private void DisableEnvironment(EnvironmentProperty environmentProperty)
    {
        foreach (var allowedTilemapController in environmentProperty.allowedTilemapControllers)
        {
            allowedTilemapController.gameObject.SetActive(false);
        }
    }

    private void AddExperience(int exp)
    {
        rogueExperienceStat.AddExperience(exp, out bool isLevelUp, out currentLevel, out _);
        if (isLevelUp)
            latestLevelEnemyProperty = GetLevelEnemyProperty(currentLevel);
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
    GroundFlat, WallToWallJump, FloatingMiddle, Skyland, GroundOfWall,
}