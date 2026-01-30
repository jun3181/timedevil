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

    [Header("Single VCam (씬마다 존재 / 배틀씬엔 없을 수 있음)")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    [Header("Clamp Extension (Clamp 후처리)")]
    [SerializeField] private CinemachineClamp2D clamp2D;

    [Header("Defaults")]
    [SerializeField] private float defaultOrthoSize = 5f;
    [SerializeField] private bool forceNoDamping = true;

    [Header("Scene Load Behavior")]
    [Tooltip("씬 로드 때마다 vcam을 다시 찾습니다. (배틀씬처럼 vcam이 없는 씬이 있어도 OK)")]
    [SerializeField] private bool alwaysReacquireVcamOnSceneLoad = true;

    [Tooltip("이름이 같으면 그 vcam을 우선적으로 잡습니다. 비우면 자동 선택합니다.")]
    [SerializeField] private string preferVcamNameOnSceneLoad = "";

    [Tooltip("씬 로드 시 Follow를 자동으로 Player로 묶고 싶으면 켜세요. (SceneCameraBootstrap 쓰면 보통 OFF 권장)")]
    [SerializeField] private bool autoBindPlayerOnSceneLoad = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool logWhenVcamMissing = false;

    public CameraModeId CurrentMode { get; private set; } = CameraModeId.Fixed;
    public bool IsTransitioning { get; private set; } = false;

    private Transform _fixedAnchor;
    private Transform _lastFollow;

    private void Reset()
    {
        vcam = FindObjectOfType<CinemachineVirtualCamera>(true);
        if (vcam)
        {
            clamp2D = vcam.GetComponent<CinemachineClamp2D>();
            if (!clamp2D) clamp2D = vcam.gameObject.AddComponent<CinemachineClamp2D>();
        }
    }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureFixedAnchor();

        // 초기에도 한번 잡기(첫 씬에 vcam 있으면 연결)
        ReacquireVcam(string.IsNullOrWhiteSpace(preferVcamNameOnSceneLoad) ? null : preferVcamNameOnSceneLoad, logWhenMissing: false);

        if (forceNoDamping) ApplyNoDamping();

        // 씬 로드마다 재탐색을 위해 항상 구독
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (alwaysReacquireVcamOnSceneLoad)
        {
            ReacquireVcam(
                preferName: string.IsNullOrWhiteSpace(preferVcamNameOnSceneLoad) ? null : preferVcamNameOnSceneLoad,
                logWhenMissing: logWhenVcamMissing
            );
        }

        if (!autoBindPlayerOnSceneLoad) return;
        if (!vcam) return;

        var player = FindPlayerTransform();
        if (!player) return;

        vcam.Follow = player;
        vcam.PreviousStateIsValid = false;

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] AutoBind Follow -> {player.name} (scene={s.name})");
    }

    // =========================
    // vcam 재탐색 API
    // =========================
    public bool ReacquireVcam(string preferName = null, bool logWhenMissing = false)
    {
        var newVcam = FindBestVcam(preferName);

        if (!newVcam)
        {
            vcam = null;
            clamp2D = null;

            if (logWhenMissing)
                Debug.LogWarning("[CameraManager] VCam not found in this scene. (OK for BattleScene)");
            return false;
        }

        bool changed = (vcam != newVcam);
        vcam = newVcam;

        clamp2D = vcam.GetComponent<CinemachineClamp2D>();
        if (!clamp2D) clamp2D = vcam.gameObject.AddComponent<CinemachineClamp2D>();

        clamp2D.enabled = false;
        clamp2D.SetBounds(null);

        vcam.PreviousStateIsValid = false;
        if (forceNoDamping) ApplyNoDamping();

        if (debugLog)
            Debug.Log($"[CameraManager] ReacquireVcam -> {(vcam ? vcam.name : "(null)")} (changed={changed})");

        return true;
    }

    private CinemachineVirtualCamera FindBestVcam(string preferName)
    {
        var vcams = FindObjectsOfType<CinemachineVirtualCamera>(true);
        if (vcams == null || vcams.Length == 0) return null;

        // 1) 이름 우선
        if (!string.IsNullOrWhiteSpace(preferName))
        {
            for (int i = 0; i < vcams.Length; i++)
            {
                var v = vcams[i];
                if (v && v.name == preferName) return v;
            }
        }

        // 2) Priority 가장 높은 것 선택 (동률이면 active 우선)
        CinemachineVirtualCamera best = null;
        for (int i = 0; i < vcams.Length; i++)
        {
            var v = vcams[i];
            if (!v) continue;

            if (best == null) { best = v; continue; }

            if (v.Priority > best.Priority) best = v;
            else if (v.Priority == best.Priority)
            {
                bool vActive = v.gameObject.activeInHierarchy;
                bool bestActive = best.gameObject.activeInHierarchy;
                if (vActive && !bestActive) best = v;
            }
        }

        return best;
    }

    // =========================
    // Transition
    // =========================
    public void BeginTransition(bool lockCamera = true)
    {
        if (!vcam) return;

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

    // =========================
    // Mode API
    // =========================
    public void SetFixed(Vector3? lockWorldPos = null, float? orthoSize = null)
    {
        if (!vcam) return;

        EnsureFixedAnchor();
        CurrentMode = CameraModeId.Fixed;

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
        if (!vcam) return;
        CurrentMode = CameraModeId.FollowFree;

        if (!followTarget)
        {
            Debug.LogWarning("[CameraManager] SetFollowFree: followTarget이 null");
            return;
        }

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
        if (!vcam) return;
        CurrentMode = CameraModeId.FollowConfined;

        if (!followTarget)
        {
            Debug.LogWarning("[CameraManager] SetFollowConfined: followTarget이 null");
            return;
        }

        vcam.Follow = followTarget;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        clamp2D.enabled = true;
        clamp2D.SetBounds(bounds);

        vcam.PreviousStateIsValid = false;

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] Mode=FollowConfined follow={followTarget.name} ortho={vcam.m_Lens.OrthographicSize} bounds={(bounds ? bounds.name : "(null)")}");
    }

    public void SetCutscene(Vector3 worldPos, float? orthoSize = null)
    {
        if (!vcam) return;

        EnsureFixedAnchor();
        CurrentMode = CameraModeId.Cutscene;

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

    // =========================
    // ✅ Teleport After-Apply (네 Teleport 코드들이 요구하는 함수)
    // =========================
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
        if (!vcam) return;

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

    // =========================
    // Warp / Snap
    // =========================
    public void NotifyTargetWarp(Transform warpedTarget, Vector3 delta)
    {
        if (!vcam || !warpedTarget) return;
        vcam.OnTargetObjectWarped(warpedTarget, delta);
        vcam.PreviousStateIsValid = false;

        if (debugLog) Debug.Log($"[CameraManager] NotifyTargetWarp target={warpedTarget.name} delta={delta}");
    }

    public void SnapCameraTo(Vector3 worldPos)
    {
        if (!vcam) return;

        Vector3 p = worldPos;
        p.z = vcam.transform.position.z;

        vcam.PreviousStateIsValid = false;
        vcam.ForceCameraPosition(p, vcam.transform.rotation);

        if (debugLog) Debug.Log($"[CameraManager] SnapCameraTo pos={p}");
    }

    // =========================
    // Internals
    // =========================
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
