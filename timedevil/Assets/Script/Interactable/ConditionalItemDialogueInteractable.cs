using UnityEngine;

/// <summary>
/// 인벤토리 내 특정 아이템 수량에 따라 다른 대화를 재생하는 상호작용 컴포넌트.
/// </summary>
public class ConditionalItemDialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("Condition")]
    [Tooltip("조건으로 체크할 아이템 ID")]
    [SerializeField] private string itemId = "piece";

    [Tooltip("완료 대화가 재생되기 위한 최소 수량")]
    [Min(1)]
    [SerializeField] private int requiredQuantity = 3;

    [Header("Dialogues")]
    [Tooltip("조건 미달 시 재생할 일반 대화")]
    [SerializeField] private Dialogue defaultDialogue;

    [Tooltip("조건 충족 시 재생할 완료 대화")]
    [SerializeField] private Dialogue completeDialogue;

    public void Interact()
    {
        if (DialogueManager.instance == null)
        {
            Debug.LogError("[ConditionalItemDialogueInteractable] DialogueManager.instance가 없습니다.");
            return;
        }

        int currentQuantity = 0;
        if (ItemRuntime.Instance != null && !string.IsNullOrEmpty(itemId))
            currentQuantity = ItemRuntime.Instance.GetQuantity(itemId);

        bool isComplete = currentQuantity >= requiredQuantity;
        Dialogue selected = isComplete ? completeDialogue : defaultDialogue;

        if (selected == null)
        {
            Debug.LogWarning($"[ConditionalItemDialogueInteractable] 재생할 대화가 비어 있습니다. (isComplete={isComplete}, itemId={itemId}, qty={currentQuantity})");
            return;
        }

        DialogueManager.instance.StartDialogue(selected);
    }
}
