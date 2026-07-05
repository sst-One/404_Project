using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardInputProcessor : IPlayerInput
{
    public Vector3 GetHandAimTarget()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        return Vector3.zero;
    }

    public bool IsGripToggled()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }
        return false;
    }

    public bool IsLeanCommitted()
    {
        if (Keyboard.current != null)
        {
            return Keyboard.current.wKey.wasPressedThisFrame;
        }
        return false;
    }

    public bool IsFreezing()
    {
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.isPressed;
        }
        return false;
    }
}
