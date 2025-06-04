using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using UniTaskExtensions = SMoonUniversalAsset.UniTaskExtensions;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MagicSwordItemController : WeaponItemController<MagicSwordItem>
{
    const float swordTravelSpeed = 20;
    private BoxCollider2D boxCollider2D;
    private Rigidbody2D rigidBody2D;
    const float detectionRange = 15;
    const float travelRange = 30;
    const float inertiaRate = 10;

    const float maximumAttackDuration = 10;

    Vector3 standPosition;
    Quaternion standRotation = Quaternion.identity;

    UnityAction onAttackingTask;

    private Vector3 GetStandPosition() => master.transform.position + standPosition;

    protected void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        boxCollider2D.isTrigger = true;
    }

    private void Update()
    {
        if (master == null)
        {
            Disable();
            return;
        }

        if (isAttacking)
        {
            return;
        }

        float inertiaValue = inertiaRate * Time.deltaTime;
        Vector3 targetPosition = GetStandPosition();
        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, targetPosition, inertiaValue),
            Quaternion.Lerp(transform.rotation, standRotation, inertiaValue));

        //TODO Buat game ini jadi rogue like, hilangkan magic sword dropped item
    }

    public override void Initialize(PlayableCharacterControllerBase playableCharacterControllerBase, bool isPlayerAsMaster, Vector3 initialPosition, UpgradeProperty upgradeProperty)
    {
        base.Initialize(playableCharacterControllerBase, isPlayerAsMaster, initialPosition, upgradeProperty);
        RandomizeStandPosition();
        onAttackingTask = GetAttackTask(itemBase.type);
    }

    private void RandomizeStandPosition()
    {
        float x = Random.Range(0.8f, 1f) * (Random.value > 0.5f ? 1 : -1);
        float y = Random.Range(0.5f, 1.5f);
        standPosition = new Vector2(x, y);
    }

    private void ResetPositionRandomizeAndScale()
    {
        RandomizeStandPosition();
        transform.position = GetStandPosition();
        transform.localScale = Vector3.one;
    }

    private UnityAction GetAttackTask(MagicSwordItemType type) => type switch
    {
        MagicSwordItemType.Common => OnMasterDirection,
        MagicSwordItemType.OnTarget => () => AttackDetectionTarget(OnTargetAttack),
        MagicSwordItemType.Slashing => () => AttackDetectionTarget(SlasherAttack, ResetPositionRandomizeAndScale),
        _ => throw new NotImplementedException(),
    };

    public override void AttackAction()
    {
        onAttackingTask.Invoke();
    }

    private void AttackDetectionTarget(Func<Collider2D, CancellationToken, UniTask> onAttackingTarget, UnityAction onClearAttacking = null)
    {
        Collider2D collider = Physics2D.OverlapCircle(transform.position, detectionRange, targetMask);

        if (collider == null)
        {
            OnMasterDirection();
            return;
        }

        AttackingTarget(collider, onAttackingTarget, onClearAttacking);
    }

    private async void AttackingTarget(Collider2D target, Func<Collider2D, CancellationToken, UniTask> onAttackingTarget, UnityAction onClearAttacking = null)
    {
        attackingCancellationTokenSource?.Cancel();
        isAttacking = true;

        attackingCancellationTokenSource = new();

        await onAttackingTarget.Invoke(target, attackingCancellationTokenSource.Token);

        onClearAttacking?.Invoke();

        isAttacking = false;
    }

    private async void OnMasterDirection()
    {
        isAttacking = true;
        Vector3 direction = playableCharacter.isFacingRight ? Vector3.right : Vector3.left;
        Vector3 targetPosition = transform.position + direction * travelRange;

        attackingCancellationTokenSource?.Cancel();
        attackingCancellationTokenSource = new();

        await TravelToTarget(targetPosition, attackingCancellationTokenSource.Token, ResetPositionRandomizeAndScale);

        isAttacking = false;
    }

    private async UniTask OnTargetAttack(Collider2D target, CancellationToken cancellationToken)
    {
        await TravelToTarget(target.transform.position, cancellationToken);
    }

    private async UniTask TravelToTarget(Vector3 targetPosition, CancellationToken cancellationToken, UnityAction onTravelFinish = null)
    {
        float totalTravel = 0f;
        float time = 0;

        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
        transform.rotation = targetRotation;

        while (time < maximumAttackDuration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            Vector2 oldPosition = transform.position;
            Vector2 moveVector = swordTravelSpeed * Time.deltaTime * (Vector2)transform.up;
            Vector2 newPosition = oldPosition + moveVector;

            totalTravel += moveVector.magnitude;

            if (totalTravel >= travelRange)
                break;

            transform.position = newPosition;

            time += Time.deltaTime;
            await UniTask.Yield();
        }

        onTravelFinish?.Invoke();
    }

    private async UniTask SlasherAttack(Collider2D target, CancellationToken cancellationToken)
    {
        float totalTravel = 0f;
        Vector3 targetPosition = target.transform.position;
        float time = 0f;
        bool slashingAttack = false;

        bool isMasterFacingRight = playableCharacter.isFacingRight;

        transform.rotation = Quaternion.identity;

        while (time < maximumAttackDuration)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            Vector3 currentPosition = transform.position;
            Vector2 newPosition = Vector2.MoveTowards(currentPosition, targetPosition, swordTravelSpeed * Time.deltaTime);

            totalTravel += Vector2.Distance(currentPosition, newPosition);
            if (totalTravel >= detectionRange || Vector2.Distance(newPosition, targetPosition) < 0.5f)
            {
                slashingAttack = true;
                break;
            }

            transform.position = newPosition;

            time += Time.deltaTime;
            await UniTask.Yield();
        }

        if (!slashingAttack)
            return;

        transform.rotation = Quaternion.identity;
        time = 0f;

        float scaleDuration = 1f;
        while (time < scaleDuration)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            float percentage = time / scaleDuration;
            Vector3 scaleLerp = Vector3.Lerp(Vector3.one, Vector3.one * 4, percentage);
            transform.localScale = scaleLerp;
            time += Time.deltaTime;
            await UniTask.Yield();
        }

        float slashingDuration = 0.5f;
        time = 0f;
        float targetAngle = isMasterFacingRight ? -360 : 360;
        while (time < slashingDuration)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            float angle = Mathf.Lerp(0f, targetAngle, time / slashingDuration);
            transform.rotation = Quaternion.Euler(0, 0, angle);
            time += Time.deltaTime;
            await UniTask.Yield();
        }

        await UniTaskExtensions.DelayWithCancel(0.5f, cancellationToken);
    }
}