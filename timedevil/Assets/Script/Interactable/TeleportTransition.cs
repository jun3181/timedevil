using System.Collections;
using UnityEngine;

public class TeleportTransition : MonoBehaviour, IInteractable
{
    [Header("Teleport Target")]
    public Transform targetPoint;

    [Header("After Teleport Camera Mode")]
    public CameraModeId afterMode = CameraModeId.FollowConfined;

    [Tooltip("FollowConfined일 때 사용할 Confiner Bounds(PolygonCollider2D 등)")]
    public Collider2D afterConfinerBounds;

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

        // 대화 중이면 텔레포트 금지
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        StartCoroutine(CoTeleport());
    }

    private IEnumerator CoTeleport()
    {
        _running = true;

        if (debugLog) Debug.Log($"[TeleportTransition] Start -> {targetPoint.position} mode={afterMode}");

        // 입력 잠금
        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = true;

        // 전환 시작(카메라 고정)
        if (CameraManager.Instance) CameraManager.Instance.BeginTransition(lockCamera: true);

        // Fade In (검은 화면)
        if (SceneFader.instance != null)
            yield return SceneFader.instance.StartCoroutine(SceneFader.instance.Fade(1f));

        // 플레이어 이동
        var player = FindObjectOfType<PlayerMove>(true);
        if (player != null)
            player.transform.position = targetPoint.position;

        // 카메라 모드 적용(이동 후)
        if (CameraManager.Instance != null)
        {
            float? size = (afterOrthoSize > 0f) ? afterOrthoSize : (float?)null;

            switch (afterMode)
            {
                case CameraModeId.Fixed:
                    CameraManager.Instance.SetFixed(
                        lockWorldPos: player ? player.transform.position : targetPoint.position,
                        orthoSize: size,
                        disableConfiner: true
                    );
                    break;

                case CameraModeId.FollowConfined:
                    CameraManager.Instance.SetFollowConfined(
                        followTarget: player ? player.transform : null,
                        bounds: afterConfinerBounds,
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
                    CameraManager.Instance.SetCutscene(
                        worldPos: targetPoint.position,
                        orthoSize: size,
                        useConfiner: false,
                        bounds: null
                    );
                    break;
            }

            CameraManager.Instance.EndTransition();
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
