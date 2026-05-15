using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryItemApplyer : MonoBehaviour
{
    [Header("페이지 메니저")]
    [SerializeField]
    private InventoryPageManagerKeys pageManager;

    [Header("인벤토리 커서")]
    [SerializeField]
    private InventoryCursor inventoryCursor;

    [Header("ItemDatabaseSO")]
    [SerializeField]
    private ItemDatabaseSO db;

    void Update() {
        if(Input.GetKeyDown(KeyCode.E)) {
            if(ItemRuntime.Instance == null || pageManager==null || inventoryCursor==null) return;

            InventoryDisplay page = pageManager.GetCurrentPage();
            InventoryItemEntry entry = page.GetCursoredItemEntry();
            if(entry == null) return;

            ItemSO so = db.GetById(entry.id);
            if(so == null) return;

            if(so.itemScript != null) {
                if(!so.itemScript.CanItemUsed()) return;
                so.itemScript.Run();
            }

            if(ItemRuntime.Instance == null) return;
            ItemRuntime.Instance.AddQuantity(entry.id, -1);
            page.DisplayCurrentPage();
        }
    }
}
