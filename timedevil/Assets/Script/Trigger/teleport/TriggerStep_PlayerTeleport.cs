// Assets/Script/Trigger/teleport/TriggerStep_PlayerTeleport.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_PlayerTeleport : TriggerStepBase
{
    [Header("Teleport Target")]
    [SerializeField] private Transform targetPoint;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Fade (in-scene)")]
    [SerializeField] private bool useFade = false;
    [SerializeField] private FadePanelFader fadePanel;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float fadeInDuration = 0.25f;

    [Header("Input Lock")]
    [SerializeField] private bool lockPlayerInput = true;

    [Header("After Teleport Camera Mode")]
    [SerializeField] private CameraModeId afterMode = CameraModeId.FollowConfined;

    [Tooltip("FollowConfined일 때 Clamp bounds로 쓸 Collider2D(보통 BoxCollider2D 추천)")]
    [SerializeField] private Collider2D afterBounds;

    [Tooltip("Fixed/Cutscene일 때 카메라 고정 앵커(Indoor)")]
    [SerializeField] private Transform fixedCameraAnchorPoint;

    [Tooltip("0이면 변경 안 함")]
    [SerializeField] private float afterOrthoSize = 0f;

    [Header("Camera Warp Fix")]
    [SerializeField] private bool notifyWarpToCinemachine = true;
    [SerializeField] private bool snapCameraWhenFixed = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        if (!fadePanel)
            fadePanel = FindObjectOfType<FadePanelFader>(true);
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!targetPoint)
        {
            Debug.LogWarning("[TriggerStep_PlayerTeleport] targetPoint가 비어있습니다.");
            yield break;
        }

        Transform playerTr = (ctx != null) ? ctx.player : null;
        if (!playerTr)
        {
            var pm = Object.FindObjectOfType<PlayerMove>(true);
            playerTr = pm ? pm.transform : null;
        }
        if (!playerTr)
        {
            Debug.LogWarning("[TriggerStep_PlayerTeleport] 플레이어 Transform을 찾지 못했습니다.");
            yield break;
        }

        Vector3 from = playerTr.position;
        Vector3 to = targetPoint.position + (Vector3)offset;

        if (debugLog)
            Debug.Log($"[TriggerStep_PlayerTeleport] from={from} to={to} mode={afterMode}");

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = true;
        if (CameraManager.Instance) CameraManager.Instance.BeginTransition(lockCamera: true);

        if (useFade && fadePanel != null)
            yield return fadePanel.FadeTo(1f, fadeOutDuration);

        // 이동
        playerTr.position = to;

        // 카메라 적용은 CameraManager 책임(Indoor 앵커도 넘김)
        if (CameraManager.Instance != null)
        {
            float? size = (afterOrthoSize > 0f) ? afterOrthoSize : (float?)null;
            if (debugLog && afterMode == CameraModeId.FollowConfined && afterBounds == null)
                Debug.LogWarning("[TriggerStep_PlayerTeleport] afterBounds is null in FollowConfined. Camera will fallback to FollowFree.");
            if (debugLog && (afterMode == CameraModeId.Fixed || afterMode == CameraModeId.Cutscene) && fixedCameraAnchorPoint == null)
                Debug.LogWarning("[TriggerStep_PlayerTeleport] fixedCameraAnchorPoint is null in Fixed/Cutscene. Camera will fallback to player/target position.");

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

        if (useFade && fadePanel != null)
            yield return fadePanel.FadeTo(0f, fadeInDuration);

        if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = false;
    }
}
