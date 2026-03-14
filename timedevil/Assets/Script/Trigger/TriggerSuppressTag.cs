using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerSuppressTag : MonoBehaviour
{
    [Header("Key (비워두면 자동: TriggerGet.RouteKey)")]
    [SerializeField] private string key;

    [Header("What to disable (비워두면 자동: Collider2D + TriggerGet)")]
    [SerializeField] private List<Behaviour> behavioursToDisable = new();

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Coroutine co;
    private readonly List<Behaviour> _cached = new();

    // ----------------------------
    //  레거시: key로 억제
    // ----------------------------
    public static void SuppressByKey(string suppressKey, float seconds)
    {
        if (string.IsNullOrWhiteSpace(suppressKey) || seconds <= 0f) return;

        var tags = Object.FindObjectsOfType<TriggerSuppressTag>(true);
        foreach (var tag in tags)
        {
            if (!tag) continue;
            tag.ApplyIfMatch(suppressKey, seconds);
        }
    }

    public void ApplyIfMatch(string suppressKey, float seconds)
    {
        if (string.IsNullOrWhiteSpace(suppressKey)) return;

        string myKey = ResolveMyKey();
        if (string.IsNullOrWhiteSpace(myKey)) return;

        if (!string.Equals(myKey, suppressKey))
            return;

        if (debugLog)
            Debug.Log($"[TriggerSuppressTag] match key='{myKey}' -> suppress {seconds:0.00}s ({name})", this);

        Suppress(seconds);
    }

    // ----------------------------
    //  B방식: key 없이 "그냥" 억제
    // ----------------------------
    public void Suppress(float seconds)
    {
        if (seconds <= 0f) seconds = 0.05f;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoSuppress(seconds));
    }

    /// <summary>
    /// B방식: 복귀 위치 근처 트리거를 자동 억제
    /// - 반경 내 Collider2D가 있는 오브젝트에서 TriggerSuppressTag를 찾아 Suppress 실행
    /// </summary>
    public static void SuppressNearPoint(Vector2 pos, float radius, float seconds, LayerMask mask)
    {
        if (seconds <= 0f || radius <= 0f) return;

        var hits = Physics2D.OverlapCircleAll(pos, radius, mask);
        if (hits == null || hits.Length == 0) return;

        // 중복 suppress 방지용
        var visited = new HashSet<TriggerSuppressTag>();

        foreach (var col in hits)
        {
            if (!col) continue;

            // 1) 같은 오브젝트에 tag
            var tag = col.GetComponent<TriggerSuppressTag>();
            if (!tag) tag = col.GetComponentInParent<TriggerSuppressTag>();

            if (tag)
            {
                if (visited.Add(tag))
                    tag.Suppress(seconds);
                continue;
            }

            // 2) tag가 없다면 최소한 collider/TriggerGet 꺼주기 (자동)
            DisableColliderAndTriggerGet(col.gameObject, seconds);
        }
    }

    private static void DisableColliderAndTriggerGet(GameObject go, float seconds)
    {
        if (!go) return;

        // 임시 억제용 런너
        var runner = go.GetComponent<TempSuppressRunner>();
        if (!runner) runner = go.AddComponent<TempSuppressRunner>();

        runner.Run(seconds);
    }

    // ----------------------------
    // 내부
    // ----------------------------
    private string ResolveMyKey()
    {
        if (!string.IsNullOrWhiteSpace(key))
            return key;

        // 같은 오브젝트의 TriggerGet에서 자동 추출
        MonoBehaviour triggerGet = null;
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb && mb.GetType().Name == "TriggerGet")
            {
                triggerGet = mb;
                break;
            }
        }
        if (!triggerGet) return null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        string[] names = { "routeKey", "RouteKey", "key", "Key" };

        foreach (var n in names)
        {
            var fi = triggerGet.GetType().GetField(n, flags);
            if (fi != null && fi.FieldType == typeof(string))
            {
                var v = fi.GetValue(triggerGet) as string;
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }

            var pi = triggerGet.GetType().GetProperty(n, flags);
            if (pi != null && pi.PropertyType == typeof(string))
            {
                var v = pi.GetValue(triggerGet) as string;
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
        }
        return null;
    }

    private IEnumerator CoSuppress(float seconds)
    {
        _cached.Clear();

        // 1) 인스펙터 지정 우선
        if (behavioursToDisable != null && behavioursToDisable.Count > 0)
        {
            foreach (var b in behavioursToDisable)
                if (b) _cached.Add(b);
        }
        else
        {
            // 2) 자동: Collider2D + TriggerGet
            foreach (var c in GetComponents<Collider2D>())
                if (c) _cached.Add(c);

            foreach (var mb in GetComponents<MonoBehaviour>())
                if (mb && mb.GetType().Name == "TriggerGet")
                    _cached.Add(mb);
        }

        foreach (var b in _cached)
            if (b) b.enabled = false;

        float t = 0f;
        while (t < seconds)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        foreach (var b in _cached)
            if (b) b.enabled = true;

        _cached.Clear();
        co = null;
    }

    // ----------------------------
    // 임시 억제 런너(자동 fallback)
    // ----------------------------
    private class TempSuppressRunner : MonoBehaviour
    {
        private Coroutine co;

        public void Run(float seconds)
        {
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(Co(seconds));
        }

        private IEnumerator Co(float seconds)
        {
            var cols = GetComponents<Collider2D>();
            foreach (var c in cols) if (c) c.enabled = false;

            // TriggerGet도 끔
            foreach (var mb in GetComponents<MonoBehaviour>())
                if (mb && mb.GetType().Name == "TriggerGet")
                    mb.enabled = false;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            foreach (var c in cols) if (c) c.enabled = true;
            foreach (var mb in GetComponents<MonoBehaviour>())
                if (mb && mb.GetType().Name == "TriggerGet")
                    mb.enabled = true;

            co = null;
            Destroy(this); // 임시 컴포넌트 정리
        }
    }
}
