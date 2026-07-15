using UnityEngine;

public interface IPlayerInput
{
    Vector3 GetHandAimTarget();
    bool IsGripToggled();
    bool IsLeanCommitted();
    bool IsFreezing();

    // [신규 통합 브릿지] 타겟 포커스 및 Ready 상태 반환
    Transform GetHoveredTarget();
    Vector3 GetHoveredPoint();
    Vector3 GetHoveredNormal();
    bool IsTargetReady();
}