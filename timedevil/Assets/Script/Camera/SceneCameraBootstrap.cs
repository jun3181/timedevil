using UnityEngine;

[DisallowMultipleComponent]
public class SceneCameraBootstrap : MonoBehaviour
{
    // 씬 시작 시 지정한 카메라 모드로 CameraManager를 초기화한다.
    [Header("Start Mode")]
    public CameraModeId startMode = CameraModeId.Fixed;

    [Header("Follow Target (비워두면 PlayerMove 자동 탐색)")]
    public Transform followTarget;

    [Header("Follow Confined Bounds (BoxCollider2D 권장)")]
    public Collider2D confinerBounds;

    [Header("카메라 Ortho Size (0 이하 = CameraManager 기본값 사용)")]
    public float orthoSize = 0f;

    [Header("Fixed/Cutscene Anchor (선택)")]
    public Transform fixedOrCutsceneAnchor;

    [Header("Debug")]
    public bool debugLog = true;

    private void Start()
    {
        // CameraManager가 없으면 아무 작업도 하지 않는다.
        if (!CameraManager.Instance) return;

        // followTarget이 비어 있으면 PlayerMove를 찾아 자동 지정한다.
        if (!followTarget)
        {
            var pm = FindObjectOfType<PlayerMove>(true);
            if (pm) followTarget = pm.transform;
        }

        // orthoSize가 0 이하이면 CameraManager 기본값을 사용한다.
        float? size = (orthoSize > 0f) ? orthoSize : (float?)null;

        switch (startMode)
        {
            case CameraModeId.Fixed:
                CameraManager.Instance.SetFixed(
                    lockWorldPos: fixedOrCutsceneAnchor ? fixedOrCutsceneAnchor.position :
                                 (followTarget ? followTarget.position : (Vector3?)null),
                    orthoSize: size
                );
                break;

            case CameraModeId.FollowFree:
                CameraManager.Instance.SetFollowFree(followTarget, size);
                break;

            case CameraModeId.FollowConfined:
                CameraManager.Instance.SetFollowConfined(followTarget, confinerBounds, size);
                break;

            case CameraModeId.Cutscene:
                CameraManager.Instance.SetCutscene(
                    fixedOrCutsceneAnchor ? fixedOrCutsceneAnchor.position :
                    (followTarget ? followTarget.position : transform.position),
                    size
                );
                break;
        }

        if (debugLog)
            Debug.Log($"[SceneCameraBootstrap] StartMode={startMode} follow={(followTarget ? followTarget.name : "(null)")} bounds={(confinerBounds ? confinerBounds.name : "(null)")} ortho={(size.HasValue ? size.Value.ToString() : "(default)")}");
    }
}
