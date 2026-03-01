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
    [SerializeField] private float _gravity = -18f;

    private CharacterController _characterController;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        var move2D = PlayerInputAdapter.ReadMove();
        var moveInput = new Vector3(move2D.x, 0f, move2D.y);
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);

        var speed = _walkSpeed;
        if (PlayerInputAdapter.IsSprintHeld())
        {
            speed *= _sprintMultiplier;
        }

        var movement = transform.TransformDirection(moveInput) * speed;

        if (_characterController.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }

        _verticalVelocity += _gravity * Time.deltaTime;
        movement.y = _verticalVelocity;

        _characterController.Move(movement * Time.deltaTime);
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
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    public static Vector2 ReadLook()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue();
        }
#endif
        return new Vector2(Input.GetAxis("Mouse X") * 12f, Input.GetAxis("Mouse Y") * 12f);
    }

    public static bool IsSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.isPressed;
        }
#endif
        return Input.GetKey(KeyCode.LeftShift);
    }

    public static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.eKey.wasPressedThisFrame;
        }
#endif
        return Input.GetKeyDown(KeyCode.E);
    }
}
}


