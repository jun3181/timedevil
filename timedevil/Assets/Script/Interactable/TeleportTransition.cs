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

    [Tooltip("Fixed/Cutscene일 때 카메라를 고정할 '앵커 위치'. 비면 player/targetPoint로 대체")]
    public Transform fixedCameraAnchorPoint;

    [Tooltip("이동 후 줌(OrthoSize). 0이면 변경 안 함")]
    public float afterOrthoSize = 0f;

    [Header("Ambient Dark Overlay (상태 유지용)")]
    public bool applyDarkOverlay = false;
    [Range(0f, 1f)] public float darkOverlayAlpha = 0.35f;
    public float darkOverlayDuration = 0.15f;

    [Header("Lock")]
    public bool lockPlayerInput = true;

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

        if (debugLog)
        {
            Debug.Log($"[TeleportTransition] Start");
            Debug.Log($"  - playerTargetPoint={targetPoint.position}");
            Debug.Log($"  - afterMode={afterMode}, afterBounds={(afterBounds ? afterBounds.name : "(null)")}");
            Debug.Log($"  - fixedAnchor={(fixedCameraAnchorPoint ? fixedCameraAnchorPoint.name : "(null)")}");
        }

        // 입력 잠금
        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = true;

        // 카메라 전환 시작(페이드 중 흔들림 방지)
        if (CameraManager.Instance) CameraManager.Instance.BeginTransition(lockCamera: true);

        // Fade In (검은 화면)
        if (SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(1f));

        // 플레이어 찾기 + 워프 델타 계산(NotifyTargetWarp용)
        var player = FindObjectOfType<PlayerMove>(true);
        Vector3 oldPos = player ? player.transform.position : Vector3.zero;

        // 1) 플레이어 이동
        if (player != null)
            player.transform.position = targetPoint.position;

        Vector3 newPos = player ? player.transform.position : targetPoint.position;
        Vector3 delta = newPos - oldPos;

        // 2) 카메라 모드 적용(이동 후)
        if (CameraManager.Instance != null)
        {
            float? size = (afterOrthoSize > 0f) ? afterOrthoSize : (float?)null;

            // Fixed/Cutscene에서 쓸 카메라 고정 위치 결정
            Vector3 fixedPos =
                fixedCameraAnchorPoint ? fixedCameraAnchorPoint.position :
                (player ? player.transform.position : targetPoint.position);

            switch (afterMode)
            {
                case CameraModeId.Fixed:
                    // ✅ 앵커 위치를 TeleportTransition에서 넘겨줌
                    CameraManager.Instance.SetFixed(lockWorldPos: fixedPos, orthoSize: size);

                    // ✅ 즉시 스냅(가끔 이전 상태로 끌리는 현상 방지)
                    CameraManager.Instance.SnapCameraTo(fixedPos);
                    break;

                case CameraModeId.FollowConfined:
                    CameraManager.Instance.SetFollowConfined(
                        followTarget: player ? player.transform : null,
                        bounds: afterBounds,
                        orthoSize: size
                    );
                    if (player) CameraManager.Instance.NotifyTargetWarp(player.transform, delta);
                    break;

                case CameraModeId.FollowFree:
                    CameraManager.Instance.SetFollowFree(
                        followTarget: player ? player.transform : null,
                        orthoSize: size
                    );
                    if (player) CameraManager.Instance.NotifyTargetWarp(player.transform, delta);
                    break;

                case CameraModeId.Cutscene:
                    CameraManager.Instance.SetCutscene(worldPos: fixedPos, orthoSize: size);
                    CameraManager.Instance.SnapCameraTo(fixedPos);
                    break;
            }

            CameraManager.Instance.EndTransition();
        }

        // 어둠 오버레이(상태 유지)
        if (applyDarkOverlay && DarkOverlay.Instance != null)
        {
            DarkOverlay.Instance.SetAlpha(darkOverlayAlpha, darkOverlayDuration);
            if (debugLog) Debug.Log($"[TeleportTransition] DarkOverlay -> {darkOverlayAlpha}");
        }

        // Fade Out (밝아짐)
        if (SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(0f));

        // 입력 해제
        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = false;

        _running = false;
        if (debugLog) Debug.Log("[TeleportTransition] Done");
    }
}
