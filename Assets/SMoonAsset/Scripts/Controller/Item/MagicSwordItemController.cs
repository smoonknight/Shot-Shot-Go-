using System;
using System.Collections.Generic;
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
    private BoxCollider2D boxCollider2D;
    private Rigidbody2D rigidBody2D;

    [SerializeField]
    MagicSwordStateMachine magicSwordStateMachine;

    const float detectionRange = 15;
    const float travelRange = 30;
    const float inertiaRate = 10;

    const float maximumAttackDuration = 10;

    Vector3 standPosition;
    Quaternion standRotation = Quaternion.identity;

    UnityAction onAttackingTask;

    bool startDetectTarget;

    IDamageable damageable;
    readonly HashSet<IDamageable> alreadyDamaged = new();

    static readonly Collider2D[] colliderBuffer = new Collider2D[16];

    void OnTriggerEnter2D(Collider2D collision) => OnTriggerEvent2D(collision);
    void OnTriggerStay2D(Collider2D collision) => OnTriggerEvent2D(collision);

    void OnTriggerEvent2D(Collider2D collision)
    {
        if (ValidateTriggerEnter2D(collision) && startDetectTarget)
        {
            collision.TryGetComponent(out damageable);
        }
    }

    bool ValidateTriggerEnter2D(Collider2D collider2D) => targetMask.CompareWithLayerIndex(collider2D.gameObject.layer);

    private bool TryGetStandPosition(out Vector2 position)
    {
        if (master == null)
        {
            position = Vector2.zero;
            return false;
        }
        position = master.transform.position + standPosition;
        return true;
    }

    protected void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        boxCollider2D.isTrigger = true;

        magicSwordStateMachine = new();
        magicSwordStateMachine.Initialize(this, MagicSwordStateType.Active);
    }

    void OnDisable()
    {
        attackingCancellationTokenSource?.Cancel();
    }

    private void Update()
    {
        if (master == null || !master.gameObject.activeInHierarchy)
        {
            Disable();
            return;
        }

        if (isAttacking)
        {
            return;
        }

        magicSwordStateMachine.CurrentState.UpdateState();
    }

    private void FixedUpdate()
    {
        magicSwordStateMachine.CurrentState.FixedUpdateState();
    }

    public void Reinitialize(MagicSwordItem itemBase)
    {
        SetItemBase(itemBase);
        magicSwordStateMachine.SetState(MagicSwordStateType.Active);
    }

    public override void Initialize(PlayableCharacterControllerBase playableCharacterControllerBase, bool isPlayerAsMaster, Vector3 initialPosition, StatProperty upgradeProperty)
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
        if (TryGetStandPosition(out Vector2 position))
            transform.position = position;
        ResetScale();
    }

    private void ResetScale()
    {
        transform.localScale = Vector3.one;
    }

    private UnityAction GetAttackTask(MagicSwordItemType type) => type switch
    {
        MagicSwordItemType.Common => OnMasterDirection,
        MagicSwordItemType.OnTarget => () => AttackDetectionTarget(OnTargetAttack, ResetPositionRandomizeAndScale),
        MagicSwordItemType.Slashing => () => AttackDetectionTarget(SlasherAttack, ResetPositionRandomizeAndScale),
        _ => throw new NotImplementedException(),
    };

    public override void AttackAction()
    {
        damageable = null;
        onAttackingTask.Invoke();
    }

    public Collider2D GetNearestTarget(Vector2 origin, float radius, LayerMask targetMask)
    {
        ContactFilter2D filter = new()
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = targetMask
        };

        int count = Physics2D.OverlapCircle(origin, radius, filter, colliderBuffer);

        if (count == 0)
            return null;

        Collider2D nearest = null;
        float minSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Vector2 toTarget = (Vector2)colliderBuffer[i].transform.position - origin;
            float sqrDist = toTarget.sqrMagnitude;

            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = colliderBuffer[i];
            }
        }

        return nearest;
    }


    private void AttackDetectionTarget(Func<Collider2D, CancellationToken, UniTask> onAttackingTarget, UnityAction onClearAttacking = null)
    {
        Collider2D collider = GetNearestTarget(master.position, detectionRange, targetMask) ?? boxCollider2D;

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
        if (playableCharacter == null)
        {
            return;
        }
        isAttacking = true;
        Vector3 direction = playableCharacter.isFacingRight ? Vector3.right : Vector3.left;
        Vector3 targetPosition = transform.position + direction * travelRange;

        attackingCancellationTokenSource?.Cancel();
        attackingCancellationTokenSource = new();

        await TravelToTarget(targetPosition, attackingCancellationTokenSource.Token, true, ResetPositionRandomizeAndScale);

        isAttacking = false;
    }

    private async UniTask OnTargetAttack(Collider2D target, CancellationToken cancellationToken)
    {
        await TravelToTarget(target.transform.position, cancellationToken, false, ResetScale);
    }

    private async UniTask TravelToTarget(Vector3 targetPosition, CancellationToken cancellationToken, bool oneTimeDealDamage, UnityAction onTravelFinish = null)
    {
        startDetectTarget = true;
        float totalTravel = 0f;
        float time = 0;
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
        transform.rotation = targetRotation;
        transform.localScale = Vector3.one * statProperty.size;

        ResetAlreadyDamaged();

        while (time < maximumAttackDuration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                startDetectTarget = false;
                return;
            }

            if (TryDealDamageToTarget(damageable, false))
            {
                attackingAudioSource.Play();
                if (oneTimeDealDamage)
                {
                    break;
                }
            }

            Vector2 oldPosition = transform.position;
            Vector2 moveVector = statProperty.speed * Time.deltaTime * (Vector2)transform.up;
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
        startDetectTarget = false;

        bool isMasterFacingRight = playableCharacter.isFacingRight;

        transform.rotation = Quaternion.identity;

        while (time < maximumAttackDuration)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            Vector3 currentPosition = transform.position;
            Vector2 newPosition = Vector2.MoveTowards(currentPosition, targetPosition, statProperty.speed * Time.deltaTime);

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

        float scaleDuration = 0.7f;
        while (time < scaleDuration)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            float percentage = time / scaleDuration;
            Vector3 scaleLerp = Vector3.Lerp(Vector3.one, Vector3.one * statProperty.size, percentage);
            transform.localScale = scaleLerp;
            time += Time.deltaTime;
            await UniTask.Yield();
        }

        float slashingDuration = 0.5f;
        time = 0f;
        float targetAngle = isMasterFacingRight ? -360 : 360;

        startDetectTarget = true;
        ResetAlreadyDamaged();

        attackingAudioSource.Play();

        while (time < slashingDuration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                startDetectTarget = false;
                return;
            }
            TryDealDamageToTarget(damageable, true);

            float angle = Mathf.Lerp(0f, targetAngle, time / slashingDuration);
            transform.rotation = Quaternion.Euler(0, 0, angle);
            time += Time.deltaTime;
            await UniTask.Yield();
        }

        startDetectTarget = false;

        await UniTaskExtensions.DelayWithCancel(0.5f, cancellationToken);
    }

    private void ResetAlreadyDamaged()
    {
        alreadyDamaged.Clear();
    }
    private bool TryDealDamageToTarget(IDamageable damageable, bool addImpact)
    {
        if (damageable != null && damageable.EnableTakeDamage() && !alreadyDamaged.Contains(damageable))
        {
            if (addImpact)
                damageable.TakeDamage(statProperty.damage, transform.position);
            else
                damageable.TakeDamage(statProperty.damage);

            alreadyDamaged.Add(damageable);
            return true;
        }

        return false;
    }

    #region Deactive State
    private void DeactiveEnter()
    {
        rigidBody2D.bodyType = RigidbodyType2D.Dynamic;
        boxCollider2D.isTrigger = false;
        isAttacking = false;
    }

    private void DeactiveUpdate()
    {
        if (!playableCharacter.IsDead)
        {
            magicSwordStateMachine.SetState(MagicSwordStateType.Active);
        }
    }

    private void DeactiveLeave()
    {
        rigidBody2D.bodyType = RigidbodyType2D.Static;
        boxCollider2D.isTrigger = true;
        isAttacking = false;
    }

    public class DeactiveState : BaseState<MagicSwordItemController>
    {
        public DeactiveState(MagicSwordItemController component) : base(component)
        {
        }

        public override void EnterState()
        {
            component.DeactiveEnter();
        }

        public override void FixedUpdateState()
        {
        }

        public override void LeaveState()
        {
            component.DeactiveLeave();
        }

        public override void UpdateState()
        {
            component.DeactiveUpdate();
        }
    }
    #endregion

    #region Active State
    private void ActiveEnter()
    {
        rigidBody2D.bodyType = RigidbodyType2D.Static;
        boxCollider2D.isTrigger = true;
    }

    private void ActiveUpdate()
    {
        if (playableCharacter.IsDead)
        {
            magicSwordStateMachine.SetState(MagicSwordStateType.Deactive);
            return;
        }

        float inertiaValue = inertiaRate * Time.deltaTime;
        if (TryGetStandPosition(out Vector2 position))
        {
            Vector3 targetPosition = position;
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, targetPosition, inertiaValue),
                Quaternion.Lerp(transform.rotation, standRotation, inertiaValue));
        }
    }

    public class ActiveState : BaseState<MagicSwordItemController>
    {
        public ActiveState(MagicSwordItemController component) : base(component)
        {
        }

        public override void EnterState()
        {
            component.ActiveEnter();
        }

        public override void FixedUpdateState()
        {
        }

        public override void LeaveState()
        {
        }

        public override void UpdateState()
        {
            component.ActiveUpdate();
        }
    }
    #endregion

    [Serializable]
    public class MagicSwordStateMachine : EnumStateMachine<MagicSwordItemController, MagicSwordStateType>
    {
        ActiveState activeState;
        DeactiveState deactiveState;
        protected override void InitializeState(MagicSwordItemController component)
        {
            activeState = new(component);
            deactiveState = new(component);
        }

        protected override BaseState<MagicSwordItemController> GetState(MagicSwordStateType type) => type switch
        {
            MagicSwordStateType.Active => activeState,
            MagicSwordStateType.Deactive => deactiveState,
            _ => throw new NotImplementedException(),
        };
    }
}

public enum MagicSwordStateType
{
    Active,
    Deactive
}