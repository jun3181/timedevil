using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TriggerStep_ItemPickup : TriggerStepBase
{
    [System.Serializable]
    private struct ItemInfo
    {
        public ItemSO itemSO;
        public int quantity;
    }

    [Header("Item DB")]
    [Tooltip("Used to verify that configured items exist")]
    [SerializeField] private ItemDatabaseSO db;

    [Header("Items")]
    [Tooltip("Items granted by this pickup")]
    [SerializeField] private List<ItemInfo> itemInfos = new();

    [Header("Dialogue Before Pickup")]
    [SerializeField] private DialogueLine[] beforeDialogue;

    [Header("Dialogue After Pickup")]
    [SerializeField] private Dialogue dialogue;

    [Header("Debug")]
    [SerializeField] private bool debuged = true;

    [Header("Pickup Flow")]
    [SerializeField] private bool hideSpriteBeforeDialogue = true;
    [SerializeField] private bool disableCollidersAfterPickup = true;
    [SerializeField] private bool destroyAfterPickup = true;
    [SerializeField] private bool waitUntilDialogueEnds = true;

    private bool _inited;
    private SpriteRenderer _spriteRenderer;
    private Collider2D[] _colliders;

    private void Awake()
    {
        _inited = false;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _colliders = GetComponents<Collider2D>();

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] == null) continue;
            _colliders[i].isTrigger = true;
            _colliders[i].enabled = true;
        }

        if (db == null)
        {
            if (debuged) Debug.LogWarning("ItemDatabaseSO is not assigned.", this);
            return;
        }

        if (itemInfos == null)
        {
            if (debuged) Debug.LogWarning("ItemInfos is not assigned.", this);
            return;
        }

        for (int i = 0; i < itemInfos.Count; i++)
        {
            if (itemInfos[i].itemSO == null)
            {
                itemInfos.RemoveAt(i);
                i--;
                if (debuged) Debug.LogWarning($"{gameObject.name} has a null itemSO.", this);
                continue;
            }

            bool found = false;
            for (int j = 0; j < db.items.Count; j++)
            {
                if (itemInfos[i].itemSO.id == db.items[j].id)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                if (debuged) Debug.LogWarning($"{itemInfos[i].itemSO.id} does not exist in {db.name}.", this);
                itemInfos.RemoveAt(i);
                i--;
            }
        }

        if (itemInfos.Count == 0)
        {
            if (debuged) Debug.LogWarning($"{gameObject.name} has no items to grant.", this);
            return;
        }

        _inited = true;
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!_inited)
        {
            if (debuged) Debug.LogWarning($"{gameObject.name}'s TriggerStep_ItemPickup is not initialized.", this);
            yield break;
        }

        if (ItemRuntime.Instance == null)
        {
            if (debuged) Debug.LogWarning("ItemRuntime.Instance is missing.", this);
            yield break;
        }

        if (debuged) PrintInventory();

        for (int i = 0; i < itemInfos.Count; i++)
            ItemRuntime.Instance.AddQuantity(itemInfos[i].itemSO.id, itemInfos[i].quantity);

        if (debuged) PrintInventory();

        if (hideSpriteBeforeDialogue && _spriteRenderer != null)
            _spriteRenderer.enabled = false;

        if (disableCollidersAfterPickup)
            SetCollidersEnabled(false);

        DialogueManager dialogueManager = DialogueManager.instance;
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(BuildPickupDialogue());

            if (waitUntilDialogueEnds)
            {
                while (dialogueManager != null && dialogueManager.isDialogueActive)
                    yield return null;
            }
        }

        if (destroyAfterPickup)
            Destroy(gameObject);
    }

    private Dialogue BuildPickupDialogue()
    {
        Dialogue pickupDialogue = new()
        {
            name = dialogue != null ? dialogue.name : string.Empty,
            sentences = dialogue != null ? dialogue.sentences : null,
            leftPortrait = dialogue != null ? dialogue.leftPortrait : null,
            rightPortrait = dialogue != null ? dialogue.rightPortrait : null
        };

        List<DialogueLine> lines = new();
        AddLines(lines, beforeDialogue);

        for (int i = 0; i < itemInfos.Count; i++)
        {
            ItemInfo itemInfo = itemInfos[i];
            lines.Add(new DialogueLine
            {
                text = $"루시는 '{itemInfo.itemSO.displayName}'를 {itemInfo.quantity}개 획득하였다!",
                focus = PortraitFocus.None
            });
        }

        AddDialogue(lines, dialogue);
        pickupDialogue.lines = lines.ToArray();
        return pickupDialogue;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (_colliders == null || _colliders.Length == 0)
            _colliders = GetComponents<Collider2D>();

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = enabled;
        }
    }

    private static void AddLines(List<DialogueLine> lines, DialogueLine[] dialogueLines)
    {
        if (dialogueLines == null)
            return;

        for (int i = 0; i < dialogueLines.Length; i++)
            lines.Add(dialogueLines[i]);
    }

    private static void AddDialogue(List<DialogueLine> lines, Dialogue source)
    {
        if (source == null)
            return;

        if (source.lines != null && source.lines.Length > 0)
        {
            AddLines(lines, source.lines);
            return;
        }

        if (source.sentences == null)
            return;

        for (int i = 0; i < source.sentences.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(source.sentences[i]))
                continue;

            lines.Add(new DialogueLine
            {
                text = source.sentences[i],
                speakerName = source.name,
                leftPortrait = source.leftPortrait,
                rightPortrait = source.rightPortrait,
                focus = PortraitFocus.None
            });
        }
    }

    private void PrintInventory()
    {
        string msg = "";
        InventoryItemEntry[] inven = ItemRuntime.Instance.CurrentData.items;
        for (int i = 0; i < inven.Length; i++)
            msg += $"{inven[i].id}: {inven[i].quantity}\n";

        Debug.Log(msg);
    }
}
