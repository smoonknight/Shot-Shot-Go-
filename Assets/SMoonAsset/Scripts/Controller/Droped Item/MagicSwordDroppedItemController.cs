using System;
using UnityEngine;

public class MagicSwordDroppedItemController : DroppedItemController<MagicSwordItemType>
{
    protected override void OnPlayerAction(PlayableCharacterControllerBase playableCharacter)
    {
        var spawnedMagicSword = MagicSwordSpawnerManager.Instance.GetSpawned(type);
        playableCharacter.AddMagicSword(spawnedMagicSword);
    }
}