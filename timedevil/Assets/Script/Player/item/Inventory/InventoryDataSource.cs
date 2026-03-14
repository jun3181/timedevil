using UnityEngine;

/// <summary>
/// InventoryDisplay가 사용할 인벤토리 데이터를 제공하는 단순 래퍼.
/// JSON은 직접 읽지 않고, ItemRuntime.Instance.CurrentData만 참조한다.
/// </summary>
public class InventoryDataSource : MonoBehaviour
{
    /// <summary>
    /// 현재 인벤토리 저장 데이터 (ReadOnly 느낌으로 UI에게 제공)
    /// </summary>
    public InventorySaveData InventoryData
        => ItemRuntime.Instance != null ? ItemRuntime.Instance.CurrentData : null;
}
