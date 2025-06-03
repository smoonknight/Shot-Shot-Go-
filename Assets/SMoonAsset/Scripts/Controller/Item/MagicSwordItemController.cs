using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

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
            Quaternion.Lerp(transform.rotation, Quaternion.identity, inertiaValue));
    }

    public override void Initialize(PlayableCharacterControllerBase playableCharacterControllerBase, bool isPlayerAsMaster)
    {
        base.Initialize(playableCharacterControllerBase, isPlayerAsMaster);
        RandomizeStandPosition();
        onAttackingTask = GetAttackTask(itemBase.type);
    }

    private void RandomizeStandPosition()
    {
        float x = Random.Range(0.8f, 1f) * (Random.value > 0.5f ? 1 : -1);
        float y = Random.Range(0.5f, 1.5f);
        standPosition = new Vector2(x, y);
    }

    private UnityAction GetAttackTask(MagicSwordItemType type) => type switch
    {
        MagicSwordItemType.Common => OnMasterDirection,
        MagicSwordItemType.OnTarget => () => AttackDetectionTarget(OnTargetAttack),
        MagicSwordItemType.Slashing => () => AttackDetectionTarget(SlasherAttack),
        _ => throw new NotImplementedException(),
    };

    public override void AttackAction()
    {
        onAttackingTask.Invoke();
    }

    private void AttackDetectionTarget(Func<Collider2D, UniTask> onAttackingTarget)
    {
        Collider2D collider = Physics2D.OverlapCircle(transform.position, detectionRange, isPlayerAsMaster ? LayerMaskManager.Instance.enemyMask : LayerMaskManager.Instance.playerMask);

        if (collider == null)
        {
            Debug.LogWarning($"Tidak terdeteksi");
            return;
        }

        AttackingTarget(collider, onAttackingTarget);
    }

    private async void AttackingTarget(Collider2D target, Func<Collider2D, UniTask> onAttackingTarget)
    {
        isAttacking = true;

        await onAttackingTarget.Invoke(target);

        isAttacking = false;
    }

    private async void OnMasterDirection()
    {
        isAttacking = true;
        Vector3 direction = playableCharacter.isFacingRight ? Vector3.right : Vector3.left;
        Vector3 targetPosition = transform.position + direction * travelRange;

        await TravelToTarget(targetPosition, () =>
        {
            RandomizeStandPosition();
            transform.position = GetStandPosition();
        });

        isAttacking = false;
    }

    private async UniTask OnTargetAttack(Collider2D target)
    {
        await TravelToTarget(target.transform.position);
    }

    private async UniTask TravelToTarget(Vector3 targetPosition, UnityAction onTravelFinish = null)
    {
        float totalTravel = 0f;
        float time = 0;

        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
        transform.rotation = targetRotation;

        while (time < maximumAttackDuration)
        {
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

    private async UniTask SlasherAttack(Collider2D target)
    {
        float totalTravel = 0f;

        Vector3 targetPosition = target.transform.position;
        Quaternion targetRotation;

        float time = 0;

        while (time < maximumAttackDuration)
        {
            Vector2 direction = (targetPosition - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
            Quaternion newRotation = targetRotation;

            Vector2 oldPosition = transform.position;
            Vector2 newPosition = Vector2.MoveTowards(transform.position, targetPosition, swordTravelSpeed * Time.deltaTime);

            totalTravel += Vector2.Distance(oldPosition, newPosition);

            if (totalTravel >= detectionRange)
                break;

            transform.SetPositionAndRotation(
                newPosition,
                newRotation
            );

            time += Time.deltaTime;
            await UniTask.Yield();
        }
    }
}