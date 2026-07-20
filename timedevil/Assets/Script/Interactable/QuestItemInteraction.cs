using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리 내 특정 아이템 수량에 따라 다른 대화를 재생하는 상호작용 컴포넌트.
/// </summary>
public class QuestItemInteraction : MonoBehaviour, IInteractable
{
    [System.Serializable]
    private struct RewardItem
    {
        [Tooltip("완료 시 지급할 아이템")]
        public ItemSO itemSO;

        [Min(1)]
        [Tooltip("지급할 수량")]
        public int quantity;
    }

    [Header("Quest Item Condition")]
    [Tooltip("조건으로 체크할 아이템 ID")]
    [SerializeField] private string itemId = "piece";

    [Tooltip("완료 대화가 재생되기 위한 최소 수량")]
    [Min(1)]
    [SerializeField] private int requiredQuantity = 3;

    [Header("Quest Dialogues")]
    [Tooltip("조건 미달 시 재생할 일반 대화")]
    [SerializeField] private Dialogue defaultDialogue;

    [Tooltip("조건 충족 시 재생할 완료 대화")]
    [SerializeField] private Dialogue completeDialogue;

    [Header("Quest Completion Reward")]
    [Tooltip("조건 충족 시 완료 대화와 함께 보상 아이템을 지급할지 여부")]
    [SerializeField] private bool giveRewardOnComplete = false;

    [Tooltip("완료 시 지급할 아이템 목록. giveRewardOnComplete가 켜져 있을 때만 사용됩니다.")]
    [SerializeField] private List<RewardItem> rewardItems = new();

    [Tooltip("켜져 있으면 이 오브젝트에서는 보상을 한 번만 지급합니다.")]
    [SerializeField] private bool giveRewardOnlyOnce = true;

    [Header("Quest Reward Dialogue")]
    [Tooltip("보상 획득 안내 문구 뒤에 이어서 재생할 대화")]
    [SerializeField] private Dialogue rewardAfterDialogue;

    [SerializeField, HideInInspector] private bool hasGivenReward = false;

    public void Interact()
    {
        if (DialogueManager.instance == null)
        {
            Debug.LogError("[QuestItemInteraction] DialogueManager.instance가 없습니다.");
            return;
        }

        int currentQuantity = 0;
        if (ItemRuntime.Instance != null && !string.IsNullOrEmpty(itemId))
            currentQuantity = ItemRuntime.Instance.GetQuantity(itemId);

        bool isComplete = currentQuantity >= requiredQuantity;
        Dialogue selected = isComplete ? completeDialogue : defaultDialogue;

        if (selected == null)
        {
            Debug.LogWarning($"[QuestItemInteraction] 재생할 대화가 비어 있습니다. (isComplete={isComplete}, itemId={itemId}, qty={currentQuantity})");
            return;
        }

        List<RewardItem> givenRewards = GiveCompletionRewardIfNeeded(isComplete);
        Dialogue dialogueToPlay = givenRewards.Count > 0
            ? BuildRewardDialogue(selected, givenRewards)
            : selected;

        DialogueManager.instance.StartDialogue(dialogueToPlay);
    }

    private List<RewardItem> GiveCompletionRewardIfNeeded(bool isComplete)
    {
        List<RewardItem> givenRewards = new();

        if (!isComplete || !giveRewardOnComplete)
            return givenRewards;

        if (giveRewardOnlyOnce && hasGivenReward)
            return givenRewards;

        if (ItemRuntime.Instance == null)
        {
            Debug.LogWarning("[QuestItemInteraction] 보상 아이템을 지급할 수 없습니다. ItemRuntime.Instance가 없습니다.");
            return givenRewards;
        }

        foreach (RewardItem rewardItem in rewardItems)
        {
            if (rewardItem.itemSO == null)
                continue;

            RewardItem givenReward = rewardItem;
            givenReward.quantity = Mathf.Max(1, rewardItem.quantity);
            ItemRuntime.Instance.AddQuantity(givenReward.itemSO.id, givenReward.quantity);
            givenRewards.Add(givenReward);
        }

        if (givenRewards.Count > 0)
            hasGivenReward = true;

        return givenRewards;
    }

    private Dialogue BuildRewardDialogue(Dialogue baseDialogue, List<RewardItem> givenRewards)
    {
        Dialogue rewardDialogue = new()
        {
            name = baseDialogue.name,
            sentences = baseDialogue.sentences,
            leftPortrait = baseDialogue.leftPortrait,
            rightPortrait = baseDialogue.rightPortrait
        };

        List<DialogueLine> lines = new();
        AddDialogueLines(lines, baseDialogue);

        foreach (RewardItem rewardItem in givenRewards)
        {
            DialogueLine rewardLine = new()
            {
                text = $"루시는 '{rewardItem.itemSO.displayName}'를 {rewardItem.quantity}개 획득하였다!",
                speakerName = baseDialogue.name,
                focus = PortraitFocus.None
            };
            lines.Add(rewardLine);
        }

        AddDialogueLines(lines, rewardAfterDialogue);
        rewardDialogue.lines = lines.ToArray();

        return rewardDialogue;
    }

    private void AddDialogueLines(List<DialogueLine> lines, Dialogue dialogue)
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
                focus = PortraitFocus.None
            });
        }
    }
}