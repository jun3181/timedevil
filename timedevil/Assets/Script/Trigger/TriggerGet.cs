// Assets/Script/Trigger/TriggerGet.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class TriggerGet : MonoBehaviour
{
    // 씬 재로드(배틀 왕복) 시에도 "이미 소모된 TriggerGet" 상태를 유지
    private static readonly System.Collections.Generic.Dictionary<string, int> s_callCountById = new();

    [Header("Router")]
    public TriggerRouter router;

    [Header("Route Key")]
    public string routeKey = "Trigger1";

    [Header("Call Limit")]
    [Tooltip("0이면 무제한, 1이면 1회만, 2면 2회까지만 실행")]
    public int maxCalls = 1;

    [Header("Detect")]
    [Tooltip("플레이어 판정: PlayerMove 컴포넌트로 체크")]
    public bool usePlayerMoveComponentCheck = true;

    //  (A) 전투만 재진입 방지용 옵션
    [Header("Grace Policy (Return from battle)")]
    [Tooltip("true면 PlayerReturnContext.IsInGracePeriod 동안 이 트리거는 발동하지 않습니다. (전투 재진입 방지용)")]
    public bool blockDuringGracePeriod = false;

    [Header("Debug")]
    public bool debugLog = true;

    [Header("Optional Parallel Step")]
    [Tooltip("같은 TriggerGet에서 Route 실행과 동시에 카메라 연출을 병행 시작")]
    public TriggerStep_CameraMove cameraMoveStep;

    private int _called = 0;
    private Collider2D _selfCollider;

    private bool _pendingByCutscene = false;
    private Collider2D _pendingInstigatorCollider = null;
    private PlayerMove _pendingPlayerMove = null;
    private Coroutine _cameraRestoreCo = null;
    private string _runtimeId;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        _runtimeId = BuildRuntimeId();

        _selfCollider = GetComponent<Collider2D>();
        if (!_selfCollider.isTrigger)
        {
            _selfCollider.isTrigger = true;
            if (debugLog) Debug.LogWarning("[TriggerGet] Collider2D.isTrigger가 꺼져있어서 켰습니다.");
        }

        if (!router) router = FindObjectOfType<TriggerRouter>(true);
        if (!cameraMoveStep) cameraMoveStep = GetComponent<TriggerStep_CameraMove>();

        // 이전 씬 인스턴스에서의 호출 횟수 복원
        if (!string.IsNullOrEmpty(_runtimeId) && s_callCountById.TryGetValue(_runtimeId, out int persisted))
            _called = Mathf.Max(0, persisted);

        ApplyConsumedStateIfNeeded();
    }

    private void Update()
    {
        if (!_pendingByCutscene) return;
        if (CutsceneRouter.IsAnyCutsceneRunning) return;

        // 컷씬이 끝났고, 아직 같은 콜라이더가 트리거 내부에 남아 있으면 발동
        if (_selfCollider != null && _pendingInstigatorCollider != null && _selfCollider.IsTouching(_pendingInstigatorCollider))
        {
            if (debugLog)
                Debug.Log($"[TriggerGet] Resume pending key='{routeKey}' after cutscene end by='{_pendingInstigatorCollider.name}'");

            TryInvokeRoute(_pendingInstigatorCollider, _pendingPlayerMove);
        }

        ClearPending();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!router)
        {
            if (debugLog) Debug.LogWarning("[TriggerGet] router가 연결되지 않았습니다.");
            return;
        }

        // 전투 복귀 직후 재진입 방지:
        // - IsInGracePeriod: 복귀 후 grace 코루틴이 실제로 도는 동안
        // - GraceSecondsPending: 복귀 씬 로드 직후 코루틴 시작 전 "틈" 프레임 방어
        if (blockDuringGracePeriod &&
            (PlayerReturnContext.IsInGracePeriod || PlayerReturnContext.GraceSecondsPending > 0f))
        {
            if (debugLog)
                Debug.Log($"[TriggerGet] Suppressed by Grace key='{routeKey}' (by='{other.name}')");
            return;
        }

        if (maxCalls > 0 && _called >= maxCalls)
            return;

        // 플레이어 판정 + PlayerMove 확보
        PlayerMove pm = null;
        if (usePlayerMoveComponentCheck)
        {
            pm = other.GetComponent<PlayerMove>();
            if (!pm) return;
        }

        if (CutsceneRouter.IsAnyCutsceneRunning)
        {
            _pendingByCutscene = true;
            _pendingInstigatorCollider = other;
            _pendingPlayerMove = pm;

            if (debugLog)
                Debug.Log($"[TriggerGet] Deferred by cutscene key='{routeKey}' by='{other.name}'");
            return;
        }

        TryInvokeRoute(other, pm);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_pendingByCutscene && other == _pendingInstigatorCollider)
            ClearPending();
    }

    private void TryInvokeRoute(Collider2D other, PlayerMove pm)
    {
        if (other == null) return;

        if (maxCalls > 0 && _called >= maxCalls)
            return;

        _called++;
        PersistCallCount();

        if (debugLog)
            Debug.Log($"[TriggerGet] Fired key='{routeKey}' call={_called}/{(maxCalls <= 0 ? "∞" : maxCalls.ToString())} by='{other.name}'");

        var ctx = new TriggerContext(
            trigger: this,
            router: router,
            instigator: other.gameObject,
            instigatorCollider: other,
            playerMove: pm
        );

        if (cameraMoveStep != null)
        {
            cameraMoveStep.BeginFromTriggerGet(ctx);
            if (_cameraRestoreCo != null) StopCoroutine(_cameraRestoreCo);
            _cameraRestoreCo = StartCoroutine(CoRestoreCameraAfterRoute(routeKey));
        }

        router.RequestRoute(routeKey, ctx);

        // 1회/유한 호출 트리거는 소진 시 즉시 비활성화하여
        // 배틀씬 왕복 후에도 동일 TriggerGet만 재발동되지 않게 보장
        ApplyConsumedStateIfNeeded();
    }

    private System.Collections.IEnumerator CoRestoreCameraAfterRoute(string key)
    {
        yield return null; // Route 코루틴이 _runningKeys에 등록될 시간 보장
        while (router != null && router.IsRouteRunning(key))
            yield return null;

        if (cameraMoveStep != null)
            cameraMoveStep.RestorePreviousMode();

        _cameraRestoreCo = null;
    }

    private void ClearPending()
    {
        _pendingByCutscene = false;
        _pendingInstigatorCollider = null;
        _pendingPlayerMove = null;
    }

    private void PersistCallCount()
    {
        if (string.IsNullOrEmpty(_runtimeId)) return;
        s_callCountById[_runtimeId] = _called;
    }

    private void ApplyConsumedStateIfNeeded()
    {
        if (maxCalls <= 0) return;
        if (_called < maxCalls) return;

        enabled = false;
        if (_selfCollider != null) _selfCollider.enabled = false;

        if (debugLog)
            Debug.Log($"[TriggerGet] Consumed -> disabled key='{routeKey}' id='{_runtimeId}' calls={_called}/{maxCalls}", this);
    }

    private string BuildRuntimeId()
    {
        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "(no-scene)";
        return $"{sceneName}::{GetTransformPath(transform)}::{routeKey}";
    }

    private static string GetTransformPath(Transform t)
    {
        if (t == null) return "(null)";
        var stack = new System.Collections.Generic.Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack.ToArray());
    }
}
