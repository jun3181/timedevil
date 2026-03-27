using UnityEngine;

// Dialog 레이어 오브젝트 등에 붙여서, 상호작용 시 Dialogue를 재생한다.
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("기본 대화")]
    [Tooltip("상호작용 시 재생되는 대화")]
    public Dialogue dialogue;

    [Header("Debug")]
    public bool debugLog = true;

    public void Interact()
    {
        if (DialogueManager.instance == null)
        {
            Debug.LogError("[DialogueInteractable] DialogueManager.instance가 없습니다.");
            return;
        }

        if (dialogue == null)
        {
            Debug.LogWarning($"[DialogueInteractable] 재생할 dialogue가 비었습니다: {name}");
            return;
        }

        if (debugLog)
            Debug.Log($"[DialogueInteractable] StartDialogue: {name} (selected={dialogue.name})");

        DialogueManager.instance.StartDialogue(dialogue);
    }
}
