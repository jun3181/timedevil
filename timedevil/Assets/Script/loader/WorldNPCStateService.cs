// Assets/Script/loader/WorldNPCStateService.cs
using System.Collections.Generic;
using UnityEngine;

public class WorldNPCStateService : MonoBehaviour
{
    public static WorldNPCStateService Instance { get; private set; }

    // 최근 전투 진입 시 부딪힌 "그" 적의 스냅샷만 쓰면 되므로, 가장 단순하게 보관
    private readonly Dictionary<string, EnemySnapshot> _lastSnapshots = new();

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
}
