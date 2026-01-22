using System.Collections;
using UnityEngine;

public class TeleportTransition : MonoBehaviour, IInteractable
{
    [Header("Teleport Target (Player)")]
    public Transform targetPoint;

    [Header("Camera Snap Point (선택)")]
    public Transform cameraSnapPoint;

    [Header("After Teleport Camera Mode")]
    public CameraModeId afterMode = CameraModeId.FollowConfined;

    [Tooltip("FollowConfined일 때 Clamp Bounds(콜라이더)")]
    public Collider2D afterBounds;

    [Tooltip("이동 후 줌(OrthoSize). 0이면 변경 안 함")]
    public float afterOrthoSize = 0f;

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

        Vector3 camPos = (cameraSnapPoint ? cameraSnapPoint.position : targetPoint.position);

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = true;
        if (CameraManager.Instance) CameraManager.Instance.BeginTransition(lockCamera: true);

        // Fade In
        if (SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(1f));

        // ===== 플레이어 이동 =====
        var player = FindObjectOfType<PlayerMove>(true);
        Vector3 before = player ? player.transform.position : Vector3.zero;

        if (player != null)
            player.transform.position = targetPoint.position;

        Vector3 after = player ? player.transform.position : targetPoint.position;
        Vector3 delta = after - before;

        //  Follow 계열에서 카메라가 이전 위치로 끌리는 현상 방지용 (스냅 아님)
        if (CameraManager.Instance && player != null)
            CameraManager.Instance.NotifyTargetWarp(player.transform, delta);

        // ===== 카메라 모드 적용 =====
        if (CameraManager.Instance != null)
        {
            float? size = (afterOrthoSize > 0f) ? afterOrthoSize : (float?)null;

            switch (afterMode)
            {
                case CameraModeId.Fixed:
                    CameraManager.Instance.SetFixed(lockWorldPos: camPos, orthoSize: size);

                    // ✅ FIXED에서만 강제 스냅
                    CameraManager.Instance.SnapCameraTo(camPos);
                    break;

                case CameraModeId.FollowConfined:
                    CameraManager.Instance.SetFollowConfined(
                        followTarget: player ? player.transform : null,
                        bounds: afterBounds,
                        orthoSize: size
                    );
                    break;

                case CameraModeId.FollowFree:
                    CameraManager.Instance.SetFollowFree(
                        followTarget: player ? player.transform : null,
                        orthoSize: size
                    );
                    break;

                case CameraModeId.Cutscene:
                    CameraManager.Instance.SetCutscene(worldPos: camPos, orthoSize: size);
                    break;
            }

            CameraManager.Instance.EndTransition();
        }

        // Fade Out
        if (SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(0f));

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = false;

        _running = false;
        if (debugLog) Debug.Log("[TeleportTransition] Done");
    }
}
