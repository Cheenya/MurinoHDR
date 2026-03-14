using UnityEngine;

namespace MurinoHDR.Player
{

public sealed class PlayerLook : MonoBehaviour
{
    [SerializeField] private float _mouseSensitivityX = 0.055f;
    [SerializeField] private float _mouseSensitivityY = 0.055f;
    [SerializeField] private float _pitchMin = -75f;
    [SerializeField] private float _pitchMax = 75f;

    private Camera _playerCamera;
    private float _pitch;
    private bool _cursorLocked;

    private void Start()
    {
        _playerCamera = GetComponentInChildren<Camera>();
        ApplyCursorLock(true);
    }

    private void Update()
    {
        if (_playerCamera == null)
        {
            return;
        }

        if (PlayerInputAdapter.WasCursorReleasePressed())
        {
            ApplyCursorLock(false);
        }

        if (!_cursorLocked)
        {
            if (PlayerInputAdapter.WasLookCapturePressed())
            {
                ApplyCursorLock(true);
            }

            return;
        }

        var look = PlayerInputAdapter.ReadLook();
        var yaw = look.x * _mouseSensitivityX;
        var pitchDelta = look.y * _mouseSensitivityY;

        _pitch -= pitchDelta;
        _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);

        transform.Rotate(Vector3.up * yaw, Space.Self);
        _playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private static void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            _cursorLocked = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        ApplyCursorLock(true);
    }

    private void ApplyCursorLock(bool locked)
    {
        _cursorLocked = locked;
        SetCursorState(locked);
    }
}
}
