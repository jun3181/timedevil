// Assets/Script/Trigger/teleport/TriggerStep_PlayerTeleport.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_PlayerTeleport : TriggerStepBase
{
    [Header("Teleport Target")]
    [SerializeField] private Transform targetPoint;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("After Teleport Facing")]
    [Tooltip("KeepCurrent이면 텔레포트 전 바라보던 방향을 유지합니다.")]
    [SerializeField] private TeleportArrivalFacing afterFacing = TeleportArrivalFacing.KeepCurrent;

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

    [Header("Ambient Dark Overlay (상태 유지용)")]
    [SerializeField] private bool applyDarkOverlay = false;
    [Range(0f, 1f)]
    [SerializeField] private float darkOverlayAlpha = 0.35f;
    [SerializeField] private float darkOverlayDuration = 0.15f;

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
            Debug.Log($"{ContextTag()} Execute start target={(targetPoint ? targetPoint.name : "<null>")} offset={offset} mode={afterMode} facing={afterFacing}", this);

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
            Debug.Log($"{ContextTag()} player={playerTr.name} from={from} to={to} targetScene={targetPoint.gameObject.scene.name} mode={afterMode} facing={afterFacing} bounds={(afterBounds ? afterBounds.name : "<null>")} fixedAnchor={(fixedCameraAnchorPoint ? fixedCameraAnchorPoint.name : "<null>")}", this);

        bool heldInputLock = false;
        if (lockPlayerInput && GameManager.Instance)
        {
            GameManager.Instance.LockAction();
            heldInputLock = true;
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
        TeleportArrivalFacingUtility.Apply(playerTr, afterFacing);
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

        if (applyDarkOverlay && DarkOverlay.Instance != null)
        {
            if (debugLog) Debug.Log($"{ContextTag()} DarkOverlay alpha={darkOverlayAlpha} duration={darkOverlayDuration}", this);
            DarkOverlay.Instance.SetAlpha(darkOverlayAlpha, darkOverlayDuration);
        }

        if (useFade && fadePanel != null)
        {
            if (debugLog) Debug.Log($"{ContextTag()} fade in start duration={fadeInDuration}", this);
            yield return fadePanel.FadeTo(0f, fadeInDuration);
            if (debugLog) Debug.Log($"{ContextTag()} fade in end", this);
        }

        if (heldInputLock && GameManager.Instance)
        {
            GameManager.Instance.UnlockAction();
            if (debugLog) Debug.Log($"{ContextTag()} input unlocked", this);
        }

        if (debugLog) Debug.Log($"{ContextTag()} Execute done", this);
    }
}

public enum TeleportArrivalFacing
{
    KeepCurrent,
    Up,
    Down,
    Left,
    Right
}

internal static class TeleportArrivalFacingUtility
{
    public static void Apply(Transform player, TeleportArrivalFacing facing)
    {
        if (!player || facing == TeleportArrivalFacing.KeepCurrent)
            return;

        Vector3 direction = ToVector(facing);

        PlayerMove move = player.GetComponent<PlayerMove>();
        if (!move) move = player.GetComponentInParent<PlayerMove>();
        if (!move) move = player.GetComponentInChildren<PlayerMove>(true);
        if (move) move.SetFacing(direction);

        PlayerAction action = player.GetComponent<PlayerAction>();
        if (!action) action = player.GetComponentInParent<PlayerAction>();
        if (!action) action = player.GetComponentInChildren<PlayerAction>(true);
        if (action) action.SetFacing(direction);

        if (!move && !action)
            ApplyAnimatorFallback(player, direction);
    }

    private static Vector3 ToVector(TeleportArrivalFacing facing)
    {
        switch (facing)
        {
            case TeleportArrivalFacing.Up: return Vector3.up;
            case TeleportArrivalFacing.Down: return Vector3.down;
            case TeleportArrivalFacing.Left: return Vector3.left;
            case TeleportArrivalFacing.Right: return Vector3.right;
            default: return Vector3.zero;
        }
    }

    private static void ApplyAnimatorFallback(Transform player, Vector3 direction)
    {
        if (!PlayerFacingMath.TryResolveCardinal(direction, out _, out int hAxis, out int vAxis, out _))
            return;

        Animator anim = player.GetComponent<Animator>();
        if (!anim) anim = player.GetComponentInChildren<Animator>(true);
        if (!anim) return;

        anim.SetInteger("hAxisRaw", hAxis);
        anim.SetInteger("vAxisRaw", vAxis);
        anim.SetBool("isChange", false);
    }
}
