using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public enum CameraModeId
{
    Fixed,
    FollowConfined,
    FollowFree,
    Cutscene
}

[DisallowMultipleComponent]
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Single VCam (씬 오브젝트)")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    [Header("Clamp Extension (Clamp 후처리)")]
    [SerializeField] private CinemachineClamp2D clamp2D;

    [Header("Defaults")]
    [SerializeField] private float defaultOrthoSize = 5f;
    [SerializeField] private bool forceNoDamping = true;

    [Header("Auto (옵션)")]
    [SerializeField] private bool autoBindPlayerOnSceneLoad = false;

    [Header("Rebind (중요)")]
    [Tooltip("씬이 로드될 때마다 vcam을 재탐색해서 재바인드합니다. (배틀씬처럼 vcam이 없는 씬을 거쳐도 복귀 씬에서 자동 회복)")]
    [SerializeField] private bool alwaysRebindVcamOnSceneLoad = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public CameraModeId CurrentMode { get; private set; } = CameraModeId.Fixed;
    public bool IsTransitioning { get; private set; } = false;

    private Transform _fixedAnchor;
    private Transform _lastFollow;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureFixedAnchor();

        // ★ 여기서 vcam이 없어도 "return" 하지 않는다!
        // 배틀씬(VCam 없음)을 거쳐도, 이후 씬에서 재바인드로 회복해야 함.
        EnsureVcam(logWhenMissing: false);

        if (forceNoDamping) ApplyNoDamping();

        // ★ 씬 로드 때마다 자동 재탐색/재바인드
        SceneManager.sceneLoaded += OnSceneLoaded_Rebind;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded_Rebind;
    }

    // =========================================================
    // 씬 로드 시: vcam 재탐색/재바인드 (+옵션으로 Follow 자동 연결)
    // =========================================================
    private void OnSceneLoaded_Rebind(Scene s, LoadSceneMode m)
    {
        if (!alwaysRebindVcamOnSceneLoad && !autoBindPlayerOnSceneLoad)
            return;

        bool ok = EnsureVcam(logWhenMissing: false);

        if (ok && forceNoDamping) ApplyNoDamping();

        if (ok && autoBindPlayerOnSceneLoad)
        {
            var player = FindPlayerTransform();
            if (player)
            {
                vcam.Follow = player;
                vcam.PreviousStateIsValid = false;

                if (debugLog) Debug.Log($"[CameraManager] AutoBind Follow -> {player.name} (scene={s.name})");
            }
        }

        if (debugLog)
        {
            Debug.Log($"[CameraManager] SceneLoaded Rebind (scene={s.name}) vcam={(vcam ? vcam.name : "(none)")} clamp2D={(clamp2D ? "ok" : "(none)")}");
        }
    }

    // =========================================================
    // Transition
    // =========================================================
    public void BeginTransition(bool lockCamera = true)
    {
        if (!EnsureVcam(logWhenMissing: false)) return;

        IsTransitioning = true;
        _lastFollow = vcam.Follow;

        if (lockCamera)
        {
            vcam.Follow = null;
            vcam.PreviousStateIsValid = false;
        }

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log("[CameraManager] BeginTransition()");
    }

    public void EndTransition()
    {
        IsTransitioning = false;
        if (debugLog) Debug.Log("[CameraManager] EndTransition()");
    }

    // =========================================================
    // Mode API
    // =========================================================
    public void SetFixed(Vector3? lockWorldPos = null, float? orthoSize = null)
    {
        if (!EnsureVcam(logWhenMissing: true)) return;

        EnsureFixedAnchor();
        CurrentMode = CameraModeId.Fixed;

        EnsureClamp();
        clamp2D.enabled = false;
        clamp2D.SetBounds(null);

        Vector3 p = lockWorldPos ?? vcam.State.FinalPosition;
        p.z = _fixedAnchor.position.z;
        _fixedAnchor.position = p;

        vcam.Follow = _fixedAnchor;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        vcam.PreviousStateIsValid = false;
        vcam.ForceCameraPosition(new Vector3(p.x, p.y, vcam.transform.position.z), vcam.transform.rotation);

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] Mode=Fixed anchor={_fixedAnchor.position} ortho={vcam.m_Lens.OrthographicSize}");
    }

    public void SetFollowFree(Transform followTarget, float? orthoSize = null)
    {
        if (!EnsureVcam(logWhenMissing: true)) return;

        CurrentMode = CameraModeId.FollowFree;

        if (!followTarget)
        {
            Debug.LogWarning("[CameraManager] SetFollowFree: followTarget이 null");
            return;
        }

        EnsureClamp();
        clamp2D.enabled = false;
        clamp2D.SetBounds(null);

        vcam.Follow = followTarget;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        vcam.PreviousStateIsValid = false;

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] Mode=FollowFree follow={followTarget.name} ortho={vcam.m_Lens.OrthographicSize}");
    }

    public void SetFollowConfined(Transform followTarget, Collider2D bounds, float? orthoSize = null)
    {
        if (!EnsureVcam(logWhenMissing: true)) return;

        CurrentMode = CameraModeId.FollowConfined;

        if (!followTarget)
        {
            Debug.LogWarning("[CameraManager] SetFollowConfined: followTarget이 null");
            return;
        }

        vcam.Follow = followTarget;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        EnsureClamp();
        clamp2D.enabled = true;
        clamp2D.SetBounds(bounds);

        vcam.PreviousStateIsValid = false;

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] Mode=FollowConfined follow={followTarget.name} ortho={vcam.m_Lens.OrthographicSize} bounds={(bounds ? bounds.name : "(null)")}");
    }

    public void SetCutscene(Vector3 worldPos, float? orthoSize = null)
    {
        if (!EnsureVcam(logWhenMissing: true)) return;

        EnsureFixedAnchor();
        CurrentMode = CameraModeId.Cutscene;

        EnsureClamp();
        clamp2D.enabled = false;
        clamp2D.SetBounds(null);

        worldPos.z = _fixedAnchor.position.z;
        _fixedAnchor.position = worldPos;

        vcam.Follow = _fixedAnchor;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        vcam.PreviousStateIsValid = false;
        vcam.ForceCameraPosition(new Vector3(worldPos.x, worldPos.y, vcam.transform.position.z), vcam.transform.rotation);

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] Mode=Cutscene pos={worldPos} ortho={vcam.m_Lens.OrthographicSize}");
    }

    // =========================================================
    // Teleport/Route
    // =========================================================
    public void ApplyAfterTeleport(
        Transform player,
        Vector3 fromPos,
        Vector3 toPos,
        CameraModeId afterMode,
        Collider2D afterBounds,
        float? afterOrthoSize,
        Transform fixedCameraAnchorPoint,
        bool notifyWarpToCinemachine = true,
        bool snapCameraWhenFixed = true
    )
    {
        if (!EnsureVcam(logWhenMissing: true)) return;

        Vector3 delta = toPos - fromPos;
        Vector3 fixedPos = fixedCameraAnchorPoint ? fixedCameraAnchorPoint.position : toPos;

        if (debugLog)
            Debug.Log($"[CameraManager] ApplyAfterTeleport mode={afterMode} from={fromPos} to={toPos} fixedPos={fixedPos} bounds={(afterBounds ? afterBounds.name : "(null)")}");

        switch (afterMode)
        {
            case CameraModeId.Fixed:
                SetFixed(lockWorldPos: fixedPos, orthoSize: afterOrthoSize);
                if (snapCameraWhenFixed) SnapCameraTo(fixedPos);
                break;

            case CameraModeId.FollowConfined:
                SetFollowConfined(player, afterBounds, afterOrthoSize);
                if (notifyWarpToCinemachine && player) NotifyTargetWarp(player, delta);
                break;

            case CameraModeId.FollowFree:
                SetFollowFree(player, afterOrthoSize);
                if (notifyWarpToCinemachine && player) NotifyTargetWarp(player, delta);
                break;

            case CameraModeId.Cutscene:
                SetCutscene(fixedPos, afterOrthoSize);
                if (snapCameraWhenFixed) SnapCameraTo(fixedPos);
                break;
        }
    }

    // =========================================================
    // Warp / Snap
    // =========================================================
    public void NotifyTargetWarp(Transform warpedTarget, Vector3 delta)
    {
        if (!EnsureVcam(logWhenMissing: false)) return;
        if (!warpedTarget) return;

        vcam.OnTargetObjectWarped(warpedTarget, delta);
        vcam.PreviousStateIsValid = false;

        if (debugLog) Debug.Log($"[CameraManager] NotifyTargetWarp target={warpedTarget.name} delta={delta}");
    }

    public void SnapCameraTo(Vector3 worldPos)
    {
        if (!EnsureVcam(logWhenMissing: false)) return;

        Vector3 p = worldPos;
        p.z = vcam.transform.position.z;

        vcam.PreviousStateIsValid = false;
        vcam.ForceCameraPosition(p, vcam.transform.rotation);

        if (debugLog) Debug.Log($"[CameraManager] SnapCameraTo pos={p}");
    }

    // =========================================================
    // Internals
    // =========================================================
    private bool EnsureVcam(bool logWhenMissing)
    {
        // vcam이 살아있으면 OK
        if (vcam) { EnsureClamp(); return true; }

        // 재탐색
        vcam = FindObjectOfType<CinemachineVirtualCamera>(true);

        if (!vcam)
        {
            if (logWhenMissing && debugLog)
                Debug.LogWarning("[CameraManager] EnsureVcam: 이 씬에서 CinemachineVirtualCamera를 찾지 못했습니다. (배틀씬처럼 vcam 없는 씬이면 정상)");
            return false;
        }

        EnsureClamp();

        // 새 vcam 잡았으면 상태 무효화(이전 씬 잔상 방지)
        vcam.PreviousStateIsValid = false;

        if (debugLog) Debug.Log($"[CameraManager] EnsureVcam: rebound -> {vcam.name}");
        return true;
    }

    private void EnsureClamp()
    {
        if (!vcam) return;

        if (!clamp2D || clamp2D.gameObject != vcam.gameObject)
        {
            clamp2D = vcam.GetComponent<CinemachineClamp2D>();
            if (!clamp2D) clamp2D = vcam.gameObject.AddComponent<CinemachineClamp2D>();

            if (debugLog) Debug.Log("[CameraManager] EnsureClamp: clamp2D attached/rebound");
        }
    }

    private void EnsureFixedAnchor()
    {
        if (_fixedAnchor) return;

        var go = new GameObject("CameraFixedAnchor");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(0f, 0f, -10f);
        _fixedAnchor = go.transform;
    }

    private void ApplyNoDamping()
    {
        if (!vcam) return;

        var framing = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framing != null)
        {
            framing.m_XDamping = 0f;
            framing.m_YDamping = 0f;
            framing.m_ZDamping = 0f;
            return;
        }

        var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            transposer.m_XDamping = 0f;
            transposer.m_YDamping = 0f;
            transposer.m_ZDamping = 0f;
        }
    }

    private Transform FindPlayerTransform()
    {
        var pm = FindObjectOfType<PlayerMove>(true);
        return pm ? pm.transform : null;
    }
}
