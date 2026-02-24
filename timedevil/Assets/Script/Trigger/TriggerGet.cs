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

    [Header("Grace Policy (Return from battle)")]
    [Tooltip("true면 PlayerReturnContext.IsInGracePeriod 동안 이 트리거는 발동하지 않습니다.")]
    public bool blockDuringGracePeriod = false;

    [Header("Debug")]
    public bool debugLog = true;

    private int _called = 0;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            if (debugLog) Debug.LogWarning("[TriggerGet] Collider2D.isTrigger가 꺼져있어서 켰습니다.");
        }

        if (!router) router = FindObjectOfType<TriggerRouter>(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!router)
        {
            if (debugLog) Debug.LogWarning("[TriggerGet] router가 연결되지 않았습니다.");
            return;
        }

        if (blockDuringGracePeriod && PlayerReturnContext.IsInGracePeriod)
        {
            if (debugLog)
                Debug.Log($"[TriggerGet] Suppressed by Grace key='{routeKey}' (by='{other.name}')");
            return;
        }

        if (maxCalls > 0 && _called >= maxCalls)
            return;

        PlayerMove pm = null;
        if (usePlayerMoveComponentCheck)
        {
            pm = other.GetComponent<PlayerMove>();
            if (!pm) return;
        }

        var ctx = new TriggerContext(
            trigger: this,
            router: router,
            instigator: other.gameObject,
            instigatorCollider: other,
            playerMove: pm
        );

        bool accepted = router.RequestRoute(routeKey, ctx);
        if (!accepted)
            return;

        _called++;

        if (debugLog)
            Debug.Log($"[TriggerGet] Fired key='{routeKey}' call={_called}/{(maxCalls <= 0 ? "∞" : maxCalls.ToString())} by='{other.name}'");
    }
}
