// Assets/Script/loader/WorldNPCStateService.cs
using System.Collections.Generic;
using UnityEngine;

public class WorldNPCStateService : MonoBehaviour
{
    public static WorldNPCStateService Instance { get; private set; }

    // ÃÖ±Ù ÀüÅõ ÁøÀÔ ½Ã ºÎµúÈù "±×" ÀûÀÇ ½º³À¼¦¸¸ ¾²¸é µÇ¹Ç·Î, °¡Àå ´Ü¼øÇÏ°Ô º¸°ü
    private readonly Dictionary<string, EnemySnapshot> _lastSnapshots = new();

    private readonly Dictionary<string, TriggerRouteProgress> _triggerRouteProgress = new();

    public struct TriggerRouteProgress
    {
        public string routeRuntimeId;
        public string routeKey;
        public int nextStepIndex;
        public bool isRunning;
    }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveSnapshot(GameObject enemyGo)
    {
        if (!enemyGo) return;
        var id = enemyGo.GetComponent<EnemyInstanceId>()?.Id ?? enemyGo.name;
        var snap = EnemySnapshot.Capture(enemyGo);
        _lastSnapshots[id] = snap;
#if UNITY_EDITOR
        Debug.Log($"[WorldNPCState] saved snapshot id='{id}' pos={snap.position}");
#endif
    }

    public bool TryGetSnapshot(string instanceId, out EnemySnapshot snap)
    {
        return _lastSnapshots.TryGetValue(instanceId, out snap);
    }

    public void ClearSnapshot(string instanceId)
    {
        _lastSnapshots.Remove(instanceId);
    }

    public void SaveTriggerRouteProgress(string routeRuntimeId, string routeKey, int nextStepIndex, bool isRunning)
    {
        if (string.IsNullOrWhiteSpace(routeRuntimeId)) return;

        _triggerRouteProgress[routeRuntimeId] = new TriggerRouteProgress
        {
            routeRuntimeId = routeRuntimeId,
            routeKey = routeKey,
            nextStepIndex = Mathf.Max(0, nextStepIndex),
            isRunning = isRunning
        };
    }

    public bool TryGetTriggerRouteProgress(string routeRuntimeId, out TriggerRouteProgress progress)
    {
        if (string.IsNullOrWhiteSpace(routeRuntimeId))
        {
            progress = default;
            return false;
        }

        return _triggerRouteProgress.TryGetValue(routeRuntimeId, out progress);
    }

    public void ClearTriggerRouteProgress(string routeRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(routeRuntimeId)) return;
        _triggerRouteProgress.Remove(routeRuntimeId);
    }

}
