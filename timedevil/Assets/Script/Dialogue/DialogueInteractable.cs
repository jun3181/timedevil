using UnityEngine;

// Dialog 레이어 오브젝트에 이 컴포넌트를 붙이고, 인스펙터에서 Dialogue를 넣는다.
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("이 오브젝트가 가진 대화 데이터")]
    public Dialogue dialogue;

    [Header("Debug")]
    public bool debugLog = true;

    public void Interact()
    {
        if (dialogue == null)
        {
            Debug.LogWarning($"[DialogueInteractable] dialogue가 비어있음: {name}");
            return;
        }

        if (DialogueManager.instance == null)
        {
            Debug.LogError("[DialogueInteractable] DialogueManager.instance가 없습니다!");
            return;
        }

        if (debugLog) Debug.Log($"[DialogueInteractable] StartDialogue: {name}");
        DialogueManager.instance.StartDialogue(dialogue);
    }
}
