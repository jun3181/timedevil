using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_ItemReward : TriggerStepBase
{
    [System.Serializable]
    private struct RewardItem
    {
        [Tooltip("지급할 아이템")]
        public ItemSO itemSO;

        [Min(1)]
        [Tooltip("지급할 수량")]
        public int quantity;
    }

    [Header("Rewards")]
    [SerializeField] private List<RewardItem> rewardItems = new();

    [Tooltip("지급 직후 인벤토리를 디스크에 저장합니다.")]
    [SerializeField] private bool saveAfterReward = false;

    [Header("Dialogue")]
    [Tooltip("아이템 획득 안내 대화창을 표시합니다.")]
    [SerializeField] private bool showAcquisitionDialogue = true;

    [Tooltip("획득 안내 뒤에 이어서 재생할 대화입니다. 비워 두면 획득 안내만 표시합니다.")]
    [SerializeField] private Dialogue afterDialogue;

    [Tooltip("대화가 끝날 때까지 다음 Route Step 실행을 기다립니다.")]
    [SerializeField] private bool waitUntilDialogueEnds = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (ItemRuntime.Instance == null)
        {
            Debug.LogWarning("[TriggerStep_ItemReward] ItemRuntime.Instance가 없어 아이템을 지급할 수 없습니다.", this);
            yield break;
        }

        List<DialogueLine> lines = new();
        int givenItemKinds = 0;

        foreach (RewardItem rewardItem in rewardItems)
        {
            if (rewardItem.itemSO == null)
                continue;

            int quantity = Mathf.Max(1, rewardItem.quantity);
            ItemRuntime.Instance.AddQuantity(rewardItem.itemSO.id, quantity);
            givenItemKinds++;

            lines.Add(new DialogueLine
            {
                text = $"루시는 '{rewardItem.itemSO.displayName}'를 {quantity}개 획득하였다!",
                focus = PortraitFocus.None
            });
        }

        if (givenItemKinds == 0)
        {
            if (debugLog)
                Debug.LogWarning("[TriggerStep_ItemReward] 지급할 아이템이 설정되지 않았습니다.", this);
            yield break;
        }

        if (saveAfterReward)
            ItemRuntime.Instance.SaveToDisk();

        if (debugLog)
            Debug.Log($"[TriggerStep_ItemReward] 아이템 {givenItemKinds}종을 지급했습니다.", this);

        if (!showAcquisitionDialogue)
            yield break;

        AppendDialogue(lines, afterDialogue);

        DialogueManager dialogueManager = DialogueManager.instance;
        if (dialogueManager == null)
        {
            Debug.LogWarning("[TriggerStep_ItemReward] DialogueManager.instance가 없어 획득 대화를 표시할 수 없습니다.", this);
            yield break;
        }

        Dialogue dialogue = new()
        {
            name = afterDialogue != null ? afterDialogue.name : string.Empty,
            leftPortrait = afterDialogue != null ? afterDialogue.leftPortrait : null,
            rightPortrait = afterDialogue != null ? afterDialogue.rightPortrait : null,
            lines = lines.ToArray()
        };

        dialogueManager.StartDialogue(dialogue);

        if (waitUntilDialogueEnds)
        {
            while (dialogueManager != null && dialogueManager.isDialogueActive)
                yield return null;
        }
    }

    private static void AppendDialogue(List<DialogueLine> lines, Dialogue dialogue)
    {
        if (dialogue == null)
            return;

        if (dialogue.lines != null && dialogue.lines.Length > 0)
        {
            foreach (DialogueLine line in dialogue.lines)
            {
                if (!string.IsNullOrWhiteSpace(line.text))
                    lines.Add(line);
            }
            return;
        }

        if (dialogue.sentences == null)
            return;

        foreach (string sentence in dialogue.sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                continue;

            lines.Add(new DialogueLine
            {
                text = sentence,
                speakerName = dialogue.name,
                leftPortrait = dialogue.leftPortrait,
                rightPortrait = dialogue.rightPortrait,
                focus = PortraitFocus.None
            });
        }
    }
}
