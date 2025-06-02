using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class Input : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool Run { get; private set; }
    public bool Jump { get; private set; }
    public bool Crouch { get; private set; }

    private InputActionMap _currentMap;
    public InputAction MoveAction { private set; get; }
    public InputAction RunAction { private set; get; }
    public InputAction JumpAction { private set; get; }
    public InputAction CrouchAction { private set; get; }
    public InputAction InteractAction { private set; get; }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        _currentMap.Enable();

    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Enable();
        _currentMap.Disable();
    }

    private void Awake()
    {
        HideCursor();
        _currentMap = playerInput.currentActionMap;
        MoveAction = _currentMap.FindAction("Move");
        RunAction = _currentMap.FindAction("Run");
        JumpAction = _currentMap.FindAction("Jump");
        CrouchAction = _currentMap.FindAction("Crouch");
        InteractAction = _currentMap.FindAction("Interact");

        MoveAction.performed += OnMove;
        RunAction.performed += OnRun;
        JumpAction.performed += OnJump;
        CrouchAction.started += OnCrouch;

        MoveAction.canceled += OnMove;
        RunAction.canceled += OnRun;
        JumpAction.canceled += OnJump;
        CrouchAction.canceled += OnCrouch;
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }
    private void OnRun(InputAction.CallbackContext context)
    {
        Run = context.ReadValueAsButton();
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        Jump = context.ReadValueAsButton();
    }
    private void OnCrouch(InputAction.CallbackContext context)
    {
        Crouch = context.ReadValueAsButton();
    }
}