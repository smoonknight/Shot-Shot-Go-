using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(Input))]
public class PlayerController : PlayableCharacterControllerBase
{
    private float attackInterval;
    public ExperienceStat experienceStat;

    public Transform magneticBody;

    [SerializeField]
    private AudioClip deadAudioClip;
    [SerializeField]
    private Canvas onScreenCanvas;
    private Input input;

    public PlayerStateMachine playerStateMachine { get; private set; }

    private TimeChecker attackIntervalTimeChecker = new();

    private List<List<PlayerUpgradePlanPorperty>> listOfPlayerUpgradePlanPorperties = new();

    private bool isExecuteUpgradeStat;

    Collider2D[] colliders = new Collider2D[100];

    bool isWallHanging;

    private void OnDestroy()
    {
        playerStateMachine = null;
    }
    protected override void Awake()
    {
        base.Awake();
        input = GetComponent<Input>();

        playerStateMachine = new();
        playerStateMachine.Initialize(this, PlayerStateType.Play);

        UIManager.Instance.InitializePlayer(this);

        onScreenCanvas.enabled = Application.platform == RuntimePlatform.Android;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        input.JumpAction.performed += JumpActionPerformed;
        input.PauseAction.performed += ValidatePause;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        input.JumpAction.performed -= JumpActionPerformed;
        input.PauseAction.performed -= ValidatePause;
    }

    private void Update()
    {
        playerStateMachine.CurrentState.UpdateState();
    }

    private void FixedUpdate()
    {
        playerStateMachine.CurrentState.FixedUpdateState();
    }

    private void JumpActionPerformed(InputAction.CallbackContext context)
    {
        if (isWallHanging)
        {
            WallJump();
        }
        ValidateDoubleJump();
        TakeJump();
    }

    private void WallJump()
    {
        Vector2 direction = new(isFacingRight ? -1f : 1f, 1f);
        SetForce(direction * wallJumpForce, 2f, true, () => isGrounded || (Mathf.Abs(rigidBody.linearVelocityX) < 0.05f && !isWallHanging));
        JumpAudio();
    }

    private void ValidateDoubleJump()
    {
        if (!enableDoubleJump || isGrounded || isWallHanging)
        {
            return;
        }
        ParticleSpawnerManager.Instance.GetSpawned(ParticleType.DoubleJump, groundCheck.position);
        enableDoubleJump = false;
    }

    private bool ValidateWallHanging()
    {
        bool isTouchingWall = CheckWall();

        if (isTouchingWall && (input.Move.x != 0 || !enableMove) && !isGrounded && rigidBody.linearVelocityY <= 0)
        {
            return true;
        }
        return false;
    }

    private void ValidatePause(InputAction.CallbackContext context)
    {
        if (isExecuteUpgradeStat || playerStateMachine.LatestType != PlayerStateType.Play)
            return;
        GameplayManager.Instance.RaisePause(this);
    }

    private void MagneticCollectable()
    {

        ContactFilter2D filter = new()
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = LayerMaskManager.Instance.collectableMask
        };

        int count = Physics2D.OverlapCircle(magneticBody.position, characterStatProperty.size, filter, colliders);
        if (count == 0)
        {
            return;
        }
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = colliders[i];
            if (collider.TryGetComponent(out IMagneticable component))
            {
                component.Attraction(magneticBody.position);
                if (Vector2.Distance(component.MagneticSource().position, magneticBody.position) < 0.5f)
                {
                    component.OnMagneticClose(this);
                }
            }
        }
    }

    public void AddExperience(int experience)
    {
        float percentage = experienceStat.AddExperienceAndGetRemainingExpPercentage(experience, out bool isLevelUp, out int currentLevel, out int LevelUpCount);
        if (isLevelUp)
        {
            var upgrades = GameplayManager.Instance.GetUpgradeStatsRandomly(LevelUpCount);
            listOfPlayerUpgradePlanPorperties.AddRange(upgrades);
            if (!isExecuteUpgradeStat)
            {
                isExecuteUpgradeStat = true;
                playerStateMachine.SetStateWhenDifference(PlayerStateType.Pause);
                ExecuteUpgradeStat();
            }
            UIManager.Instance.SetLevel(currentLevel);
        }
        UIManager.Instance.SetExperience(percentage);
    }

    public async void ExecuteUpgradeStat()
    {
        Time.timeScale = 0;
        GameManager.Instance.SetCursor(true);

        bool hasStart = false;
        while (listOfPlayerUpgradePlanPorperties.Count > 0)
        {
            var list = listOfPlayerUpgradePlanPorperties[0];
            listOfPlayerUpgradePlanPorperties.RemoveAt(0);

            List<(UpgradeType upgradeType, UpgradeStat upgradeStat, int index)> values = new();

            int index = 0;
            foreach (var item in list)
            {
                UpgradeStat upgradeStat = item.GetUpgradeStatPlan();
                (UpgradeType upgradeType, UpgradeStat upgradeStat, int index) value = new()
                {
                    upgradeType = item.type,
                    upgradeStat = upgradeStat,
                    index = index
                };

                values.Add(value);

                index++;
            }

            int selectedIndex = await UIManager.Instance.StartChooseUpgrade(this, values, hasStart, listOfPlayerUpgradePlanPorperties.Count != 0);

            list[selectedIndex].CurrentPlanIndex++;

            hasStart = true;
        }

        playerStateMachine.SetPrevState(PlayerStateType.Play);

        isExecuteUpgradeStat = false;
    }

    public void AddHealth(int health)
    {
        Health = Mathf.Clamp(Health + health, 0, MaximumHealth);
        UIManager.Instance.SetHealth(this);
    }


    public override void OnZeroHealth()
    {
        if (playerStateMachine.LatestType == PlayerStateType.Play)
            playerStateMachine.SetState(PlayerStateType.Dead);
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        SetImmune(2);
        UIManager.Instance.SetHealth(this);
        ProcessAlphaChangePingPong(1, 0.5f, 1, 0.25f, 4);
    }

    public override void TakeDamage(int damage, Vector2 sourcePosition)
    {
        TakeDamage(damage);
        Vector2 impactForce = new(transform.position.x < sourcePosition.x ? -1 : 1, 1);
        SetForce(impactForce * 5, 0.5f, true);
    }

    protected override float GetMoveTargetDirectionX() => input.Move.x;
    public override bool IsPlayer() => true;
    public override bool HoldingJump() => input.Jump;
    public override StatProperty GetCharacterUpgradeProperty() => GameManager.Instance.GetCopyOfDefaultCharacterUpgradeProperty();
    public override bool CheckOutOfBound() => true;

    public override void OnUpgradeCharacterStatProperty(UpgradeStat upgradeStat)
    {
        switch (upgradeStat.type)
        {
            case UpgradeStatType.health:
                UIManager.Instance.SetMaximumHealth(this);
                AddHealth(Mathf.RoundToInt(upgradeStat.value));
                break;
        }
    }
    #region Pause State

    private void PauseEnter()
    {
        Time.timeScale = 0;
        GameManager.Instance.SetCursor(true);
    }

    private void PauseLeave()
    {
        Time.timeScale = 1;
        GameManager.Instance.SetCursor(false);
    }

    public class PauseState : BaseState<PlayerController>
    {
        public PauseState(PlayerController component) : base(component)
        {
        }

        public override void EnterState()
        {
            component.PauseEnter();
        }

        public override void FixedUpdateState()
        {
        }

        public override void LeaveState()
        {
            component.PauseLeave();
        }

        public override void UpdateState()
        {
        }
    }
    #endregion
    #region Dead State

    private const float waitingToGameOverDuration = 2;
    TimeChecker waitingToGameOverTimeChecker = new();
    bool isWaitingRaiseGameOver;
    private void DeadEnter()
    {
        rigidBody.linearVelocity = Vector2.zero;
        waitingToGameOverTimeChecker.UpdateTime(waitingToGameOverDuration);

        audioSource.clip = deadAudioClip;
        audioSource.Play();

        isWaitingRaiseGameOver = true;

        AudioExtendedManager.Instance.SetMusic(GameplayManager.Instance.endMusicName, false);

        UpdateDeadAnimation(true);
    }

    private void DeadFixedUpdate()
    {
        if (!isWaitingRaiseGameOver || !waitingToGameOverTimeChecker.IsDurationEnd())
        {
            return;
        }

        isWaitingRaiseGameOver = false;
        GameplayManager.Instance.RaiseGameOver(this);
    }

    private void DeadLeave()
    {
        UpdateDeadAnimation(false);
    }

    public class NoneState : BaseState<PlayerController>
    {
        public NoneState(PlayerController component) : base(component)
        {
        }

        public override void EnterState()
        {
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

    public class DeadState : BaseState<PlayerController>
    {
        public DeadState(PlayerController component) : base(component)
        {
        }

        public override void EnterState()
        {
            component.DeadEnter();
        }

        public override void FixedUpdateState()
        {
            component.DeadFixedUpdate();
        }

        public override void LeaveState()
        {
            component.DeadLeave();
        }

        public override void UpdateState()
        {

        }
    }

    #endregion

    #region Play State
    private void PlayStateEnter()
    {
        enableMove = true;
        enableDoubleJump = true;
        rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void PlayStateUpdate()
    {
        isGrounded = CheckGrounded();

        if (isGrounded)
        {
            enableDoubleJump = true;
            coyoteCounter = coyoteTime;
        }
        else
            coyoteCounter -= Time.deltaTime;

        if (enableDoubleJump)
        {
            coyoteCounter = coyoteTime;
        }

        jumpBufferCounter -= Time.deltaTime;

        ValidateJump();

        isWallHanging = ValidateWallHanging();

        ApplyBetterJump();

        UpdateMoveAnimation();
        UpdateJumpAnimation(isGrounded);
        UpdateWallHangAnimation(isWallHanging);

        MagneticCollectable();

        if (input.Fire && attackIntervalTimeChecker.IsDurationEnd())
        {
            Fire();
            attackIntervalTimeChecker.UpdateTime(attackInterval);
        }
    }

    private void PlayStateFixedUpdate()
    {
        ValidateMove();
        rigidBody.constraints = isWallHanging ? RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.FreezeRotation;
    }

    public override void OnSetCharacterStatProperty(StatProperty upgradeProperty)
    {
        attackInterval = Mathf.Max(upgradeProperty.attackInterval, 0.1f);
    }

    public class PlayState : BaseState<PlayerController>
    {
        public PlayState(PlayerController component) : base(component)
        {
        }

        public override void EnterState()
        {
            component.PlayStateEnter();
        }

        public override void FixedUpdateState()
        {
            component.PlayStateFixedUpdate();
        }

        public override void LeaveState()
        {
        }

        public override void UpdateState()
        {
            component.PlayStateUpdate();
        }
    }
    #endregion

    #region Cutscene State
    public class CutsceneState : BaseState<PlayerController>
    {
        public CutsceneState(PlayerController component) : base(component)
        {
        }

        public override void EnterState()
        {
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
    #endregion

    public class PlayerStateMachine : EnumStateMachine<PlayerController, PlayerStateType>
    {
        private CutsceneState cutsceneState;
        private PlayState playState;
        private DeadState deadState;
        private PauseState pauseState;
        private NoneState noneState;

        protected override void InitializeState(PlayerController playerController)
        {
            cutsceneState = new(playerController);
            playState = new(playerController);
            deadState = new(playerController);
            pauseState = new(playerController);
            noneState = new(playerController);
        }

        protected override BaseState<PlayerController> GetState(PlayerStateType type) => type switch
        {
            PlayerStateType.Cutscene => cutsceneState,
            PlayerStateType.Play => playState,
            PlayerStateType.Dead => deadState,
            PlayerStateType.Pause => pauseState,
            PlayerStateType.None => noneState,
            _ => throw new System.NotImplementedException(),
        };
    }
}


public enum PlayerStateType
{
    Cutscene, Play, Dead, Pause, None
}