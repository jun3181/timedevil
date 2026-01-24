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

        if (maxCalls > 0 && _called >= maxCalls)
            return;

        // 플레이어 판정 + PlayerMove 확보
        PlayerMove pm = null;
        if (usePlayerMoveComponentCheck)
        {
            pm = other.GetComponent<PlayerMove>();
            if (!pm) return;
        }

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

        router.RequestRoute(routeKey, ctx);
    }
}
