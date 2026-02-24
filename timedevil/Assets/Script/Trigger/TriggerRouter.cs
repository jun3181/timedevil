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
    }

    [Header("Routes (Key -> Steps)")]
    public List<Route> routes = new();

    [Header("Policy")]
    [Tooltip("false면 같은 key가 실행 중일 때 중복 요청을 무시")]
    public bool allowReentrySameKey = false;

    [Header("Debug")]
    public bool debugLog = true;

    private readonly Dictionary<string, Route> _map = new();
    private readonly HashSet<string> _runningKeys = new();

    private void Awake()
    {
        BuildMap();
    }

    private void OnValidate()
    {
        BuildMap();
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
                if (debugLog) Debug.LogWarning($"[TriggerRouter] routes[{i}] key가 비었습니다.");
                continue;
            }

            if (_map.ContainsKey(r.key))
            {
                Debug.LogError($"[TriggerRouter] Route key 중복: '{r.key}'");
                continue;
            }

            _map.Add(r.key, r);
        }
    }

    public bool RequestRoute(string key, TriggerContext ctx)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            if (debugLog) Debug.LogWarning("[TriggerRouter] RequestRoute: key가 비었습니다.");
            return false;
        }

        if (!_map.TryGetValue(key, out var route) || route == null)
        {
            if (debugLog) Debug.LogWarning($"[TriggerRouter] Route not found: '{key}'");
            return false;
        }

        if (!allowReentrySameKey && _runningKeys.Contains(key))
        {
            if (debugLog) Debug.Log($"[TriggerRouter] Ignore (running) key='{key}'");
            return false;
        }

        StartCoroutine(CoRunRoute(key, route, ctx));
        return true;
    }

    private IEnumerator CoRunRoute(string key, Route route, TriggerContext ctx)
    {
        _runningKeys.Add(key);
        if (debugLog)
            Debug.Log($"[TriggerRouter] START key='{key}' steps={(route.steps != null ? route.steps.Count : 0)} trigger='{(ctx?.trigger ? ctx.trigger.name : "null")}'");

        try
        {
            if (route.steps != null)
            {
                for (int i = 0; i < route.steps.Count; i++)
                {
                    var step = route.steps[i];
                    if (!step) continue;

                    if (debugLog) Debug.Log($"[TriggerRouter]  step[{i}] -> {step.GetType().Name} ({step.name})");
                    yield return CoRunStepSafe(step, ctx, i, key);
                }
            }
        }
        finally
        {
            _runningKeys.Remove(key);
            if (debugLog) Debug.Log($"[TriggerRouter] END key='{key}'");
        }
    }

    private IEnumerator CoRunStepSafe(TriggerStepBase step, TriggerContext ctx, int index, string key)
    {
        IEnumerator it = null;
        try
        {
            it = step.Execute(ctx);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TriggerRouter] key='{key}' step[{index}] Execute() throw: {e}");
            yield break;
        }

        if (it == null) yield break;

        while (true)
        {
            object current;
            try
            {
                if (!it.MoveNext()) break;
                current = it.Current;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TriggerRouter] key='{key}' step[{index}] coroutine throw: {e}");
                yield break;
            }

            yield return current;
        }
    }
}
