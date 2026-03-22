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

    [SerializeField] private ItemDatabaseSO db;

    [SerializeField] private List<ItemInfo> itemInfos = new();
    [SerializeField] private bool debuged = true;
    
    // 설정한 ItemSO가 ItemDatabaseSO에 등록되어 있는지 확인
    void Awake() {
        if(db==null) {
            if(debuged) Debug.LogWarning("ItemDatabaseSO가 설정되지 않았습니다.");
            return;
        } else if(itemInfos==null) {
            if(debuged) Debug.LogWarning("ItemInfos가 설정되지 않았습니다.");
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

        if(itemInfos.Count==0 && debuged) {
            Debug.LogWarning($"{gameObject.name}과 상호작용시 지급될 아이템이 존재하지 않습니다.");
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
