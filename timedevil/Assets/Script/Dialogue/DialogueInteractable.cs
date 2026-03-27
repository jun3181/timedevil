using UnityEngine;

// Dialog 레이어 오브젝트 등에 붙여서, 상호작용 시 Dialogue를 재생한다.
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("기본 대화")]
    [Tooltip("조건을 사용하지 않거나, 조건 미달 시 재생되는 대화")]
    public Dialogue dialogue;

    [Header("조건부 완료 대화 (선택)")]
    [Tooltip("체크를 켜면 itemId 수량이 requiredQuantity 이상일 때 completeDialogue를 재생")]
    [SerializeField] private bool useInventoryCondition = false;

    [SerializeField] private string itemId = "piece";

    [Min(1)]
    [SerializeField] private int requiredQuantity = 3;

    [Tooltip("조건 충족 시 재생할 대화. 비어 있으면 기본 대화를 사용")]
    [SerializeField] private Dialogue completeDialogue;

    [Header("Debug")]
    public bool debugLog = true;

    public void Interact()
    {
        if (DialogueManager.instance == null)
        {
            Debug.LogError("[DialogueInteractable] DialogueManager.instance가 없습니다.");
            return;
        }

        Dialogue selectedDialogue = SelectDialogue();
        if (selectedDialogue == null)
        {
            Debug.LogWarning($"[DialogueInteractable] 재생할 dialogue가 비었습니다: {name}");
            return;
        }

        if (debugLog)
            Debug.Log($"[DialogueInteractable] StartDialogue: {name} (selected={selectedDialogue.name})");

        DialogueManager.instance.StartDialogue(selectedDialogue);
    }

    private Dialogue SelectDialogue()
    {
        if (!useInventoryCondition)
            return dialogue;

        if (ItemRuntime.Instance == null || string.IsNullOrEmpty(itemId))
            return dialogue;

        int quantity = ItemRuntime.Instance.GetQuantity(itemId);
        bool isComplete = quantity >= requiredQuantity;

        if (isComplete && completeDialogue != null)
            return completeDialogue;

        return dialogue;
    }
}
