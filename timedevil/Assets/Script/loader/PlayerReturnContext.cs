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

    // (기존에 쓰는 몬스터 관련 값이 있다면 유지 - 컴파일/참조 깨짐 방지)
    public static Vector2 MonsterReturnPosition;
    public static string MonsterNameInScene;
    public static string MonsterInstanceId;

    // -----------------------------

    /// <summary>
    /// 트리거로 배틀 진입 전에 "복귀정보" 저장 (B Suppression 포함)
    /// </summary>
    public static void SetReturnFromTrigger(
        string returnSceneName,
        Vector2 returnPosition,
        float graceSeconds,
        bool requestCameraRebind,
        string targetVcamName,
        bool useOverlapSuppression,
        float overlapRadius,
        float overlapSeconds
    )
    {
        ReturnSceneName = returnSceneName;
        ReturnPosition = returnPosition;
        HasReturnPosition = true;

        // Grace
        if (graceSeconds > 0f)
        {
            IsInGracePeriod = true;
            GraceSecondsPending = graceSeconds;
        }
        else
        {
            IsInGracePeriod = false;
            GraceSecondsPending = 0f;
        }

        // Camera
        CameraRebindRequested = requestCameraRebind;
        TargetVcamName = string.IsNullOrWhiteSpace(targetVcamName) ? null : targetVcamName;

        // B Suppression
        UseOverlapSuppression = useOverlapSuppression && overlapRadius > 0f && overlapSeconds > 0f;
        OverlapRadiusPending = overlapRadius;
        OverlapSecondsPending = overlapSeconds;
    }

    /// <summary>복귀 처리 끝나면 1회성 데이터 정리</summary>
    public static void ClearReturnCore()
    {
        ReturnSceneName = null;
        HasReturnPosition = false;
        ReturnPosition = Vector2.zero;

        GraceSecondsPending = 0f;
        CameraRebindRequested = false;
        TargetVcamName = null;

        UseOverlapSuppression = false;
        OverlapRadiusPending = 0f;
        OverlapSecondsPending = 0f;
    }
}
