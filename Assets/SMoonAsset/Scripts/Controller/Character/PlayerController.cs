using SMoonUniversalAsset;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Input))]
public class PlayerController : PlayableCharacterControllerBase
{
    private Input input;

    private PlayerStateMachine playerStateMachine;

    private TimeChecker attackIntervalTimeChecker = new(attackInterval);

    bool isWallHanging;

    protected override void Awake()
    {
        base.Awake();
        input = GetComponent<Input>();

        playerStateMachine = new();
        playerStateMachine.Initialize(this, PlayerStateType.Play);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        input.JumpAction.performed += JumpActionPerformed;
    }

    private void OnDisable()
    {
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
        SetForce(direction * wallJumpForce, 2f, true, () => isGrounded);
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

    protected override float GetMoveTargetVelocityX() => input.Move.x * moveSpeed;
    public override bool IsPlayer() => true;
    public override bool HoldingJump() => input.Jump;
    public override UpgradeProperty GetCharacterUpgradeProperty() => GameManager.Instance.GetCopyOfDefaultCharacterUpgradeProperty();

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

        if (input.Fire && attackIntervalTimeChecker.IsUpdatingTime())
        {
            Fire();
        }
    }

    private void PlayStateFixedUpdate()
    {
        Move();
        rigidBody.constraints = isWallHanging ? RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.FreezeRotation;
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

        protected override void InitializeState(PlayerController playerController)
        {
            cutsceneState = new(playerController);
            playState = new(playerController);
        }

        protected override BaseState<PlayerController> GetState(PlayerStateType type)
        {
            return type switch
            {
                PlayerStateType.Cutscene => cutsceneState,
                PlayerStateType.Play => playState,
                _ => throw new System.NotImplementedException(),
            };
        }
    }
}


public enum PlayerStateType
{
    Cutscene, Play
}