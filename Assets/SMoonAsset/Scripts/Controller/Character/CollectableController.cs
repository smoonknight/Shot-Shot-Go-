using SMoonUniversalAsset;
using UnityEngine;

public class CollectableController : MonoBehaviour, IMagneticable, IOutOfBoundable
{
    public CollectableType type;
    public Rigidbody2D rigidBody2D;
    public Collider2D collectableCollider2D;
    public Transform magneticSource;
    const float travelSpeed = 10;

    public int value;

    bool isMagnetic;

    readonly TimeChecker loseAttractionTimeChecker = new(0.5f);

    public void FixedUpdate()
    {
        if (!isMagnetic)
        {
            return;
        }
        if (loseAttractionTimeChecker.IsDurationEnd())
        {
            rigidBody2D.bodyType = RigidbodyType2D.Dynamic;
            collectableCollider2D.isTrigger = false;
            isMagnetic = false;
        }
    }

    public void Attraction(Vector2 magnetPosition)
    {
        loseAttractionTimeChecker.UpdateTime();
        transform.position = Vector2.MoveTowards(transform.position, magnetPosition, travelSpeed * Time.deltaTime);
        rigidBody2D.bodyType = RigidbodyType2D.Static;
        collectableCollider2D.isTrigger = true;
        isMagnetic = true;
    }

    public bool CheckOutOfBound() => true;

    public void OutOfBoundChangeLocation()
    {
        rigidBody2D.linearVelocity = Vector3.zero;
        transform.position = GameplayManager.Instance.GetOutOfBoundByGameMode();
    }

    public Transform MagneticSource() => magneticSource;

    public void OnMagneticClose(PlayerController playerController)
    {
        ProcessCollectable(playerController);
        gameObject.SetActive(false);
    }

    private void ProcessCollectable(PlayerController playerController)
    {
        switch (type)
        {
            case CollectableType.Coin:
                playerController.AddExperience(value);
                break;
            case CollectableType.Heart:
                playerController.AddHealth(value);
                break;
            default:
                break;
        }
    }
}

