using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TriggerGet : MonoBehaviour
{
    [Header("Router")]
    [SerializeField] private TriggerRouter router;

    [Header("Call Limit (0 = infinite)")]
    [SerializeField] private int maxCalls = 1;     // 0이면 무한
    [SerializeField] private bool disableAfterMaxCalls = true;

    [Header("Filter")]
    [Tooltip("PlayerMove를 가진 오브젝트만 트리거로 인정")]
    [SerializeField] private bool requirePlayerMove = true;

    [Header("Anti-Spam")]
    [SerializeField] private bool preventReenterWhileRunning = true;
    [SerializeField] private float cooldownSeconds = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private int usedCalls = 0;
    private float lastFireTime = -999f;

    private void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        router ??= GetComponent<TriggerRouter>();
        if (!router) router = FindObjectOfType<TriggerRouter>(true);
    }

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            if (debugLog) Debug.Log($"[TriggerGet] '{name}' BoxCollider2D.isTrigger=true로 강제 설정");
        }

        if (!router) router = GetComponent<TriggerRouter>();
        if (!router)
            Debug.LogError($"[TriggerGet] '{name}' router가 비었습니다. TriggerRouter를 연결하세요.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!router) return;

        // 1) 필터: 플레이어만
        if (requirePlayerMove && other.GetComponent<PlayerMove>() == null)
            return;

        // 2) 쿨다운
        if (Time.time - lastFireTime < cooldownSeconds)
        {
            if (debugLog) Debug.Log($"[TriggerGet] cooldown ignore ({cooldownSeconds}s) other={other.name}");
            return;
        }

        // 3) 호출 횟수 제한
        if (maxCalls > 0 && usedCalls >= maxCalls)
        {
            if (debugLog) Debug.Log($"[TriggerGet] maxCalls reached ({usedCalls}/{maxCalls}) ignore");
            if (disableAfterMaxCalls) DisableSelf();
            return;
        }

        // 4) 라우터 실행 중 재진입 방지
        if (preventReenterWhileRunning && router.IsRunning)
        {
            if (debugLog) Debug.Log($"[TriggerGet] router running -> ignore (other={other.name})");
            return;
        }

        // 5) 호출
        lastFireTime = Time.time;
        usedCalls++;

        if (debugLog)
        {
            Debug.Log(
                $"[TriggerGet] FIRE '{name}' by '{other.name}' usedCalls={usedCalls}/{(maxCalls == 0 ? "inf" : maxCalls.ToString())}"
            );
        }

        router.StartSequence(other.gameObject);

        // 6) 다 썼으면 비활성화
        if (maxCalls > 0 && usedCalls >= maxCalls && disableAfterMaxCalls)
            DisableSelf();
    }

    private void DisableSelf()
    {
        if (debugLog) Debug.Log($"[TriggerGet] DisableSelf '{name}'");

        // 콜라이더만 꺼도 되고 오브젝트 자체를 꺼도 됨. 일단 안전하게 콜라이더 먼저.
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        // 필요하면 오브젝트 자체도 끄기
        // gameObject.SetActive(false);
    }
}
