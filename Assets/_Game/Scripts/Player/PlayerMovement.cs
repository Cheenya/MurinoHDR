using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MurinoHDR.Player
{

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _walkSpeed = 4.5f;
    [SerializeField] private float _sprintMultiplier = 1.6f;
    [SerializeField] private float _crouchMultiplier = 0.55f;
    [SerializeField] private float _jumpHeight = 1.15f;
    [SerializeField] private float _gravity = -22f;
    [SerializeField] private float _standingHeight = 1.8f;
    [SerializeField] private float _crouchHeight = 1.05f;
    [SerializeField] private float _heightLerpSpeed = 12f;
    [SerializeField] private float _standingCameraHeight = 0.72f;
    [SerializeField] private float _crouchCameraHeight = 0.3f;

    private CharacterController _characterController;
    private Transform _cameraTransform;
    private Vector3 _cameraLocalPosition;
    private float _verticalVelocity;
    private bool _isCrouching;

    public bool IsGrounded => _characterController != null && _characterController.isGrounded;
    public bool IsCrouching => _isCrouching;
    public float CurrentHorizontalSpeed { get; private set; }

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _characterController.height = _standingHeight;
        _characterController.center = new Vector3(0f, _standingHeight * 0.5f, 0f);
    }

    private void Start()
    {
        var playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            _cameraTransform = playerCamera.transform;
            _cameraLocalPosition = _cameraTransform.localPosition;
        }
    }

    private void Update()
    {
        UpdateStance();
        UpdateMovement();
        UpdateCameraHeight();
    }

    private void UpdateStance()
    {
        var wantsCrouch = PlayerInputAdapter.IsCrouchHeld();
        _isCrouching = wantsCrouch || (_isCrouching && !CanStandUp());

        var targetHeight = _isCrouching ? _crouchHeight : _standingHeight;
        _characterController.height = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * _heightLerpSpeed);
        _characterController.height = Mathf.Clamp(_characterController.height, _crouchHeight, _standingHeight);
        _characterController.center = new Vector3(0f, _characterController.height * 0.5f, 0f);
    }

    private void UpdateMovement()
    {
        var move2D = PlayerInputAdapter.ReadMove();
        var moveInput = new Vector3(move2D.x, 0f, move2D.y);
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);

        var speed = _walkSpeed;
        if (_isCrouching)
        {
            speed *= _crouchMultiplier;
        }
        else if (PlayerInputAdapter.IsSprintHeld())
        {
            speed *= _sprintMultiplier;
        }

        var movement = transform.TransformDirection(moveInput) * speed;

        if (IsGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }

        if (IsGrounded && !_isCrouching && PlayerInputAdapter.WasJumpPressed())
        {
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        _verticalVelocity += _gravity * Time.deltaTime;
        movement.y = _verticalVelocity;

        _characterController.Move(movement * Time.deltaTime);
        CurrentHorizontalSpeed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;
    }

    private void UpdateCameraHeight()
    {
        if (_cameraTransform == null)
        {
            return;
        }

        var targetY = _isCrouching ? _crouchCameraHeight : _standingCameraHeight;
        var localPosition = _cameraTransform.localPosition;
        localPosition.y = Mathf.Lerp(localPosition.y, targetY, Time.deltaTime * _heightLerpSpeed);
        _cameraTransform.localPosition = localPosition;
        _cameraLocalPosition = localPosition;
    }

    private bool CanStandUp()
    {
        var radius = Mathf.Max(0.05f, _characterController.radius * 0.95f);
        var bottom = transform.position + Vector3.up * radius;
        var top = transform.position + Vector3.up * (_standingHeight - radius);
        var colliders = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].transform.root != transform)
            {
                return false;
            }
        }

        return true;
    }
}

internal static class PlayerInputAdapter
{
    public static Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            var move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed) move.y -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            return Vector2.ClampMagnitude(move, 1f);
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }

    public static Vector2 ReadLook()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#else
        return Vector2.zero;
#endif
    }

    public static bool IsSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.isPressed;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift);
#else
        return false;
#endif
    }

    public static bool IsCrouchHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
#else
        return false;
#endif
    }

    public static bool WasJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    public static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.eKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }

    public static bool WasCursorReleasePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    public static bool WasLookCapturePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }
}
}
