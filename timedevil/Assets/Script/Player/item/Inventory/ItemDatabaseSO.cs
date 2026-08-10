using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Database", fileName = "ItemDatabase")]
public class ItemDatabaseSO : ScriptableObject
{
    public List<ItemSO> items = new();

    private Dictionary<string, ItemSO> map;

    void OnEnable()
    {
        RebuildMap();
    }

    private void RebuildMap()
    {
        map = new Dictionary<string, ItemSO>();

        foreach (var it in items)
        {
            if (!it) continue;
            if (!string.IsNullOrEmpty(it.id) && !map.ContainsKey(it.id))
            {
                map.Add(it.id, it);
            }
        }
    }

    public ItemSO GetById(string id)
    {
        if (map == null) RebuildMap();
        if (string.IsNullOrEmpty(id)) return null;

        map.TryGetValue(id, out var so);
        return so;
    }
}
