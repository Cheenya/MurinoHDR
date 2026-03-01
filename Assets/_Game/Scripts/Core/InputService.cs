using UnityEngine;

namespace MurinoHDR.Core;

public sealed class InputService : MonoBehaviour
{
    public Vector2 ReadMove()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    public Vector2 ReadLook()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    public bool IsSprintHeld()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    public bool WasInteractPressed()
    {
        return Input.GetKeyDown(KeyCode.E);
    }
}
