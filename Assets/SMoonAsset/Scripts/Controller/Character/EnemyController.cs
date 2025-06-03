using UnityEngine;

public class EnemyController : PlayableCharacterControllerBase
{
    public override bool IsPlayer() => true;

    protected override float GetMoveTargetVelocityX()
    {
        return 0;
    }
}