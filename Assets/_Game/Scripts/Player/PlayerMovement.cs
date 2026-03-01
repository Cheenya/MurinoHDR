using UnityEngine;

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
        var moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);

        var speed = _walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
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
}


