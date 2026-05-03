// Assets/Script/Trigger/TriggerGet.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class TriggerGet : MonoBehaviour
{
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

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        _selfCollider = GetComponent<Collider2D>();
        if (!_selfCollider.isTrigger)
        {
            _selfCollider.isTrigger = true;
            if (debugLog) Debug.LogWarning("[TriggerGet] Collider2D.isTrigger가 꺼져있어서 켰습니다.");
        }

        if (!router) router = FindObjectOfType<TriggerRouter>(true);
        if (!cameraMoveStep) cameraMoveStep = GetComponent<TriggerStep_CameraMove>();
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

        // 전투 트리거만 Grace 동안 막기
        if (blockDuringGracePeriod && PlayerReturnContext.IsInGracePeriod)
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
            cameraMoveStep.BeginFromTriggerGet(ctx);

        router.RequestRoute(routeKey, ctx);
    }

    private void ClearPending()
    {
        _pendingByCutscene = false;
        _pendingInstigatorCollider = null;
        _pendingPlayerMove = null;
    }
}
