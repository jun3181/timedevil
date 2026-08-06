using UnityEngine;

public class InventoryItemApplyer : MonoBehaviour
{
    [Header("Page Manager")]
    [SerializeField] private InventoryPageManagerKeys pageManager;

    [Header("Inventory Cursor")]
    [SerializeField] private InventoryCursor inventoryCursor;

    [Header("Item Database")]
    [SerializeField] private ItemDatabaseSO db;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        if (ItemRuntime.Instance == null || pageManager == null || inventoryCursor == null)
            return;

        InventoryDisplay page = pageManager.GetCurrentPage();
        if (page == null)
            return;

        InventoryItemEntry entry = page.GetCursoredItemEntry();
        if (entry == null)
            return;

        ItemDatabaseSO database = db != null ? db : page.itemDatabase;
        if (database == null)
        {
            Debug.LogWarning("ItemDatabaseSO is not assigned.");
            return;
        }

        ItemSO item = database.GetById(entry.id);
        if (item == null)
        {
            Debug.LogWarning($"ItemSO not found for id '{entry.id}'.");
            return;
        }

        if (!item.TryUse(out string message))
        {
            if (!string.IsNullOrEmpty(message))
                Debug.LogWarning(message);
            return;
        }

        if (ItemRuntime.Instance == null)
            return;

        if (item.consumeOnUse)
            ItemRuntime.Instance.AddQuantity(entry.id, -1);

        RefreshStateDisplays();
        page.DisplayCurrentPage();
    }

    private void RefreshStateDisplays()
    {
        foreach (StatusPanel statusPanel in FindObjectsOfType<StatusPanel>(true))
            statusPanel.Refresh();

        foreach (HPUIBinder hpUIBinder in FindObjectsOfType<HPUIBinder>(true))
            hpUIBinder.Refresh();
    }
}
