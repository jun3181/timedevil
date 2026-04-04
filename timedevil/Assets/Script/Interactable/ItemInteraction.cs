using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
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

    [Header("아이템 획득 전 대사")]
    [Tooltip("기본적인 획득 대사는 따로 설정할 필요가 없으며 Legacy 속성은 건들지 말 것")]
    [SerializeField] private DialogueLine[] beforeDialogue;

    [Header("아이템 획득 후 대사")]
    [Tooltip("기본적인 획득 대사는 따로 설정할 필요가 없으며 Legacy 속성은 건들지 말 것")]
    [SerializeField] private Dialogue dialogue;

    [Header("디버그 메시지 출력 여부")]
    [SerializeField] private bool debuged = true;

    private bool _inited;
    private SpriteRenderer _spriteRenderer;
    
    // 설정한 ItemSO가 ItemDatabaseSO에 등록되어 있는지 확인
    void Awake() {
        _inited = false;
        _spriteRenderer = GetComponent<SpriteRenderer>();

        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
        collider.enabled = true;

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
                if(debuged) Debug.LogWarning($"{gameObject.name}의 itemSO의 값을 None(null)로 설정할 수 없습니다.");
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
                if(debuged) Debug.LogWarning($"{itemInfos[i].itemSO.id}는 {db.name}내에 존재하지 않습니다.");
                itemInfos.RemoveAt(i);
                i--;
            }
        }

        if(itemInfos.Count==0) {
            if(debuged) Debug.LogWarning($"{gameObject.name}과 상호작용시 지급될 아이템이 존재하지 않습니다.");
            return;
        }

        Queue<DialogueLine> newDialogueLines = new();
        foreach(DialogueLine dl in beforeDialogue) {
            newDialogueLines.Enqueue(dl);
        }

        foreach(ItemInfo itemInfo in itemInfos) {
            DialogueLine dl = new();
            dl.text = $"루시는 '{itemInfo.itemSO.displayName}'를 {itemInfo.quantity}개 획득하였다!";
            newDialogueLines.Enqueue(dl);
        }

        foreach(DialogueLine dl in dialogue.lines) {
            newDialogueLines.Enqueue(dl);
        }

        dialogue.lines = newDialogueLines.ToArray();

        _inited = true;
    }

    public void Interact() {
        if(!_inited) {
            if(debuged) Debug.LogWarning($"{gameObject.name}의 ItemInteraction이 정상적으로 초기화되지 않은 상태입니다.");
            return;
        }

        if(ItemRuntime.Instance == null) {
            if(debuged) Debug.LogWarning($"ItemRuntime의 인스턴스가 Scene내 존재하지 않습니다.");
            return;
        }

        if(debuged) PrintInventory();

        for(int i = 0; i < itemInfos.Count; i++) {
            ItemRuntime.Instance.AddQuantity(itemInfos[i].itemSO.id, itemInfos[i].quantity);
        }

        if(debuged) PrintInventory();

        _spriteRenderer.enabled = false;

        if(DialogueManager.instance != null) DialogueManager.instance.StartDialogue(dialogue);

        Destroy(gameObject);
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
