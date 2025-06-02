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

    protected virtual void Awake()
    {
        rigidBody.GetComponent<Rigidbody2D>();
    }
}