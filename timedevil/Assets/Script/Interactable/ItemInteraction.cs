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

    [Header("아이템 DB")]
    [Tooltip("아이템이 게임내 존재하는지 검사하는 용도")]
    [SerializeField] private ItemDatabaseSO db;

    [Header("아이템 정보")]
    [Tooltip("획득될 아이템")]
    [SerializeField] private List<ItemInfo> itemInfos = new();

    [Header("아이템 획득 대사 후 추가적인 대사")]
    [Tooltip("기본적인 획득 대사는 따로 설정할 필요가 없으며 Legacy 속성은 건들지 말 것")]
    [SerializeField] private Dialogue dialogue;

    [Header("디버그 메시지 출력 여부")]
    [SerializeField] private bool debuged = true;

    private bool _inited;
    
    // 설정한 ItemSO가 ItemDatabaseSO에 등록되어 있는지 확인
    void Awake() {
        _inited = false;
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
            return;
        }

        int newLength = itemInfos.Count;
        newLength += dialogue.lines?.Length ?? 0;

        DialogueLine[] defaultDialogueLine = new DialogueLine[newLength];
        for(int i=0; i<itemInfos.Count; i++) {
            defaultDialogueLine[i] = new DialogueLine();
            defaultDialogueLine[i].text = $"루시는 '{itemInfos[i].itemSO.displayName}'을 획득하였다!";
        }

        for(int i=itemInfos.Count; i<newLength; i++) {
            defaultDialogueLine[i] = dialogue.lines[i - itemInfos.Count];
        }

        dialogue.lines = defaultDialogueLine;

        _inited = true;
    }


    public void Interact() {
        if(!_inited) {
            if(debuged) Debug.LogWarning($"{gameObject.name}의 ItemInteraction이 정상적으로 초기화되지 않은 상태입니다.");
            return;
        }

        if(ItemRuntime.Instance == null) return;

        if(debuged) PrintInventory();

        for(int i = 0; i < itemInfos.Count; i++) {
            ItemRuntime.Instance.AddQuantity(itemInfos[i].itemSO.id, itemInfos[i].quantity);
        }

        if(debuged) PrintInventory();

        if(DialogueManager.instance != null) DialogueManager.instance.StartDialogue(dialogue);
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
