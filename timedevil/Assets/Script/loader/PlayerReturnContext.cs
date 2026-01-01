using UnityEngine;

public static class PlayerReturnContext
{
    public static string ReturnSceneName;
    public static bool HasReturnPosition;
    public static Vector2 ReturnPosition;

    public static Vector2 MonsterReturnPosition;
    public static string MonsterNameInScene;

    public static string MonsterInstanceId;


    public static bool IsInGracePeriod = false; // 충돌 무적시간 등 사용 시

    public static float GraceSecondsPending = 0f; // ⬅️ 추가

    // ★ 추가: 카메라 재바인딩 요청 플래그 & 타겟 vcam 이름(옵션)
    public static bool CameraRebindRequested = false;
    public static string TargetVcamName = null; // null이면 씬 내 첫 번째 CinemachineVirtualCamera 사용

}
