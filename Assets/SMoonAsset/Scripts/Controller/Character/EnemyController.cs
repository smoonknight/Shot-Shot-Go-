using System.Threading;
using Cysharp.Threading.Tasks;
using SMoonUniversalAsset;
using UnityEngine;
using UniTaskExtensions = SMoonUniversalAsset.UniTaskExtensions;

public class EnemyController : PlayableCharacterControllerBase, ITrampolineable
{
    public EnemyType type;

    private bool holdingJump;

    public override UpgradeProperty GetCharacterUpgradeProperty() => GameManager.Instance.GetCopyOfDefaultEnemyCharacterUpgradeProperty(type).upgradeProperty;

    public bool IsDamaging() => true;

    public int JumpValue() => 8;

    const float squashingDuration = 0.15f;
    const float waitingNormalAfterSquasedDuration = 0.75f;
    const float normalAfterSquasedDuration = 0.10f;

    CancellationTokenSource squashCancellationTokenSource;

    protected override float GetMoveTargetVelocityX()
    {
        return 0;
    }

    public override bool IsPlayer() => true;
    public override bool HoldingJump() => holdingJump;

    public void OnTakeTrampoline()
    {
        squashCancellationTokenSource?.Cancel();
        squashCancellationTokenSource = new();

        DoSquashAnimation(squashingDuration, waitingNormalAfterSquasedDuration, normalAfterSquasedDuration, squashCancellationTokenSource.Token);
    }

    private async void DoSquashAnimation(float squashingDuration, float waitingNormalAfterSquasedDuration, float normalAfterSquasedDuration, CancellationToken cancellationToken)
    {
        float originalScaleY = initialScale.y;
        float squashedScaleY = originalScaleY * 0.80f;

        await LerpScaleY(originalScaleY, squashedScaleY, squashingDuration, cancellationToken);

        await UniTaskExtensions.DelayWithCancel(waitingNormalAfterSquasedDuration, cancellationToken);

        await LerpScaleY(transform.localScale.y, originalScaleY, normalAfterSquasedDuration, cancellationToken);

        transform.localScale = initialScale;
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

        // Pastikan nilai akhir benar
        Vector3 finalScale = transform.localScale;
        finalScale.y = to;
        transform.localScale = finalScale;
    }

}

public enum EnemyType
{
    Trired
}