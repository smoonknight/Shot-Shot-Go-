using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Input))]
public class PlayerController : PlayableCharacterControllerBase
{
    private float attackInterval;
    public ExperienceStat experienceStat;

    public Transform magneticBody;

    private Input input;

    private PlayerStateMachine playerStateMachine;

    private TimeChecker attackIntervalTimeChecker = new();

    private List<List<PlayerUpgradePlanPorperty>> listOfPlayerUpgradePlanPorperties = new();

    private bool isExecuteUpgradeStat;

    Collider2D[] colliders = new Collider2D[100];

    bool isWallHanging;

    protected override void Awake()
    {
        base.Awake();
        input = GetComponent<Input>();

        playerStateMachine = new();
        playerStateMachine.Initialize(this, PlayerStateType.Play);

        UIManager.Instance.InitializePlayer(this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        input.JumpAction.performed += JumpActionPerformed;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        input.JumpAction.performed -= JumpActionPerformed;
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
    }

    private void ValidateDoubleJump()
    {
        if (!enableDoubleJump || isGrounded || isWallHanging)
        {
            return;
        }
        ParticleSpawnerManager.Instance.GetSpawned(ParticleType.ImpactGroundHit, groundCheck.position);
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
        experienceStat.AddExperience(experience, out bool isLevelUp, out int currentLevel, out int LevelUpCount);
        if (isLevelUp)
        {
            var upgrades = GameplayManager.Instance.GetUpgradeStatsRandomly(LevelUpCount);
            listOfPlayerUpgradePlanPorperties.AddRange(upgrades);
            if (!isExecuteUpgradeStat)
            {
                isExecuteUpgradeStat = true;
                ExecuteUpgradeStat();
            }
        }
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

            await UIManager.Instance.StartChooseUpgrade(this, list, hasStart, listOfPlayerUpgradePlanPorperties.Count != 0);
            hasStart = true;
        }

        Time.timeScale = 1;
        GameManager.Instance.SetCursor(false);

        isExecuteUpgradeStat = false;
    }

    public void AddHealth(int health)
    {
        Health = Mathf.Clamp(Health + health, 0, MaximumHealth);
        UIManager.Instance.SetHealth(this);
    }


    public override void OnZeroHealth()
    {
        playerStateMachine.SetStateWhenDifference(PlayerStateType.Dead);
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
    #region Dead State

    private const float waitingToGameOverDuration = 2;
    TimeChecker waitingToGameOverTimeChecker = new();

    bool hasRaiseGameOver;

    private void DeadEnter()
    {
        rigidBody.linearVelocity = Vector2.zero;
        hasRaiseGameOver = false;
        waitingToGameOverTimeChecker.UpdateTime(waitingToGameOverDuration);

        UpdateDeadAnimation(true);
    }

    private void DeadFixedUpdate()
    {
        if (hasRaiseGameOver || !waitingToGameOverTimeChecker.IsDurationEnd())
        {
            return;
        }

        hasRaiseGameOver = true;
        GameManager.Instance.RaiseGameOver();
    }

    private void DeadLeave()
    {
        UpdateDeadAnimation(false);
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

        protected override void InitializeState(PlayerController playerController)
        {
            cutsceneState = new(playerController);
            playState = new(playerController);
            deadState = new(playerController);
        }

        protected override BaseState<PlayerController> GetState(PlayerStateType type) => type switch
        {
            PlayerStateType.Cutscene => cutsceneState,
            PlayerStateType.Play => playState,
            PlayerStateType.Dead => deadState,
            _ => throw new System.NotImplementedException(),
        };
    }
}


public enum PlayerStateType
{
    Cutscene, Play,
    Dead
}