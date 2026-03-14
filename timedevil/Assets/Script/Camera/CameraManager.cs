// Assets/Script/Camera/CameraManager.cs
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

    [Header("Single VCam")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    [Header("Clamp Extension (Clamp 후처리)")]
    [SerializeField] private CinemachineClamp2D clamp2D;

    [Header("Defaults")]
    [SerializeField] private float defaultOrthoSize = 5f;
    [SerializeField] private bool forceNoDamping = true;

    [Header("Auto (옵션)")]
    [Tooltip("씬 로드될 때마다 Player를 Follow로 자동 바인딩")]
    [SerializeField] private bool autoBindPlayerOnSceneLoad = false;

    [Tooltip("씬 로드될 때마다 vcam을 재탐색 (배틀씬처럼 vcam 없는 씬 왕복 대응)")]
    [SerializeField] private bool reacquireVcamOnSceneLoad = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public CameraModeId CurrentMode { get; private set; } = CameraModeId.Fixed;
    public bool IsTransitioning { get; private set; } = false;

    private Transform _fixedAnchor;
    private Transform _lastFollow;

    //  FollowConfined 스냅샷용(Clamp2D 내부 private boundsShape에 접근 못하니, 여기서 마지막 bounds를 기억)
    private Collider2D _lastConfineBounds;
    private string _lastConfineBoundsName = "";

    private void Reset()
    {
        vcam ??= FindObjectOfType<CinemachineVirtualCamera>(true);
        if (vcam)
        {
            clamp2D ??= vcam.GetComponent<CinemachineClamp2D>();
            if (!clamp2D) clamp2D = vcam.gameObject.AddComponent<CinemachineClamp2D>();
        }
    }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureFixedAnchor();

        //  씬 로드시마다 vcam 재탐색을 위해 항상 구독
        SceneManager.sceneLoaded += HandleSceneLoaded;

        // 첫 씬에서도 최대한 잡아봄
        ReacquireVcam(null, logWhenMissing: false);

        if (forceNoDamping) ApplyNoDamping();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void HandleSceneLoaded(Scene s, LoadSceneMode m)
    {
        //  배틀씬처럼 vcam이 없는 씬 갔다가 돌아오면 여기서 다시 잡아야 함
        if (reacquireVcamOnSceneLoad)
            ReacquireVcam(null, logWhenMissing: false);

        // (옵션) 플레이어 자동 Follow
        if (autoBindPlayerOnSceneLoad && vcam)
        {
            var player = FindPlayerTransform();
            if (player)
            {
                vcam.Follow = player;
                vcam.PreviousStateIsValid = false;

                if (debugLog) Debug.Log($"[CameraManager] AutoBind Follow -> {player.name} (scene={s.name})");
            }
        }

        if (forceNoDamping) ApplyNoDamping();

        if (debugLog)
            Debug.Log($"[CameraManager] sceneLoaded => '{s.name}' vcam={(vcam ? vcam.name : "(null)")} clamp={(clamp2D ? clamp2D.name : "(null)")}");

        // 씬이 바뀌면서 bounds 콜라이더가 파괴됐을 수도 있으니 Unity-null 체크로 정리
        if (_lastConfineBounds == null) { /* Unity null OK */ }
    }

    /// <summary>
    ///  씬이 바뀌어도 vcam 참조가 깨지지 않게, 현재 씬에서 다시 CinemachineVirtualCamera를 찾는다.
    /// preferredVcamName이 있으면 그 이름 우선, 없으면 첫 번째 vcam.
    /// </summary>
    public bool ReacquireVcam(string preferredVcamName = null, bool logWhenMissing = true)
    {
        // 살아있는 참조면 유지 (Unity null 비교)
        if (vcam != null && vcam.gameObject != null)
            return true;

        CinemachineVirtualCamera found = null;

        var all = FindObjectsOfType<CinemachineVirtualCamera>(true);
        if (all != null && all.Length > 0)
        {
            // 이름 우선
            if (!string.IsNullOrWhiteSpace(preferredVcamName))
            {
                foreach (var c in all)
                {
                    if (c && c.name == preferredVcamName) { found = c; break; }
                }
            }

            // 못 찾았으면 첫 번째
            if (!found)
            {
                foreach (var c in all)
                {
                    if (c) { found = c; break; }
                }
            }
        }

        vcam = found;

        if (!vcam)
        {
            if (logWhenMissing)
                Debug.LogWarning($"[CameraManager] ReacquireVcam: vcam not found (preferred='{preferredVcamName ?? "null"}')");
            return false;
        }

        // clamp2D 보장
        if (!clamp2D)
        {
            clamp2D = vcam.GetComponent<CinemachineClamp2D>();
            if (!clamp2D) clamp2D = vcam.gameObject.AddComponent<CinemachineClamp2D>();
        }

        if (forceNoDamping) ApplyNoDamping();

        if (debugLog)
            Debug.Log($"[CameraManager] ReacquireVcam -> '{vcam.name}' (preferred='{preferredVcamName ?? "null"}')");

        return true;
    }

    // =========================
    //  Snapshot API (TriggerStep_Scene에서 쓰는 함수)
    // =========================
    public bool TryGetSnapshot(
        out CameraModeId camMode,
        out float camOrtho,
        out Vector3 fixedPos,
        out string boundsName
    )
    {
        camMode = CurrentMode;
        camOrtho = 0f;
        fixedPos = Vector3.zero;
        boundsName = "";

        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
        if (!vcam) return false;

        camOrtho = vcam.m_Lens.OrthographicSize;

        // Fixed/Cutscene이면 "고정 앵커"를 저장
        if (CurrentMode == CameraModeId.Fixed || CurrentMode == CameraModeId.Cutscene)
        {
            fixedPos = _fixedAnchor ? _fixedAnchor.position : vcam.State.FinalPosition;
        }
        else
        {
            // Follow 계열이면 참고용으로 현재 카메라 위치
            fixedPos = vcam.State.FinalPosition;
        }

        // FollowConfined이면 마지막으로 지정된 bounds 이름 저장
        if (CurrentMode == CameraModeId.FollowConfined)
        {
            if (_lastConfineBounds) boundsName = _lastConfineBounds.name;
            else boundsName = _lastConfineBoundsName ?? "";
        }

        return true;
    }

    // =========================
    // Transition
    // =========================
    public void BeginTransition(bool lockCamera = true)
    {
        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
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
        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
        if (!vcam) return;

        EnsureFixedAnchor();
        CurrentMode = CameraModeId.Fixed;

        _lastConfineBounds = null;
        _lastConfineBoundsName = "";

        if (clamp2D)
        {
            clamp2D.enabled = false;
            clamp2D.SetBounds(null);
        }

        Vector3 p = lockWorldPos ?? vcam.State.FinalPosition;
        p.z = _fixedAnchor.position.z; // 2D: -10 유지
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
        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
        if (!vcam) return;

        CurrentMode = CameraModeId.FollowFree;

        _lastConfineBounds = null;
        _lastConfineBoundsName = "";

        if (!followTarget)
        {
            Debug.LogWarning("[CameraManager] SetFollowFree: followTarget이 null");
            return;
        }

        if (clamp2D)
        {
            clamp2D.enabled = false;
            clamp2D.SetBounds(null);
        }

        vcam.Follow = followTarget;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        vcam.PreviousStateIsValid = false;

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] Mode=FollowFree follow={followTarget.name} ortho={vcam.m_Lens.OrthographicSize}");
    }

    public void SetFollowConfined(Transform followTarget, Collider2D bounds, float? orthoSize = null)
    {
        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
        if (!vcam) return;

        CurrentMode = CameraModeId.FollowConfined;

        if (!followTarget)
        {
            Debug.LogWarning("[CameraManager] SetFollowConfined: followTarget이 null");
            return;
        }

        //  스냅샷용으로 기억
        _lastConfineBounds = bounds;
        _lastConfineBoundsName = bounds ? bounds.name : "";

        vcam.Follow = followTarget;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        if (clamp2D)
        {
            clamp2D.enabled = true;
            clamp2D.SetBounds(bounds);
        }

        vcam.PreviousStateIsValid = false;

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] Mode=FollowConfined follow={followTarget.name} ortho={vcam.m_Lens.OrthographicSize} bounds={(bounds ? bounds.name : "(null)")}");
    }

    public void SetCutscene(Vector3 worldPos, float? orthoSize = null)
    {
        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
        if (!vcam) return;

        EnsureFixedAnchor();
        CurrentMode = CameraModeId.Cutscene;

        _lastConfineBounds = null;
        _lastConfineBoundsName = "";

        if (clamp2D)
        {
            clamp2D.enabled = false;
            clamp2D.SetBounds(null);
        }

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
    // Teleport/Route 요청
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
        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
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
                if (player)
                {
                    SetFollowConfined(player, afterBounds, afterOrthoSize);
                    if (notifyWarpToCinemachine) NotifyTargetWarp(player, delta);
                }
                break;

            case CameraModeId.FollowFree:
                if (player)
                {
                    SetFollowFree(player, afterOrthoSize);
                    if (notifyWarpToCinemachine) NotifyTargetWarp(player, delta);
                }
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
        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
        if (!vcam || !warpedTarget) return;

        vcam.OnTargetObjectWarped(warpedTarget, delta);
        vcam.PreviousStateIsValid = false;

        if (debugLog) Debug.Log($"[CameraManager] NotifyTargetWarp target={warpedTarget.name} delta={delta}");
    }

    public void SnapCameraTo(Vector3 worldPos)
    {
        if (!vcam) ReacquireVcam(null, logWhenMissing: false);
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
