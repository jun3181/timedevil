// CardDatabaseSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Card Database", fileName = "CardDatabase")]
public class CardDatabaseSO : ScriptableObject
{
    public List<BaseCardSO> cards = new();

    private Dictionary<string, BaseCardSO> map;

    void OnEnable()
    {
        map = new Dictionary<string, BaseCardSO>();
        foreach (var c in cards)
        {
            if (!c) continue;
            if (!string.IsNullOrEmpty(c.id) && !map.ContainsKey(c.id))
                map.Add(c.id, c);
        }
    }

    public BaseCardSO GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || map == null) return null;
        map.TryGetValue(id, out var so);
        return so;
    }
}
