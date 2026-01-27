using System.Collections;
using UnityEngine;

public class TeleportTransition : MonoBehaviour, IInteractable
{
    [Header("Teleport Target (플레이어 이동 위치)")]
    public Transform targetPoint;

    [Header("After Teleport Camera Mode")]
    public CameraModeId afterMode = CameraModeId.FollowConfined;

    [Tooltip("FollowConfined일 때 사용할 Bounds(BoxCollider2D/PolygonCollider2D 등)")]
    public Collider2D afterBounds;

    [Tooltip("Fixed/Cutscene일 때 카메라를 고정할 '앵커 위치'. (Indoor 같은 케이스)")]
    public Transform fixedCameraAnchorPoint;

    [Tooltip("이동 후 줌(OrthoSize). 0이면 변경 안 함")]
    public float afterOrthoSize = 0f;

    [Header("Ambient Dark Overlay (상태 유지용)")]
    public bool applyDarkOverlay = false;
    [Range(0f, 1f)] public float darkOverlayAlpha = 0.35f;
    public float darkOverlayDuration = 0.15f;

    [Header("Lock")]
    public bool lockPlayerInput = true;

    [Header("Camera Warp Fix")]
    public bool notifyWarpToCinemachine = true;
    public bool snapCameraWhenFixed = true;

    [Header("Debug")]
    public bool debugLog = true;

    private bool _running = false;

    public void Interact()
    {
        if (_running) return;

        if (!targetPoint)
        {
            Debug.LogWarning("[TeleportTransition] targetPoint가 비어있음");
            return;
        }

        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        StartCoroutine(CoTeleport());
    }

    private IEnumerator CoTeleport()
    {
        _running = true;

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = true;

        if (CameraManager.Instance) CameraManager.Instance.BeginTransition(lockCamera: true);

        if (SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(1f));

        var player = FindObjectOfType<PlayerMove>(true);
        if (!player)
        {
            Debug.LogWarning("[TeleportTransition] PlayerMove를 찾지 못했습니다.");
            if (SceneFader.instance != null)
                yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(0f));
            if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = false;
            _running = false;
            yield break;
        }

        Transform playerTr = player.transform;

        Vector3 from = playerTr.position;
        Vector3 to = targetPoint.position;

        // 1) 플레이어 이동
        playerTr.position = to;

        // 2) 카메라 적용은 CameraManager가 책임지고 처리
        if (CameraManager.Instance != null)
        {
            float? size = (afterOrthoSize > 0f) ? afterOrthoSize : (float?)null;

            CameraManager.Instance.ApplyAfterTeleport(
                player: playerTr,
                fromPos: from,
                toPos: to,
                afterMode: afterMode,
                afterBounds: afterBounds,
                afterOrthoSize: size,
                fixedCameraAnchorPoint: fixedCameraAnchorPoint,
                notifyWarpToCinemachine: notifyWarpToCinemachine,
                snapCameraWhenFixed: snapCameraWhenFixed
            );

            CameraManager.Instance.EndTransition();
        }

        if (applyDarkOverlay && DarkOverlay.Instance != null)
        {
            DarkOverlay.Instance.SetAlpha(darkOverlayAlpha, darkOverlayDuration);
            if (debugLog) Debug.Log($"[TeleportTransition] DarkOverlay -> {darkOverlayAlpha}");
        }

        if (SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(0f));

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = false;

        _running = false;
        if (debugLog) Debug.Log("[TeleportTransition] Done");
    }
}
