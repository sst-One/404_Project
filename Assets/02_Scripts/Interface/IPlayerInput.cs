using UnityEngine;

public interface IPlayerInput
{
    Vector3 GetHandAimTarget();
    bool IsGripToggled();
    bool IsLeanCommitted();
    bool IsFreezing();
}
