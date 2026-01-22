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

    [Header("Optional Components on VCam")]
    [SerializeField] private CinemachineConfiner2D confiner2D;

    [Header("Defaults")]
    [SerializeField] private float defaultOrthoSize = 5f;
    [SerializeField] private bool forceNoDamping = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public CameraModeId CurrentMode { get; private set; } = CameraModeId.Fixed;
    public bool IsTransitioning { get; private set; } = false;

    private void Reset()
    {
        vcam ??= FindObjectOfType<CinemachineVirtualCamera>(true);
        if (vcam) confiner2D ??= vcam.GetComponent<CinemachineConfiner2D>();
    }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RebindVcamIfNeeded();

        if (!vcam)
        {
            Debug.LogError("[CameraManager] vcam이 없습니다. 씬에 CinemachineVirtualCamera가 1개 있어야 합니다.");
            return;
        }

        if (forceNoDamping) ApplyNoDamping();

        // 씬이 바뀌면(다른 씬에 vcam이 있을 수 있으니) 재탐색 가능하게
        SceneManager.sceneLoaded += (_, __) => RebindVcamIfNeeded();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= (_, __) => RebindVcamIfNeeded();
    }

    private void RebindVcamIfNeeded()
    {
        if (!vcam) vcam = FindObjectOfType<CinemachineVirtualCamera>(true);
        if (vcam && !confiner2D) confiner2D = vcam.GetComponent<CinemachineConfiner2D>();

        if (debugLog && vcam)
        {
            Debug.Log($"[CameraManager] Rebind vcam='{vcam.name}', confiner={(confiner2D ? "exists" : "none")}");
        }
    }

    // ===== Transition (텔레포트 중 잠깐 카메라 고정) =====
    public void BeginTransition(bool lockCamera = true)
    {
        if (!vcam) return;
        IsTransitioning = true;

        if (lockCamera)
        {
            vcam.Follow = null;
            if (forceNoDamping) ApplyNoDamping();
        }

        if (debugLog) DumpState("BeginTransition");
    }

    public void EndTransition()
    {
        IsTransitioning = false;
        if (debugLog) DumpState("EndTransition");
    }

    // ===== Modes =====
    public void SetFixed(Vector3? lockWorldPos = null, float? orthoSize = null, bool disableConfiner = true)
    {
        if (!vcam) return;

        CurrentMode = CameraModeId.Fixed;
        vcam.Follow = null;

        if (lockWorldPos.HasValue)
        {
            var p = lockWorldPos.Value;
            p.z = vcam.transform.position.z;
            vcam.transform.position = p;
        }

        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        if (disableConfiner) SetConfiner(false, null);

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) DumpState("SetFixed");
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

        // bounds가 null이면 Confiner를 켜봤자 의미 없으니 꺼버림
        if (bounds) SetConfiner(true, bounds);
        else SetConfiner(false, null);

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) DumpState("SetFollowConfined");
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

        vcam.Follow = followTarget;
        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        SetConfiner(false, null);

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) DumpState("SetFollowFree");
    }

    public void SetCutscene(Vector3 worldPos, float? orthoSize = null, bool useConfiner = false, Collider2D bounds = null)
    {
        if (!vcam) return;

        CurrentMode = CameraModeId.Cutscene;

        vcam.Follow = null;

        worldPos.z = vcam.transform.position.z;
        vcam.transform.position = worldPos;

        vcam.m_Lens.OrthographicSize = orthoSize ?? defaultOrthoSize;

        if (useConfiner && bounds) SetConfiner(true, bounds);
        else SetConfiner(false, null);

        if (forceNoDamping) ApplyNoDamping();
        if (debugLog) DumpState("SetCutscene");
    }

    // 텔레포트(워프)하면 Cinemachine 내부 상태도 갱신해주는 게 안전
    public void NotifyTargetWarp(Transform target, Vector3 delta)
    {
        if (!vcam || !target) return;
        vcam.OnTargetObjectWarped(target, delta);
    }

    // ===== Internals =====
    private void SetConfiner(bool enable, Collider2D shape)
    {
        if (!vcam) return;

        // “없으면 추가”까지 자동으로 해주면 셋업 실수로 인한 디버깅이 줄어듦
        if (!confiner2D) confiner2D = vcam.GetComponent<CinemachineConfiner2D>();
        if (!confiner2D && enable) confiner2D = vcam.gameObject.AddComponent<CinemachineConfiner2D>();

        if (!confiner2D) return;

        confiner2D.enabled = enable;

        if (enable)
        {
            confiner2D.m_BoundingShape2D = shape;
            confiner2D.InvalidateCache();
        }
        else
        {
            confiner2D.m_BoundingShape2D = null;
        }
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

    public void DumpState(string tag)
    {
        string follow = vcam && vcam.Follow ? vcam.Follow.name : "(null)";
        string conf = confiner2D ? (confiner2D.enabled ? "ON" : "OFF") : "none";
        string shape = (confiner2D && confiner2D.m_BoundingShape2D) ? confiner2D.m_BoundingShape2D.name : "(null)";
        float ortho = vcam ? vcam.m_Lens.OrthographicSize : -1f;

        Debug.Log($"[CameraManager] {tag} mode={CurrentMode} follow={follow} ortho={ortho} confiner={conf} shape={shape}");
    }
}
