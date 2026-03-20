using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ItemInteraction: MonoBehaviour, IInteractable
{
    [System.Serializable]
    private struct ItemInfo {
        public string id;
        public int quantity;
    }

    [SerializeField] private List<ItemInfo> itemInfos = new();
    [SerializeField] private bool debuged = true;


    public void Interact() {
        Debug.Log("Actived");
        if(ItemRuntime.Instance == null) return;

        if(debuged) PrintInventory();

        for(int i = 0; i < itemInfos.Count; i++) {
            ItemRuntime.Instance.AddQuantity(itemInfos[i].id, itemInfos[i].quantity);
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
