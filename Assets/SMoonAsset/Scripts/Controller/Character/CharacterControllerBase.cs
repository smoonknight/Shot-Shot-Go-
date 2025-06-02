using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class CharacterControllerBase : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    protected float moveSpeed = 8f;
    [SerializeField]
    protected float acceleration = 10f;
    [SerializeField]
    protected float deceleration = 10f;

    [Header("Jump")]
    [SerializeField]
    protected float jumpForce = 16f;
    [SerializeField]
    protected float coyoteTime = 0.2f;
    [SerializeField]
    protected float jumpBufferTime = 0.15f;
    [SerializeField]
    protected float fallMultiplier = 2f;
    [SerializeField]
    protected float lowJumpMultiplier = 3f;

    [Header("Ground Check")]
    [SerializeField]
    protected Transform groundCheck;
    [SerializeField]
    protected float groundRadius = 0.2f;
    [SerializeField]
    protected Rigidbody2D rigidBody;
    [SerializeField]
    protected Animator animator;

    int moveHash;
    int isJumpHash;
    int jumpHash;

    float lastMoveSmooth;
    float lastJumpSmooth;

    protected bool latestIsRightDirection = true;

    protected const float inertiaRate = 5f;

    const float changeDirectionTime = 0.1f;

    private CancellationTokenSource jumpCancellationTokenSource;

    protected virtual void Awake()
    {
        rigidBody.GetComponent<Rigidbody2D>();

        moveHash = Animator.StringToHash("Move");
        isJumpHash = Animator.StringToHash("Is Jump");
        jumpHash = Animator.StringToHash("Jump");
    }

    protected void SetDirection(bool isRightDirection)
    {
        if (latestIsRightDirection == isRightDirection)
        {
            return;
        }
        latestIsRightDirection = isRightDirection;

        jumpCancellationTokenSource?.Cancel();
        jumpCancellationTokenSource = new();
        SetDirectionSmoothly(isRightDirection, jumpCancellationTokenSource.Token).Forget();
    }

    private async UniTaskVoid SetDirectionSmoothly(bool isRightDirection, CancellationToken cancellationToken)
    {
        float latestScaleX = transform.localScale.x;

        float time = 0;
        float targetScaleX = isRightDirection ? 1 : -1;
        while (time < changeDirectionTime)
        {
            float percentage = time / changeDirectionTime;
            Vector2 scale = transform.localScale;
            scale.x = Mathf.Lerp(latestScaleX, targetScaleX, percentage);
            transform.localScale = scale;
            time += Time.deltaTime;
            await UniTask.Yield(cancellationToken);
        }
    }

    protected void UpdateMoveAnimation()
    {
        lastMoveSmooth = Mathf.Lerp(lastMoveSmooth, Mathf.Abs(rigidBody.linearVelocity.x) / moveSpeed, inertiaRate * Time.deltaTime);
        animator.SetFloat(moveHash, lastMoveSmooth);
    }
    protected void UpdateJumpAnimation(bool isGrounded)
    {
        animator.SetBool(isJumpHash, !isGrounded);
        if (isGrounded)
        {
            return;
        }
        lastJumpSmooth = Mathf.Lerp(lastJumpSmooth, rigidBody.linearVelocity.normalized.y, inertiaRate * Time.deltaTime);
        animator.SetFloat(jumpHash, lastJumpSmooth);
    }
}