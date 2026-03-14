/// <summary>
/// JSON에 실제로 저장될 데이터 형식 (InventorySaveData와 거의 동일)
/// </summary>
[System.Serializable]
public class ItemSave
{
    public InventoryItemEntry[] items;
}
