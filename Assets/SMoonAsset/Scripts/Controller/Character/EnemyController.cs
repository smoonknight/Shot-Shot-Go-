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

    private bool holdingJump;
    private bool isWantToMoving;

    public override UpgradeProperty GetCharacterUpgradeProperty() => GameManager.Instance.GetCopyOfDefaultEnemyCharacterUpgradeProperty(type).upgradeProperty;

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

    private Vector3 targetMovePosition;
    private FillChecker wantToJumpFillChecker = new(1);

    CancellationTokenSource squashCancellationTokenSource;
    CancellationTokenSource holdingJumpRandomizerCancellationTokenSource;

    TimeChecker trampolineTakeDamageTimeChecker = new(trampolineTakeDamageCooldownDuration, false);
    TimeChecker interestTimeChecker = new();

    private PlayerController playerController;

    protected override void Awake()
    {
        base.Awake();
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
        {
            return 0;
        }
        float deltaX = targetMovePosition.x - groundCheck.position.x;

        if (Mathf.Abs(deltaX) < 0.1f)
        {
            isWantToMoving = false;
            return 0;
        }

        int direction = deltaX > 0 ? 1 : -1;

        RaycastHit2D groundHit = GetRaycastHit2DOnFrontGround(direction, groundRadius, 1);

        if (isGrounded && !groundHit)
        {
            if (wantToJumpFillChecker.PushFill(Time.deltaTime))
            {
                ProcessHoldingJumpRandomize(holdingJumpRandomizerCancellationTokenSource.ResetToken());
                SetForce(new Vector2(direction, 1) * jumpForce, 2f, true, () => isGrounded || (rigidBody.linearVelocityX == 0));
            }

            return 0;
        }
        else
        {
            float deltaY = targetMovePosition.y - groundCheck.position.y;
            if (Mathf.Abs(deltaY).IsBetween(minimumWantToJumpTolerance, maximumWantToJumpTolerance))
            {
                if (wantToJumpFillChecker.PushFill(Time.deltaTime))
                {
                    ProcessHoldingJumpRandomize(holdingJumpRandomizerCancellationTokenSource.ResetToken());
                    SetForce(new Vector2(direction, 1) * jumpForce, 2f, true, () => isGrounded || (rigidBody.linearVelocityX == 0));
                }
            }
            else
            {
                wantToJumpFillChecker.ReduceFill(Time.deltaTime);
            }
        }

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

    protected bool TryGetPlayerPosition()
    {
        RaycastHit2D raycastHit2D = Physics2D.CircleCast(groundCheck.position, playerDetection, Vector2.zero, 0, LayerMaskManager.Instance.playerMask);
        if (raycastHit2D.collider != null && raycastHit2D.collider.TryGetComponent(out playerController))
        {
            targetMovePosition = playerController.transform.position;
            return true;
        }
        return false;
    }

    protected Vector2 GetSamplePositionByGameMode()
    {
        return GameManager.Instance.currentGameModeType switch
        {
            GameModeType.Normal => throw new System.NotImplementedException(),
            GameModeType.Rogue => RogueManager.Instance.GetSampleSpawnPosition(),
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

        Vector3 finalScale = transform.localScale;
        finalScale.y = initialScale.y;
        transform.localScale = finalScale;
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

    public override async void OnZeroHealth()
    {
        CancellationToken cancellationToken = alphaCancellationTokenSource.ResetToken();
        await LerpGeneric(1f, 0f, 0.5f, Mathf.Lerp, AlphaChange, cancellationToken);
        gameObject.SetActive(false);
    }

    public override bool IsPlayer() => true;
    public override bool HoldingJump() => holdingJump;
    public override bool CheckOutOfBound() => true;

    #region Target Lock State

    private void TargetLockUpdate()
    {
        isGrounded = CheckGrounded();
        ApplyBetterJump();

        UpdateMoveAnimation();
        UpdateJumpAnimation(isGrounded);
    }

    private void TargetLockFixedUpdate()
    {
        ValidateMove();
        if (playerController != null && Vector2.Distance(groundCheck.position, playerController.transform.position) < playerDetection)
        {
            isWantToMoving = true;
            targetMovePosition = playerController.transform.position;
            return;
        }
        enemyStateMachine.SetState(EnemyStateType.Scouting);
    }
    public class TargetLockState : BaseState<EnemyController>
    {
        public TargetLockState(EnemyController component) : base(component)
        {
        }

        public override void EnterState()
        {
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
        protected override void InitializeState(EnemyController component)
        {
            scoutingState = new(component);
            targetLockState = new(component);
        }

        protected override BaseState<EnemyController> GetState(EnemyStateType type) => type switch
        {
            EnemyStateType.Scouting => scoutingState,
            EnemyStateType.TargetLock => targetLockState,
            _ => throw new System.NotImplementedException(),
        };
    }
}

public enum EnemyStateType
{
    Scouting, TargetLock,
}

public enum EnemyType
{
    Trired
}