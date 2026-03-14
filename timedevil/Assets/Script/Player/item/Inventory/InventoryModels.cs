using UnityEngine;

[System.Serializable]
public class InventoryItemEntry
{
    public string id;      // ItemSO.id (예: "potion")
    public int quantity;   // 현재 플레이어가 가진 개수
}

[System.Serializable]
public class InventorySaveData
{
    public InventoryItemEntry[] items;
}
