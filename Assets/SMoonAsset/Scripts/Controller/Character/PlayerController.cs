using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Input))]
public class PlayerController : CharacterControllerBase
{
    private bool isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private Input input;

    private PlayerStateMachine playerStateMachine;

    float currentVelocityX;

    const float accelerationSmoothRate = 0.1f;

    protected override void Awake()
    {
        base.Awake();
        input = GetComponent<Input>();

        playerStateMachine = new();
        playerStateMachine.Initialize(this, PlayerStateType.Play);
    }

    private void OnEnable()
    {
        input.JumpAction.performed += ctx => jumpBufferCounter = jumpBufferTime;
    }

    private void OnDisable()
    {
        input.JumpAction.performed -= ctx => jumpBufferCounter = jumpBufferTime;
    }

    private void Update()
    {
        playerStateMachine.CurrentState.UpdateState();
    }

    private void FixedUpdate()
    {
        playerStateMachine.CurrentState.FixedUpdateState();
    }

    private void Jump()
    {
        rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocity.x, jumpForce);
        coyoteCounter = 0;
    }

    private void ApplyBetterJump()
    {
        if (rigidBody.linearVelocity.y < 0)
            rigidBody.linearVelocityY += (fallMultiplier - 1) * Physics2D.gravity.y * Time.deltaTime;
        else if (rigidBody.linearVelocity.y > 0 && !input.Jump)
            rigidBody.linearVelocityY += (lowJumpMultiplier - 1) * Physics2D.gravity.y * Time.deltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }

    #region Play State
    private void PlayStateUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, LayerMaskManager.Instance.groundLayer);

        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            Jump();
            jumpBufferCounter = 0;
        }

        ApplyBetterJump();

        UpdateMoveAnimation();
        UpdateJumpAnimation(isGrounded);
    }

    private void PlayStateFixedUpdate()
    {
        float targetVelocityX = input.Move.x * moveSpeed;
        float newVelocityX = Mathf.SmoothDamp(rigidBody.linearVelocityX, targetVelocityX, ref currentVelocityX, accelerationSmoothRate);

        rigidBody.linearVelocity = new Vector2(newVelocityX, rigidBody.linearVelocityY);
        if (targetVelocityX != 0)
            SetDirection(targetVelocityX > 0);
    }

    public class PlayState : BaseState<PlayerController>
    {
        public PlayState(PlayerController component) : base(component)
        {
        }

        public override void EnterState()
        {
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