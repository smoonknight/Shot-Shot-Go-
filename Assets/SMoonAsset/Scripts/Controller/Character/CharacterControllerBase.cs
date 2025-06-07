using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
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
    protected float fallMultiplier = 1f;
    [SerializeField]
    protected float lowJumpMultiplier = 2f;

    [Header("Ground Check")]
    [SerializeField]
    protected Transform groundCheck;
    [SerializeField]
    protected float groundRadius = 0.2f;
    [SerializeField]
    protected Rigidbody2D rigidBody;
    [SerializeField]
    protected Animator animator;

    [Header("Wall Hang")]
    [SerializeField]
    protected float wallCheckDistance = 0.5f;
    [SerializeField]
    protected float wallJumpForce = 8;

    [Header("Collider")]
    [SerializeField]
    protected Collider2D characterCollider;

    [Header("Sound")]
    [SerializeField]
    protected AudioSource audioSource;
    [SerializeField]
    protected AudioClipSamples jumpClipSamples;

    protected bool isGrounded;
    protected float coyoteCounter;
    protected float jumpBufferCounter;

    public Vector2 ColliderCenter => characterCollider.bounds.center;
    public float HalfHeight => characterCollider.bounds.size.y / 2f;

    protected List<SpriteRenderer> spriteRenderers = new();

    float currentVelocityX;

    int moveHash;

    int isJumpHash;
    int isWallHangHash;

    int jumpHash;
    int wallHangHash;

    int isDeadHash;


    float lastMoveSmooth;
    float lastJumpSmooth;

    protected Vector3 initialScale;

    protected float horizontalScale = 1;

    [SerializeField]
    [ReadOnly]
    public bool isFacingRight = true;
    [SerializeField]
    [ReadOnly]
    protected bool enableMove = true;
    [SerializeField]
    [ReadOnly]
    protected bool enableDoubleJump;

    public bool IsDead { protected set; get; }

    protected const float inertiaRate = 5f;
    const float accelerationSmoothRate = 0.1f;
    const float changeDirectionTime = 0.1f;

    protected CancellationTokenSource jumpCancellationTokenSource;
    protected CancellationTokenSource forceCancellationTokenSource;
    protected CancellationTokenSource colorCancellationTokenSource;
    protected CancellationTokenSource alphaCancellationTokenSource;

    protected virtual void OnDisable()
    {
        jumpCancellationTokenSource?.Cancel();
        forceCancellationTokenSource?.Cancel();
        colorCancellationTokenSource?.Cancel();
        alphaCancellationTokenSource?.Cancel();
    }

    protected virtual void Awake()
    {
        rigidBody.GetComponent<Rigidbody2D>();

        moveHash = Animator.StringToHash("Move");

        isJumpHash = Animator.StringToHash("Is Jump");
        isWallHangHash = Animator.StringToHash("Is Wall Hang");

        jumpHash = Animator.StringToHash("Jump");
        wallHangHash = Animator.StringToHash("Wall Hang");

        isDeadHash = Animator.StringToHash("Is Dead");

        spriteRenderers = TransformHelper.GetComponentsRecursively<SpriteRenderer>(transform);

        SetInitial();
    }

    public void SetInitial()
    {
        initialScale = transform.localScale;
        horizontalScale = initialScale.x;
    }

    protected void ValidateMove()
    {
        if (!enableMove || IsDead)
        {
            return;
        }
        Move();
    }

    protected virtual void Move()
    {
        float targetVelocityX = GetMoveTargetDirectionX() * moveSpeed;
        float newVelocityX = Mathf.SmoothDamp(rigidBody.linearVelocityX, targetVelocityX, ref currentVelocityX, accelerationSmoothRate);

        rigidBody.linearVelocity = new Vector2(newVelocityX, rigidBody.linearVelocityY);
        SetDirection(targetVelocityX);
    }

    protected void Jump()
    {
        rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocity.x, jumpForce);
        coyoteCounter = 0;
        JumpAudio();
    }

    protected void SetDirectionFromMoveTargetVelocity()
    {
        SetDirection(GetMoveTargetDirectionX());
    }

    protected void SetDirection(float velocityX)
    {
        if (velocityX != 0)
            SetDirection(velocityX > 0);
    }

    protected void SetDirection(bool isRightDirection)
    {
        if (isFacingRight == isRightDirection)
        {
            return;
        }
        isFacingRight = isRightDirection;

        jumpCancellationTokenSource?.Cancel();
        jumpCancellationTokenSource = new();
        SetDirectionSmoothly(isRightDirection, jumpCancellationTokenSource.Token).Forget();
    }

    private async UniTaskVoid SetDirectionSmoothly(bool isRightDirection, CancellationToken cancellationToken)
    {
        float latestScaleX = transform.localScale.x;

        float time = 0;

        float targetScaleX = isRightDirection ? horizontalScale : horizontalScale * -1;
        Vector2 scale;
        while (time < changeDirectionTime && !cancellationToken.IsCancellationRequested)
        {
            float percentage = time / changeDirectionTime;
            scale = transform.localScale;
            scale.x = Mathf.Lerp(latestScaleX, targetScaleX, percentage);
            transform.localScale = scale;
            time += Time.deltaTime;
            await UniTask.Yield();
        }

        scale = transform.localScale;
        scale.x = targetScaleX;
        transform.localScale = scale;
    }

    protected bool CheckWall()
    {
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        return Physics2D.Raycast(transform.position, direction, wallCheckDistance, LayerMaskManager.Instance.wallLayer);
    }

    protected void SetForce(Vector2 force, float duration, bool changeDirection, Func<bool> cancelFunc = null)
    {
        forceCancellationTokenSource?.Cancel();
        forceCancellationTokenSource = new();

        StartSetForce(force, duration, changeDirection, forceCancellationTokenSource.Token, cancelFunc).Forget();
    }

    private async UniTaskVoid StartSetForce(Vector2 force, float duration, bool changeDirection, CancellationToken cancellationToken, Func<bool> cancelFunc = null)
    {
        if (changeDirection) SetDirection(force.x > 0);

        await UniTask.Yield();

        enableMove = false;
        rigidBody.linearVelocity = Vector2.zero;
        rigidBody.AddForce(force, ForceMode2D.Impulse);

        float timer = 0f;
        while (timer < duration)
        {
            if (cancellationToken.IsCancellationRequested || (cancelFunc?.Invoke() ?? false)) break;
            timer += Time.deltaTime;
            await UniTask.Yield();
        }

        enableMove = true;
    }

    public async void ProcessColorChangePingPong(Color colorA, Color colorB, Color finalColor, float changeInterval, int totalPingPong)
    {
        CancellationToken cancellationToken = colorCancellationTokenSource.ResetToken();

        for (int i = 0; i < totalPingPong; i++)
        {
            await LerpGeneric(colorA, colorB, changeInterval, Color.Lerp, ColorChange, cancellationToken);
            await LerpGeneric(colorB, colorA, changeInterval, Color.Lerp, ColorChange, cancellationToken);
        }

        ColorChange(finalColor);
    }

    public async void ProcessAlphaChangePingPong(float valueA, float valueB, float finalValue, float changeInterval, int totalPingPong)
    {
        CancellationToken cancellationToken = alphaCancellationTokenSource.ResetToken();

        for (int i = 0; i < totalPingPong; i++)
        {
            await LerpGeneric(valueA, valueB, changeInterval, Mathf.Lerp, AlphaChange, cancellationToken);
            await LerpGeneric(valueB, valueA, changeInterval, Mathf.Lerp, AlphaChange, cancellationToken);
        }

        AlphaChange(finalValue);
    }



    /// <summary>
    /// Generic Lerp Coroutine.
    /// </summary>
    /// <typeparam name="T">Tipe data yang ingin diinterpolasi</typeparam>
    /// <param name="from">Nilai awal</param>
    /// <param name="to">Nilai akhir</param>
    /// <param name="duration">Waktu interpolasi</param>
    /// <param name="lerpFunc">Fungsi Lerp, contoh: Color.Lerp / Mathf.Lerp</param>
    /// <param name="applyFunc">Fungsi untuk menerapkan hasil interpolasi</param>
    /// <param name="cancellationToken">Token pembatalan</param>
    protected async UniTask LerpGeneric<T>(
        T from,
        T to,
        float duration,
        Func<T, T, float, T> lerpFunc,
        Action<T> applyFunc,
        CancellationToken cancellationToken)
    {
        float time = 0f;

        while (time < duration)
        {
            if (cancellationToken.IsCancellationRequested) return;

            float t = time / duration;
            applyFunc(lerpFunc(from, to, t));

            await UniTask.Yield(PlayerLoopTiming.Update);
            time += Time.deltaTime;
        }

        applyFunc(to);
    }

    public void ColorChange(Color color)
    {
        foreach (var sr in spriteRenderers)
        {
            if (this == null)
                break;
            sr.color = color;
        }
    }

    public void AlphaChange(float opacity)
    {
        foreach (var sr in spriteRenderers)
        {
            if (this == null)
                break;
            sr.SetOpacity(opacity);
        }
    }

    public void ForceChangePosition(Vector2 position)
    {
        transform.position = position;
        rigidBody.linearVelocity = Vector3.zero;
    }

    protected void UpdateMoveAnimation()
    {
        lastMoveSmooth = Mathf.Lerp(lastMoveSmooth, Mathf.Abs(rigidBody.linearVelocity.x) / moveSpeed, inertiaRate * Time.deltaTime);
        animator.SetFloat(moveHash, lastMoveSmooth);
    }
    protected void UpdateJumpAnimation(bool isGrounded)
    {
        UpdateAnimation(isJumpHash, jumpHash, ref lastJumpSmooth, rigidBody.linearVelocity.normalized.y, !isGrounded);
    }

    protected void UpdateWallHangAnimation(bool isWallHang)
    {
        animator.SetBool(isWallHangHash, isWallHang);
    }

    protected void UpdateDeadAnimation(bool isDead)
    {
        animator.SetBool(isDeadHash, isDead);
    }

    protected void UpdateAnimation(int isConditionHash, int isValueHash, ref float smoothValue, float targetValue, bool condition)
    {
        animator.SetBool(isConditionHash, condition);
        if (!condition)
        {
            smoothValue = 0;
            return;
        }
        smoothValue = Mathf.Lerp(smoothValue, targetValue, inertiaRate * Time.deltaTime);
        animator.SetFloat(isValueHash, smoothValue);
    }

    protected void JumpAudio()
    {
        audioSource.clip = jumpClipSamples.GetClip();
        audioSource.Play();
    }

    protected abstract float GetMoveTargetDirectionX();
}