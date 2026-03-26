using UnityEngine;

[DisallowMultipleComponent]
public class SceneCameraBootstrap : MonoBehaviour
{
    [Header("Start Mode")]
    public CameraModeId startMode = CameraModeId.Fixed;

    [Header("Follow Target (비우면 PlayerMove 자동 탐색)")]
    public Transform followTarget;

    [Header("FollowConfined용 Bounds (BoxCollider2D 권장)")]
    public Collider2D confinerBounds;

                // Fixed ÷̾ fallback ,  ġ Ŀ(  Ʈ ġ) 
                    lockWorldPos: fixedOrCutsceneAnchor ? fixedOrCutsceneAnchor.position : transform.position,

    [Header("Fixed/Cutscene 위치 (선택)")]
    public Transform fixedOrCutsceneAnchor;

    [Header("Debug")]
    public bool debugLog = true;

    private void Start()
    {
        if (!CameraManager.Instance) return;

        if (!followTarget)
        {
            var pm = FindObjectOfType<PlayerMove>(true);
            if (pm) followTarget = pm.transform;
        }

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
