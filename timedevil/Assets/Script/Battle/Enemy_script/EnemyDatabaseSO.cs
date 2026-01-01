// EnemyDatabaseSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Enemy Database", fileName = "EnemyDatabase")]
public class EnemyDatabaseSO : ScriptableObject
{
    public List<EnemySO> enemies = new();

    private Dictionary<string, EnemySO> _map;

    void OnEnable()
    {
        _map = new Dictionary<string, EnemySO>();
        foreach (var so in enemies)
        {
            if (!so) continue;
            if (!string.IsNullOrEmpty(so.enemyId) && !_map.ContainsKey(so.enemyId))
                _map.Add(so.enemyId, so);
        }
    }

    public EnemySO GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || _map == null) return null;
        _map.TryGetValue(id, out var so);
        return so;
    }
}
