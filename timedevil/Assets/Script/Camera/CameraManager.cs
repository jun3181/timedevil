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
    [SerializeField] private bool autoBindPlayerOnSceneLoad = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public CameraModeId CurrentMode { get; private set; } = CameraModeId.Fixed;
    public bool IsTransitioning { get; private set; } = false;

    private Transform _fixedAnchor;
    private Transform _lastFollow;

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

        if (!vcam) vcam = FindObjectOfType<CinemachineVirtualCamera>(true);
        if (!vcam)
        {
            Debug.LogError("[CameraManager] CinemachineVirtualCamera(vcam)를 찾지 못했습니다.");
            return;
        }

        if (!clamp2D)
        {
            clamp2D = vcam.GetComponent<CinemachineClamp2D>();
            if (!clamp2D) clamp2D = vcam.gameObject.AddComponent<CinemachineClamp2D>();
        }

        clamp2D.enabled = false;
        clamp2D.SetBounds(null);

        EnsureFixedAnchor();

        if (forceNoDamping) ApplyNoDamping();

        if (autoBindPlayerOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (autoBindPlayerOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        var player = FindPlayerTransform();
        if (!player) return;

        vcam.Follow = player;
        vcam.PreviousStateIsValid = false;

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) Debug.Log($"[CameraManager] AutoBind Follow -> {player.name} (scene={s.name})");
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
        p.z = _fixedAnchor.position.z; // 2D: -10 유지
        _fixedAnchor.position = p;

        vcam.Follow = _fixedAnchor;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        // ★ 즉시 반영
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
    // ★ Teleport 보정용 API
    // =========================

    /// <summary>
    /// Follow 대상이 “순간이동” 했다는걸 Cinemachine에 알려줌 (카메라가 예전 위치로 끌려가는 현상 방지)
    /// </summary>
    public void NotifyTargetWarp(Transform warpedTarget, Vector3 delta)
    {
        if (!vcam || !warpedTarget) return;
        vcam.OnTargetObjectWarped(warpedTarget, delta);
        vcam.PreviousStateIsValid = false;

        if (debugLog) Debug.Log($"[CameraManager] NotifyTargetWarp target={warpedTarget.name} delta={delta}");
    }

    /// <summary>
    /// 카메라를 즉시 특정 월드 좌표로 “스냅” (Fixed/컷씬/텔레포트 순간 확정용)
    /// </summary>
    public void SnapCameraTo(Vector3 worldPos)
    {
        if (!vcam) return;

        Vector3 p = worldPos;
        p.z = vcam.transform.position.z;

        // 강제 반영
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
