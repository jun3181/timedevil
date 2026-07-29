using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_StayThenTeleport : TriggerStepBase
{
    [Header("Stay Check")]
    [Min(0f)]
    [SerializeField] private float staySeconds = 3f;
    [SerializeField] private bool useUnscaledTime = false;
    [Tooltip("비워두면 이 Step을 실행한 TriggerGet의 Collider2D를 사용합니다.")]
    [SerializeField] private Collider2D stayArea;

    [Header("Teleport")]
    [SerializeField] private Transform targetPoint;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Fade")]
    [SerializeField] private bool useFade = false;
    [SerializeField] private FadePanelFader fadePanel;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float fadeInDuration = 0.25f;

    [Header("Input Lock During Teleport")]
    [SerializeField] private bool lockPlayerInputDuringTeleport = true;

    [Header("After Direct Teleport Camera Mode")]
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

    [Header("Rigidbody")]
    [SerializeField] private bool zeroVelocityAfterTeleport = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override bool AllowPlayerInputWhileExecuting => true;

    private void Reset()
    {
        stayArea = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (!stayArea)
            stayArea = GetComponent<Collider2D>();

        if (!fadePanel)
            fadePanel = FindObjectOfType<FadePanelFader>(true);
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        Collider2D area = ResolveStayArea(ctx);
        Collider2D playerCollider = ResolvePlayerCollider(ctx);
        Transform playerTransform = ResolvePlayerTransform(ctx);

        if (!area)
        {
            Debug.LogWarning("[TriggerStep_StayThenTeleport] stayArea가 비어있습니다. TriggerGet 또는 이 오브젝트에 Collider2D를 연결하세요.", this);
            yield break;
        }

        if (!playerCollider || !playerTransform)
        {
            Debug.LogWarning("[TriggerStep_StayThenTeleport] 플레이어 Collider2D/Transform을 찾지 못했습니다.", this);
            yield break;
        }

        if (!IsPlayerInside(area, playerCollider))
        {
            if (debugLog) Debug.Log("[TriggerStep_StayThenTeleport] 플레이어가 이미 영역을 벗어나 텔레포트를 취소합니다.", this);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < staySeconds)
        {
            if (!area || !playerCollider || !IsPlayerInside(area, playerCollider))
            {
                if (debugLog) Debug.Log($"[TriggerStep_StayThenTeleport] {elapsed:0.###}초 후 영역 이탈 -> 텔레포트 취소", this);
                yield break;
            }

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (!area || !playerCollider || !IsPlayerInside(area, playerCollider))
        {
            if (debugLog) Debug.Log("[TriggerStep_StayThenTeleport] 대기 완료 직후 영역 이탈 -> 텔레포트 취소", this);
            yield break;
        }

        if (debugLog) Debug.Log($"[TriggerStep_StayThenTeleport] {staySeconds:0.###}초 동안 영역 유지 -> 텔레포트 실행", this);

        if (!targetPoint)
        {
            Debug.LogWarning("[TriggerStep_StayThenTeleport] targetPoint가 필요합니다.", this);
            yield break;
        }

        Vector3 from = playerTransform.position;
        Vector3 to = targetPoint.position + (Vector3)offset;
        bool heldInputLock = false;

        if (lockPlayerInputDuringTeleport && GameManager.Instance)
        {
            GameManager.Instance.LockAction();
            heldInputLock = true;
        }

        if (CameraManager.Instance)
            CameraManager.Instance.BeginTransition(lockCamera: true);

        if (useFade && fadePanel != null)
            yield return fadePanel.FadeTo(1f, fadeOutDuration);

        playerTransform.position = to;

        if (zeroVelocityAfterTeleport)
        {
            Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
            if (rb) rb.velocity = Vector2.zero;
        }

        if (CameraManager.Instance)
        {
            float? size = (afterOrthoSize > 0f) ? afterOrthoSize : (float?)null;

            CameraManager.Instance.ApplyAfterTeleport(
                player: playerTransform,
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

        if (heldInputLock && GameManager.Instance)
            GameManager.Instance.UnlockAction();
    }

    private Collider2D ResolveStayArea(TriggerContext ctx)
    {
        if (stayArea) return stayArea;
        return ctx?.trigger ? ctx.trigger.GetComponent<Collider2D>() : null;
    }

    private static Collider2D ResolvePlayerCollider(TriggerContext ctx)
    {
        if (ctx?.instigatorCollider) return ctx.instigatorCollider;
        PlayerMove pm = Object.FindObjectOfType<PlayerMove>(true);
        return pm ? pm.GetComponent<Collider2D>() : null;
    }

    private static Transform ResolvePlayerTransform(TriggerContext ctx)
    {
        if (ctx?.player) return ctx.player;
        PlayerMove pm = Object.FindObjectOfType<PlayerMove>(true);
        return pm ? pm.transform : null;
    }

    private static bool IsPlayerInside(Collider2D area, Collider2D playerCollider)
    {
        return area.enabled && playerCollider.enabled && area.IsTouching(playerCollider);
    }
}