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

    private string ContextTag()
    {
        return $"[TriggerStep_PlayerTeleport] scene={gameObject.scene.name} object={name}";
    }

    private static string BuildTransformPath(Transform t)
    {
        if (!t) return "<null>";
        string path = t.name;
        Transform cur = t.parent;
        while (cur != null)
        {
            path = cur.name + "/" + path;
            cur = cur.parent;
        }
        return path;
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (debugLog)
            Debug.Log($"{ContextTag()} Execute start target={(targetPoint ? targetPoint.name : "<null>")} offset={offset} mode={afterMode}", this);

        if (!targetPoint)
        {
            Debug.LogWarning($"{ContextTag()} targetPoint가 비어있습니다.", this);
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

        if (debugLog)
            Debug.Log($"{ContextTag()} resolved player name={playerTr.name} id={playerTr.gameObject.GetInstanceID()} active={playerTr.gameObject.activeInHierarchy} path={BuildTransformPath(playerTr)} scene={playerTr.gameObject.scene.name}", this);

        Vector3 from = playerTr.position;
        Vector3 to = targetPoint.position + (Vector3)offset;

        if (debugLog)
            Debug.Log($"{ContextTag()} player={playerTr.name} from={from} to={to} targetScene={targetPoint.gameObject.scene.name} mode={afterMode} bounds={(afterBounds ? afterBounds.name : "<null>")} fixedAnchor={(fixedCameraAnchorPoint ? fixedCameraAnchorPoint.name : "<null>")}", this);

        if (lockPlayerInput && GameManager.Instance)
        {
            GameManager.Instance.isAction = true;
            if (debugLog) Debug.Log($"{ContextTag()} input locked", this);
        }
        if (CameraManager.Instance)
        {
            CameraManager.Instance.BeginTransition(lockCamera: true);
            if (debugLog) Debug.Log($"{ContextTag()} CameraManager.BeginTransition(lockCamera=true)", this);
        }

        if (useFade && fadePanel != null)
        {
            if (debugLog) Debug.Log($"{ContextTag()} fade out start duration={fadeOutDuration}", this);
            yield return fadePanel.FadeTo(1f, fadeOutDuration);
            if (debugLog) Debug.Log($"{ContextTag()} fade out end", this);
        }

        // 이동
        playerTr.position = to;
        if (debugLog) Debug.Log($"{ContextTag()} player position applied current={playerTr.position}", this);

        // 카메라 적용은 CameraManager 책임(Indoor 앵커도 넘김)
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

            if (debugLog)
                Debug.Log($"{ContextTag()} ApplyAfterTeleport mode={afterMode} bounds={(afterBounds ? afterBounds.name : "<null>")} ortho={(size.HasValue ? size.Value.ToString() : "<keep>")} fixedAnchor={(fixedCameraAnchorPoint ? fixedCameraAnchorPoint.position.ToString() : "<null>")} notifyWarp={notifyWarpToCinemachine} snapFixed={snapCameraWhenFixed}", this);

            CameraManager.Instance.EndTransition();
            if (debugLog) Debug.Log($"{ContextTag()} CameraManager.EndTransition", this);
        }

        if (useFade && fadePanel != null)
        {
            if (debugLog) Debug.Log($"{ContextTag()} fade in start duration={fadeInDuration}", this);
            yield return fadePanel.FadeTo(0f, fadeInDuration);
            if (debugLog) Debug.Log($"{ContextTag()} fade in end", this);
        }

        if (lockPlayerInput && GameManager.Instance)
        {
            GameManager.Instance.isAction = false;
            if (debugLog) Debug.Log($"{ContextTag()} input unlocked", this);
        }

        if (debugLog) Debug.Log($"{ContextTag()} Execute done", this);
    }
}
