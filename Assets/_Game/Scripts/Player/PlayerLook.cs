using UnityEngine;

namespace MurinoHDR.Player;

public sealed class PlayerLook : MonoBehaviour
{
    [SerializeField] private float _mouseSensitivity = 180f;
    [SerializeField] private float _pitchMin = -75f;
    [SerializeField] private float _pitchMax = 75f;

    private Camera _playerCamera;
    private float _pitch;

    private void Start()
    {
        _playerCamera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        var mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity * Time.deltaTime;
        var mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity * Time.deltaTime;

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);

        transform.Rotate(Vector3.up * mouseX);

        if (_playerCamera != null)
        {
            _playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
