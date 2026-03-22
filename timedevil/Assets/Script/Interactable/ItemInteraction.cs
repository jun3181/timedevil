using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ItemInteraction: MonoBehaviour, IInteractable
{
    [System.Serializable]
    private struct ItemInfo {
        public ItemSO itemSO;
        public int quantity;
    }

    [SerializeField] ItemDatabaseSO db;

    [SerializeField] private List<ItemInfo> itemInfos = new();
    [SerializeField] private bool debuged = true;
    
    // 설정한 ItemSO가 ItemDatabaseSO에 등록되어 있는지 확인
    void Awake() {
        if(db==null || itemInfos==null || itemInfos.Count==0) {
            if(debuged) Debug.Log("아직 설정되지 않음");
            return;
        }

        bool found;
        for(int i=0; i<itemInfos.Count; i++) {
            if(itemInfos[i].itemSO == null) {
                itemInfos.RemoveAt(i);
                i--;
                continue;
            }

            found = false;
            for(int j = 0; j < db.items.Count; j++) {
                if(itemInfos[i].itemSO.id == db.items[j].id) {
                    found = true;
                    break;
                }
            }

            if(!found) {
                itemInfos.RemoveAt(i);
                i--;
            }
        }
    }


    public void Interact() {
        if(ItemRuntime.Instance == null) return;

        if(debuged) PrintInventory();

        for(int i = 0; i < itemInfos.Count; i++) {
            ItemRuntime.Instance.AddQuantity(itemInfos[i].itemSO.id, itemInfos[i].quantity);
        }

        if(debuged) PrintInventory();
    }

    private void PrintInventory() {
        string msg = "";
        InventoryItemEntry[] inven = ItemRuntime.Instance.CurrentData.items;
        for(int i = 0; i<inven.Length; i++) {
            msg += $"{inven[i].id}: {inven[i].quantity}\n";
        }

        Debug.Log(msg);
    }
}
