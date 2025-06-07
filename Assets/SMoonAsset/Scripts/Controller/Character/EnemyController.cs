using System.Threading;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UniTaskExtensions = SMoonUniversalAsset.UniTaskExtensions;

public class EnemyController : PlayableCharacterControllerBase, ITrampolineable
{
    public EnemyType type;

    public EnemyStateMachine enemyStateMachine;

    [SerializeField]
    private float sideDetectionRange = 1f;
    [SerializeField]
    private float verticalToleranceForAttacking = 0.3f;
    [SerializeField]
    private AudioClip sayNakamaAudioClip;

    private bool holdingJump;
    private bool isWantToMoving;

    public override StatProperty GetCharacterUpgradeProperty() => GameManager.Instance.GetCopyOfDefaultEnemyCharacterUpgradeProperty(type).statProperty;

    public bool IsDamaging() => true;

    public int JumpValue() => 8;

    const float squashingDuration = 0.15f;
    const float waitingNormalAfterSquasedDuration = 0.75f;
    const float normalAfterSquasedDuration = 0.10f;

    const float trampolineTakeDamageCooldownDuration = 0.1f;

    const float minimumWantToJumpTolerance = 3;
    const float maximumWantToJumpTolerance = 6;

    const float playerDetection = 15;

    const float minimumInterestDuration = 5;
    const float maximumInterestDuration = 15;

    FMinMaxRandomizer wantToJumpRandomizer = new(1, 2);
    FMinMaxRandomizer wantToSayNakamaRandomizer = new(10, 15);

    private Vector3 targetMovePosition;
    private FillChecker wantToJumpFillChecker;
    private FillChecker wantToSayNakamaFillChecker;


    CancellationTokenSource squashCancellationTokenSource;
    CancellationTokenSource holdingJumpRandomizerCancellationTokenSource;

    TimeChecker trampolineTakeDamageTimeChecker = new(trampolineTakeDamageCooldownDuration, false);
    TimeChecker interestTimeChecker = new();
    TimeChecker wantToShotTimeChecker = new();

    private PlayerController playerController;

    protected override void Awake()
    {
        base.Awake();

        wantToJumpFillChecker = new(wantToJumpRandomizer.GetRandomize());
        wantToSayNakamaFillChecker = new(wantToSayNakamaRandomizer.GetRandomize());

        enemyStateMachine = new();
        enemyStateMachine.Initialize(this, EnemyStateType.Scouting);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        holdingJumpRandomizerCancellationTokenSource?.Cancel();
    }

    private void Update()
    {
        enemyStateMachine.CurrentState.UpdateState();
    }

    private void FixedUpdate()
    {
        enemyStateMachine.CurrentState.FixedUpdateState();
    }

    public override void SetupPlayable()
    {
        base.SetupPlayable();
        enemyStateMachine?.SetState(EnemyStateType.Scouting);
    }

    public override void TakeDamage(int damage)
    {
        ProcessColorChangePingPong(Color.white, Color.black, Color.white, 0.2f, 1);
        base.TakeDamage(damage);
    }

    public void TrampolineTakeDamage(int damage)
    {
        if (trampolineTakeDamageTimeChecker.IsUpdatingTime())
        {
            TakeDamage(damage);
        }
    }

    protected override float GetMoveTargetDirectionX()
    {
        if (!isWantToMoving)
            return 0;

        float deltaX = targetMovePosition.x - groundCheck.position.x;
        if (Mathf.Abs(deltaX) < 0.1f)
        {
            isWantToMoving = false;
            return 0;
        }

        int direction = deltaX > 0 ? 1 : -1;
        float deltaY = targetMovePosition.y - groundCheck.position.y;
        bool shouldJump = Mathf.Abs(deltaY).IsBetween(minimumWantToJumpTolerance, maximumWantToJumpTolerance);

        bool isEdge = isGrounded && !GetRaycastHit2DOnFrontGround(direction, groundRadius, 1);

        if (isEdge || shouldJump)
        {
            if (wantToJumpFillChecker.PushFill(Time.deltaTime))
            {
                wantToJumpFillChecker.SetRequireFill(wantToJumpRandomizer.GetRandomize());
                ProcessHoldingJumpRandomize(holdingJumpRandomizerCancellationTokenSource.ResetToken());
                JumpAudio();
                SetForce(new Vector2(direction, 1) * jumpForce, 3f, true, () => isGrounded || rigidBody.linearVelocityX == 0);
            }
            return isEdge ? 0 : direction;
        }

        wantToJumpFillChecker.ReduceFill(Time.deltaTime);
        return direction;
    }


    protected RaycastHit2D GetRaycastHit2DOnFrontGround(float horizontalDistance, float downDistance, int maximumIteration)
    {
        float currentDistance = 0 + horizontalDistance;
        for (int i = 0; i < maximumIteration; i++)
        {
            Vector2 origin = new(groundCheck.position.x + currentDistance, groundCheck.position.y);
            RaycastHit2D raycastHit2D = Physics2D.Raycast(origin, Vector2.down, downDistance, LayerMaskManager.Instance.groundableLayer);
            if (raycastHit2D.collider != null)
            {
                return raycastHit2D;
            }
            currentDistance += horizontalDistance;
        }

        return new();
    }

    protected RaycastHit2D GetRaycastHit2DOnSide(float distance, LayerMask targetMask)
    {
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        return Physics2D.Raycast(transform.position, direction, distance, targetMask);
    }

    protected void CloseAttack()
    {
        Vector2 targetCenter = playerController.ColliderCenter;
        Vector2 center = ColliderCenter;

        float horizontalDistance = Mathf.Abs(center.x - targetCenter.x);
        float verticalDistance = Mathf.Abs(center.y - targetCenter.y);

        bool isBeside = horizontalDistance <= sideDetectionRange && verticalDistance <= verticalToleranceForAttacking;

        if (isBeside)
        {
            playerController.ValidateTakeDamage(characterStatProperty.damage, groundCheck.position);
        }
    }

    private void WantToShoot()
    {
        if (wantToShotTimeChecker.IsDurationEnd())
        {
            Fire();
            wantToShotTimeChecker.UpdateTime(characterStatProperty.attackInterval);
        }
    }

    private void WantToSayNakama()
    {
        RaycastHit2D raycastHit2D = GetRaycastHit2DOnSide(1, LayerMaskManager.Instance.enemyMask);
        if (raycastHit2D.collider != null)
        {
            if (wantToSayNakamaFillChecker.PushFill(Time.deltaTime))
            {
                SayNakama();
                wantToSayNakamaFillChecker.SetRequireFill(wantToSayNakamaRandomizer.GetRandomize());
            }
            return;
        }
        wantToJumpFillChecker.ReduceFill(Time.deltaTime);
    }

    void SayNakama()
    {
        ParticleSpawnerManager.Instance.GetSpawned(ParticleType.Nakama, transform.position + Vector3.up * 1.5f);
        audioSource.clip = sayNakamaAudioClip;
        audioSource.Play();
    }

    protected bool TryGetPlayerPosition()
    {
        RaycastHit2D raycastHit2D = Physics2D.CircleCast(groundCheck.position, playerDetection, Vector2.zero, 0, LayerMaskManager.Instance.playerMask);
        if (raycastHit2D.collider != null && raycastHit2D.collider.TryGetComponent(out playerController) && !playerController.IsDead)
        {
            targetMovePosition = playerController.transform.position;
            return true;
        }
        return false;
    }

    protected Vector2 GetSamplePositionByGameMode()
    {
        return GameplayManager.Instance.currentGameModeType switch
        {
            GameModeType.Normal => throw new System.NotImplementedException(),
            GameModeType.Rogue => RogueManager.Instance.GetSampleSpawnPosition(),
            GameModeType.MainMenu => transform.position - Vector3.right * 2,
            _ => throw new System.NotImplementedException(),
        };
    }

    public void OnTakeTrampoline()
    {
        squashCancellationTokenSource?.Cancel();
        squashCancellationTokenSource = new();

        DoSquashAnimation(squashingDuration, waitingNormalAfterSquasedDuration, normalAfterSquasedDuration, squashCancellationTokenSource.Token);
    }

    private async void DoSquashAnimation(float squashingDuration, float waitingNormalAfterSquasedDuration, float normalAfterSquasedDuration, CancellationToken cancellationToken)
    {
        enableMove = false;
        await UniTask.Yield();
        float originalScaleY = initialScale.y;
        float squashedScaleY = originalScaleY * 0.80f;

        await LerpScaleY(originalScaleY, squashedScaleY, squashingDuration, cancellationToken);

        await UniTaskExtensions.DelayWithCancel(waitingNormalAfterSquasedDuration, cancellationToken);

        await LerpScaleY(transform.localScale.y, originalScaleY, normalAfterSquasedDuration, cancellationToken);

        Vector3 scale = transform.localScale;
        scale.x = isFacingRight ? horizontalScale : horizontalScale * -1;
        transform.localScale = scale;
        enableMove = true;
    }

    private async UniTask LerpScaleY(float from, float to, float duration, CancellationToken cancellationToken)
    {
        float time = 0f;
        while (time < duration)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            float t = time / duration;
            Vector3 scale = transform.localScale;
            scale.y = Mathf.Lerp(from, to, t);
            transform.localScale = scale;

            time += Time.deltaTime;
            await UniTask.NextFrame();
        }
    }

    private async void ProcessHoldingJumpRandomize(CancellationToken cancellationToken)
    {
        holdingJump = true;

        await UniTask.Yield();


        float randomWaitingTime = Random.Range(0.5f, 1f);
        await UniTaskExtensions.DelayWithCancel(randomWaitingTime, cancellationToken);

        holdingJump = false;
    }

    public override void OnZeroHealth()
    {
        if (enemyStateMachine.LatestType != EnemyStateType.Dead)
            enemyStateMachine.SetState(EnemyStateType.Dead);
    }

    public override bool IsPlayer() => false;
    public override bool HoldingJump() => holdingJump;
    public override bool CheckOutOfBound() => true;

    public override void OnSetCharacterStatProperty(StatProperty upgradeProperty)
    {
    }

    public override void OnUpgradeCharacterStatProperty(UpgradeStat upgradeStat)
    {
    }

    #region Target Lock State

    private void TargetLockEnter()
    {
        wantToShotTimeChecker.UpdateTime(characterStatProperty.attackInterval);
    }

    private void TargetLockUpdate()
    {
        isGrounded = CheckGrounded();
        ApplyBetterJump();

        UpdateMoveAnimation();
        UpdateJumpAnimation(isGrounded);

        CloseAttack();
    }

    private void TargetLockFixedUpdate()
    {
        ValidateMove();
        WantToShoot();
        WantToSayNakama();
        if (playerController != null && !playerController.IsDead && Vector2.Distance(groundCheck.position, playerController.transform.position) < playerDetection)
        {
            isWantToMoving = true;
            targetMovePosition = playerController.transform.position;
            return;
        }
        enemyStateMachine.SetState(EnemyStateType.Scouting);
    }

    private async void DeadEnter()
    {
        CancellationToken cancellationToken = alphaCancellationTokenSource.ResetToken();
        await LerpGeneric(1f, 0f, 0.5f, Mathf.Lerp, AlphaChange, cancellationToken);
        GameplayManager.Instance.DropCollectable(this);
        gameObject.SetActive(false);
    }

    public class DeadState : BaseState<EnemyController>
    {
        public DeadState(EnemyController component) : base(component)
        {
        }

        public override void EnterState()
        {
            component.DeadEnter();
        }

        public override void FixedUpdateState()
        {
        }

        public override void LeaveState()
        {
        }

        public override void UpdateState()
        {
        }
    }
    public class TargetLockState : BaseState<EnemyController>
    {
        public TargetLockState(EnemyController component) : base(component)
        {
        }

        public override void EnterState()
        {
            component.TargetLockEnter();
        }

        public override void FixedUpdateState()
        {
            component.TargetLockUpdate();
        }

        public override void LeaveState()
        {
        }

        public override void UpdateState()
        {
            component.TargetLockFixedUpdate();
        }
    }

    #endregion

    #region Scouting State

    private void ScoutingEnter()
    {
        isWantToMoving = true;
        targetMovePosition = GetSamplePositionByGameMode();
    }

    private void ScoutingUpdate()
    {
        isGrounded = CheckGrounded();
        ApplyBetterJump();

        UpdateMoveAnimation();
        UpdateJumpAnimation(isGrounded);

        if (TryGetPlayerPosition())
        {
            enemyStateMachine.SetState(EnemyStateType.TargetLock);
            return;
        }

        if (interestTimeChecker.IsDurationEnd())
        {
            enemyStateMachine.SetState(EnemyStateType.Scouting);
            float randomizeInterestDuration = Random.Range(minimumInterestDuration, maximumInterestDuration);
            interestTimeChecker.UpdateTime(randomizeInterestDuration);
        }
    }

    private void ScoutingFixedUpdate()
    {
        ValidateMove();
        WantToSayNakama();
    }

    public class ScoutingState : BaseState<EnemyController>
    {
        public ScoutingState(EnemyController component) : base(component)
        {
        }

        public override void EnterState()
        {
            component.ScoutingEnter();
        }

        public override void FixedUpdateState()
        {
            component.ScoutingFixedUpdate();
        }

        public override void LeaveState()
        {
        }

        public override void UpdateState()
        {
            component.ScoutingUpdate();
        }
    }

    #endregion

    public class EnemyStateMachine : EnumStateMachine<EnemyController, EnemyStateType>
    {
        private ScoutingState scoutingState;
        private TargetLockState targetLockState;
        private DeadState deadState;
        protected override void InitializeState(EnemyController component)
        {
            scoutingState = new(component);
            targetLockState = new(component);
            deadState = new(component);
        }

        protected override BaseState<EnemyController> GetState(EnemyStateType type) => type switch
        {
            EnemyStateType.Scouting => scoutingState,
            EnemyStateType.TargetLock => targetLockState,
            EnemyStateType.Dead => deadState,
            _ => throw new System.NotImplementedException(),
        };
    }
}

public enum EnemyStateType
{
    Scouting, TargetLock, Dead
}

public enum EnemyType
{
    Trired, Twored, OneRed, BossRed
}