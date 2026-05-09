using UnityEngine;

public static class PlayerReturnContext
{
    // --- Return Core ---
    public static string ReturnSceneName;
    public static bool HasReturnPosition;
    public static Vector2 ReturnPosition;

    // --- Grace (재진입 방지 등) ---
    public static bool IsInGracePeriod = false;
    public static float GraceSecondsPending = 0f;

    // --- Camera Rebind (옵션) ---
    public static bool CameraRebindRequested = false;
    public static string TargetVcamName = null;

    // --- B Suppression (Overlap) ---
    public static bool UseOverlapSuppression = false;
    public static float OverlapRadiusPending = 0f;
    public static float OverlapSecondsPending = 0f;

    // --- Return Camera Restore (추가) ---
    public static bool RestoreCameraStatePending = false;
    public static CameraModeId ReturnCameraMode = CameraModeId.Fixed;
    public static float ReturnCameraOrthoSize = 0f;          // 0이면 CameraManager 기본값 유지
    public static Vector2 ReturnCameraFixedPos = Vector2.zero;
    public static string ReturnCameraBoundsName = null;      // FollowConfined에서 bounds 찾는 이름

    // (기존 참조 유지용)
    public static Vector2 MonsterReturnPosition;
    public static string MonsterNameInScene;
    public static string MonsterInstanceId;

    /// <summary>
    /// 트리거로 배틀 진입 전에 "복귀정보" 저장 (B Suppression + 카메라 복원 포함)
    /// </summary>
    public static void SetReturnFromTrigger(
        string returnSceneName,
        Vector2 returnPosition,
        float graceSeconds,
        bool requestCameraRebind,
        string targetVcamName,
        bool useOverlapSuppression,
        float overlapRadius,
        float overlapSeconds,

        // ===== (추가) 복귀 카메라 복원 데이터 =====
        bool restoreCameraState = false,
        CameraModeId cameraMode = CameraModeId.Fixed,
        float cameraOrthoSize = 0f,
        Vector2 cameraFixedPos = default,
        string cameraBoundsName = null
    )
    {
        ReturnSceneName = returnSceneName;
        ReturnPosition = returnPosition;
        HasReturnPosition = true;

        // Grace
        // 주의: 실제 grace 활성화는 "복귀 씬"에서 PlayerReturnManager가 담당한다.
        // 여기서는 pending 값만 기록하고 즉시 차단 플래그를 켜지 않는다.
        IsInGracePeriod = false;
        GraceSecondsPending = Mathf.Max(0f, graceSeconds);

        // Camera (Rebind 옵션)
        CameraRebindRequested = requestCameraRebind;
        TargetVcamName = string.IsNullOrWhiteSpace(targetVcamName) ? null : targetVcamName;

        // B Suppression
        UseOverlapSuppression = useOverlapSuppression && overlapRadius > 0f && overlapSeconds > 0f;
        OverlapRadiusPending = overlapRadius;
        OverlapSecondsPending = overlapSeconds;

        // Return Camera Restore (추가)
        RestoreCameraStatePending = restoreCameraState;
        ReturnCameraMode = cameraMode;
        ReturnCameraOrthoSize = cameraOrthoSize;
        ReturnCameraFixedPos = cameraFixedPos;
        ReturnCameraBoundsName = string.IsNullOrWhiteSpace(cameraBoundsName) ? null : cameraBoundsName;
    }

    /// <summary>복귀 처리 끝나면 1회성 데이터 정리</summary>
    public static void ClearReturnCore()
    {
        ReturnSceneName = null;
        HasReturnPosition = false;
        ReturnPosition = Vector2.zero;

        IsInGracePeriod = false;
        GraceSecondsPending = 0f;

        CameraRebindRequested = false;
        TargetVcamName = null;

        UseOverlapSuppression = false;
        OverlapRadiusPending = 0f;
        OverlapSecondsPending = 0f;

        // 추가
        RestoreCameraStatePending = false;
        ReturnCameraMode = CameraModeId.Fixed;
        ReturnCameraOrthoSize = 0f;
        ReturnCameraFixedPos = Vector2.zero;
        ReturnCameraBoundsName = null;
    }
}
