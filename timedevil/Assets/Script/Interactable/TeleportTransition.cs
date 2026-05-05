// Assets/Script/Interactable/TeleportTransition.cs
using System.Collections;
using UnityEngine;

public class TeleportTransition : MonoBehaviour, IInteractable
{
    [Header("Teleport Target (플레이어 이동 위치)")]
    public Transform targetPoint;

    [Header("Fade (in-scene)")]
    [Tooltip("Teleport 연출에 쓸 FadePanelFader (없으면 씬에서 자동 탐색)")]
    public FadePanelFader fadePanel;
    public float fadeOutDuration = 0.25f;
    public float fadeInDuration = 0.25f;

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

    private string ContextTag()
    {
        return $"[TeleportTransition] scene={gameObject.scene.name} object={name}";
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

    private void Awake()
    {
        if (!fadePanel)
            fadePanel = FindObjectOfType<FadePanelFader>(true);

        TryAutoResolveTargetPoint();
    }

    private void OnEnable()
    {
        // 씬 복귀 후 참조가 비어 있는 케이스를 방어
        TryAutoResolveTargetPoint();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryAutoResolveTargetPoint();
    }
#endif

    private bool TryAutoResolveTargetPoint()
    {
        if (targetPoint) return true;

        // 1) 같은 오브젝트 이름 규칙 우선
        var direct = transform.Find("TargetPoint");
        if (!direct) direct = transform.Find("targetPoint");

        // 2) 자식 전체에서 이름 기준 보강 탐색
        if (!direct)
        {
            var children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                var c = children[i];
                if (c == transform) continue;
                if (c.name == "TargetPoint" || c.name == "targetPoint")
                {
                    direct = c;
                    break;
                }
            }
        }

        if (direct)
        {
            targetPoint = direct;
            if (debugLog)
                Debug.Log($"[TeleportTransition] Auto-resolved targetPoint: {targetPoint.name}", this);
            return true;
        }

        return false;
    }

    public void Interact()
    {
        if (_running)
        {
            if (debugLog) Debug.Log($"{ContextTag()} Interact ignored: already running", this);
            return;
        }

        if (debugLog)
            Debug.Log($"{ContextTag()} Interact start target={(targetPoint ? targetPoint.name : "<null>")} mode={afterMode} bounds={(afterBounds ? afterBounds.name : "<null>")} fixedAnchor={(fixedCameraAnchorPoint ? fixedCameraAnchorPoint.name : "<null>")}", this);

        if (!targetPoint && !TryAutoResolveTargetPoint())
        {
            Debug.LogWarning($"[TeleportTransition] targetPoint가 비어있음 (object={name}, scene={gameObject.scene.name})", this);
            return;
        }

        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
        {
            if (debugLog) Debug.Log($"{ContextTag()} blocked: dialogue active", this);
            return;
        }

        StartCoroutine(CoTeleport());
    }

    private IEnumerator CoTeleport()
    {
        _running = true;
        if (debugLog) Debug.Log($"{ContextTag()} CoTeleport begin", this);

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
        else if (debugLog)
        {
            Debug.LogWarning($"{ContextTag()} CameraManager.Instance is null", this);
        }

        //  Teleport는 SceneFader가 아니라 FadePanelFader
        if (fadePanel != null)
        {
            if (debugLog) Debug.Log($"{ContextTag()} fade out start duration={fadeOutDuration}", this);
            yield return fadePanel.FadeTo(1f, fadeOutDuration);
            if (debugLog) Debug.Log($"{ContextTag()} fade out end", this);
        }

        var player = FindObjectOfType<PlayerMove>(true);
        if (!player)
        {
            Debug.LogWarning("[TeleportTransition] PlayerMove를 찾지 못했습니다.");
            if (fadePanel != null)
                yield return fadePanel.FadeTo(0f, fadeInDuration);

            if (lockPlayerInput && GameManager.Instance) GameManager.Instance.isAction = false;
            _running = false;
            yield break;
        }

        Transform playerTr = player.transform;

        if (debugLog)
            Debug.Log($"{ContextTag()} resolved PlayerMove name={player.name} id={player.GetInstanceID()} active={player.gameObject.activeInHierarchy} path={BuildTransformPath(playerTr)} scene={player.gameObject.scene.name}", this);

        Vector3 from = playerTr.position;
        Vector3 to = targetPoint.position;

        if (debugLog)
            Debug.Log($"{ContextTag()} teleport player from={from} to={to} targetScene={targetPoint.gameObject.scene.name}", this);

        // 1) 플레이어 이동
        playerTr.position = to;
        if (debugLog) Debug.Log($"{ContextTag()} player position applied current={playerTr.position}", this);

        // 2) 카메라 적용 (CameraManager가 책임)
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
            {
                Debug.Log($"{ContextTag()} ApplyAfterTeleport mode={afterMode} bounds={(afterBounds ? afterBounds.name : "<null>")} ortho={(size.HasValue ? size.Value.ToString() : "<keep>")} fixedAnchor={(fixedCameraAnchorPoint ? fixedCameraAnchorPoint.position.ToString() : "<null>")} notifyWarp={notifyWarpToCinemachine} snapFixed={snapCameraWhenFixed}", this);
            }

            CameraManager.Instance.EndTransition();
            if (debugLog) Debug.Log($"{ContextTag()} CameraManager.EndTransition", this);
        }

        if (applyDarkOverlay && DarkOverlay.Instance != null)
        {
            if (debugLog) Debug.Log($"{ContextTag()} DarkOverlay alpha={darkOverlayAlpha} duration={darkOverlayDuration}", this);
            DarkOverlay.Instance.SetAlpha(darkOverlayAlpha, darkOverlayDuration);
        }

        if (fadePanel != null)
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

        _running = false;
        if (debugLog) Debug.Log($"{ContextTag()} Done");
    }
}
