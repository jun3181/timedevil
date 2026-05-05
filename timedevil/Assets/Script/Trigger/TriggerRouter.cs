// Assets/Script/Trigger/TriggerRouter.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerRouter : MonoBehaviour
{
    [System.Serializable]
    public class Route
    {
        public string key = "Trigger1";
        public List<TriggerStepBase> steps = new();

        [Tooltip("이 Route 실행 중에는 플레이어 입력(GameManager Action Lock)을 막을지 여부")]
        public bool blockPlayerInputWhileRunning = false;
    }

    [Header("Routes (Key -> Steps)")]
    public List<Route> routes = new();

    [Header("Policy")]
    [Tooltip("false  key    û ")]
    public bool allowReentrySameKey = false;

    [Header("Debug")]
    public bool debugLog = true;

    private readonly Dictionary<string, Route> _map = new();
    private readonly HashSet<string> _runningKeys = new();
    private int _heldRouteInputLockCount = 0;

    private void Awake()
    {
        BuildMap();
    }

    private void Start()
    {
        TryResumeInProgressRoutes();
    }

    private void OnValidate()
    {
        // Ϳ Ű ߺ üũ 
        BuildMap();
    }

    private void OnDisable()
    {
        ReleaseAllRouteInputLocks("OnDisable");
    }

    private void OnDestroy()
    {
        ReleaseAllRouteInputLocks("OnDestroy");
    }

    private void BuildMap()
    {
        _map.Clear();

        if (routes == null) return;

        for (int i = 0; i < routes.Count; i++)
        {
            var r = routes[i];
            if (r == null) continue;

            if (string.IsNullOrWhiteSpace(r.key))
            {
                if (debugLog) Debug.LogWarning($"[TriggerRouter] routes[{i}] key ");
                continue;
            }

            if (_map.ContainsKey(r.key))
            {
                Debug.LogError($"[TriggerRouter] Route key ߺ: '{r.key}' (ϳ ܾ )");
                continue;
            }

            _map.Add(r.key, r);
        }
    }

    public void RequestRoute(string key, TriggerContext ctx)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            if (debugLog) Debug.LogWarning("[TriggerRouter] RequestRoute: key ");
            return;
        }

        if (!_map.TryGetValue(key, out var route) || route == null)
        {
            if (debugLog) Debug.LogWarning($"[TriggerRouter] Route not found: '{key}'");
            return;
        }

        if (!allowReentrySameKey && _runningKeys.Contains(key))
        {
            if (debugLog) Debug.Log($"[TriggerRouter] Ignore (running) key='{key}'");
            return;
        }

        StartCoroutine(CoRunRoute(key, route, ctx, 0, false));
    }

    private IEnumerator CoRunRoute(string key, Route route, TriggerContext ctx, int startIndex, bool isResume)
    {
        _runningKeys.Add(key);
        bool heldInputLock = false;
        string runtimeId = BuildRouteRuntimeId(key);
        bool completedAllSteps = false;

        if (debugLog)
            Debug.Log($"[TriggerRouter] START key='{key}' resume={isResume} fromStep={startIndex} steps={(route.steps != null ? route.steps.Count : 0)} trigger='{(ctx?.trigger ? ctx.trigger.name : "null")}'");

        if (route.blockPlayerInputWhileRunning && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            heldInputLock = true;
            _heldRouteInputLockCount++;
            if (debugLog) Debug.Log($"[TriggerRouter] INPUT LOCK key='{key}'");
        }

        try
        {
            if (route.steps != null)
            {
                for (int i = Mathf.Max(0, startIndex); i < route.steps.Count; i++)
                {
                    var step = route.steps[i];
                    if (!step) continue;
                    // step 시작 전: 중간에 씬 전환되면 현재 step부터 재개
                    WorldNPCStateService.Instance?.SaveTriggerRouteProgress(runtimeId, key, i, true);

                    if (debugLog) Debug.Log($"[TriggerRouter]  step[{i}] -> {step.GetType().Name} ({step.name})");

                    IEnumerator it = null;
                    try
                    {
                        it = step.Execute(ctx);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[TriggerRouter] step[{i}] Execute() throw: {e}");
                    }

                    if (it != null)
                        yield return it;

                    // step 정상 종료 후: 다음 step 인덱스로 전진 저장
                    WorldNPCStateService.Instance?.SaveTriggerRouteProgress(runtimeId, key, i + 1, true);
                }
            }

            completedAllSteps = true;
        }
        finally
        {
            if (heldInputLock && GameManager.Instance != null)
            {
                GameManager.Instance.UnlockAction();
                _heldRouteInputLockCount = Mathf.Max(0, _heldRouteInputLockCount - 1);
                if (debugLog) Debug.Log($"[TriggerRouter] INPUT UNLOCK key='{key}'");
            }

            if (debugLog) Debug.Log($"[TriggerRouter] END key='{key}'");
            _runningKeys.Remove(key);

            // 정상 완주 시에만 progress 제거.
            // 씬 전환/중단으로 코루틴이 끝난 경우(progress가 있어야 복귀 후 resume 가능)
            if (completedAllSteps)
            {
                WorldNPCStateService.Instance?.ClearTriggerRouteProgress(runtimeId);
            }
        }
    }

    public bool IsRouteRunning(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return _runningKeys.Contains(key);
    }

    private void ReleaseAllRouteInputLocks(string reason)
    {
        if (_heldRouteInputLockCount <= 0) return;
        if (GameManager.Instance == null) return;

        int releaseCount = _heldRouteInputLockCount;
        for (int i = 0; i < releaseCount; i++)
            GameManager.Instance.UnlockAction();

        _heldRouteInputLockCount = 0;

        if (debugLog)
            Debug.Log($"[TriggerRouter] Force INPUT UNLOCK x{releaseCount} ({reason})");
    }

    private void TryResumeInProgressRoutes()
    {
        if (WorldNPCStateService.Instance == null || routes == null) return;

        for (int i = 0; i < routes.Count; i++)
        {
            var r = routes[i];
            if (r == null || string.IsNullOrWhiteSpace(r.key)) continue;

            string runtimeId = BuildRouteRuntimeId(r.key);
            if (!WorldNPCStateService.Instance.TryGetTriggerRouteProgress(runtimeId, out var p)) continue;
            if (!p.isRunning) continue;
            if (_runningKeys.Contains(r.key)) continue;

            if (debugLog)
                Debug.Log($"[TriggerRouter] RESUME key='{r.key}' fromStep={p.nextStepIndex}");

            StartCoroutine(CoRunRoute(r.key, r, BuildResumeContext(), p.nextStepIndex, true));
        }
    }

    private TriggerContext BuildResumeContext()
    {
        var player = FindObjectOfType<PlayerMove>(true);
        var playerGo = player ? player.gameObject : null;
        var playerCol = player ? player.GetComponent<Collider2D>() : null;
        return new TriggerContext(
            trigger: null,
            router: this,
            instigator: playerGo,
            instigatorCollider: playerCol,
            playerMove: player
        );
    }

    private string BuildRouteRuntimeId(string key)
    {
        return $"{gameObject.scene.name}::{GetTransformPath(transform)}::{key}";
    }

    private static string GetTransformPath(Transform t)
    {
        if (t == null) return "(null)";
        var stack = new Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack.ToArray());
    }
}
