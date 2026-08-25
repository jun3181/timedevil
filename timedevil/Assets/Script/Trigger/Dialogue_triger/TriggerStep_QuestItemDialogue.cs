using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_QuestItemDialogue : TriggerStepBase
{
    [System.Serializable]
    private struct RewardItem
    {
        [Tooltip("Item granted on completion")]
        public ItemSO itemSO;

        [Min(1)]
        [Tooltip("Quantity to grant")]
        public int quantity;
    }

    [Header("Quest Item Condition")]
    [Tooltip("Item ID to check")]
    [SerializeField] private string itemId = "piece";

    [Tooltip("Minimum quantity required to play the completion dialogue")]
    [Min(1)]
    [SerializeField] private int requiredQuantity = 3;

    [Tooltip("Consume the required item quantity when the completion dialogue is selected")]
    [SerializeField] private bool consumeRequiredItemsOnComplete = false;

    [Tooltip("Consume the required items only once from this object")]
    [SerializeField] private bool consumeRequiredItemsOnlyOnce = true;

    [SerializeField, HideInInspector] private bool hasConsumedRequiredItems = false;

    [Header("Quest Dialogues")]
    [Tooltip("Dialogue played when the item condition is not met")]
    [SerializeField] private Dialogue defaultDialogue;

    [Tooltip("Dialogue played when the item condition is met")]
    [SerializeField] private Dialogue completeDialogue;

    [Header("Quest Completion Reward")]
    [Tooltip("Grant reward items together with the completion dialogue")]
    [SerializeField] private bool giveRewardOnComplete = false;

    [Tooltip("Reward items used when giveRewardOnComplete is enabled")]
    [SerializeField] private List<RewardItem> rewardItems = new();

    [Tooltip("Grant rewards only once from this object")]
    [SerializeField] private bool giveRewardOnlyOnce = true;

    [Header("Quest Reward Dialogue")]
    [Tooltip("Dialogue appended after reward acquisition lines")]
    [SerializeField] private Dialogue rewardAfterDialogue;

    [SerializeField, HideInInspector] private bool hasGivenReward = false;

    [Header("Quest Completion Route")]
    [Tooltip("TriggerRouter to run after the completion dialogue ends")]
    [SerializeField] private TriggerRouter completionRouter;

    [Tooltip("Route key to run after the completion dialogue ends")]
    [SerializeField] private string completionRouteKey;

    [Tooltip("Run the completion route only once from this object")]
    [SerializeField] private bool triggerCompletionRouteOnlyOnce = true;

    [SerializeField, HideInInspector] private bool hasTriggeredCompletionRoute = false;

    [Header("Flow")]
    [Tooltip("Wait until the selected dialogue ends before the next route step runs")]
    [SerializeField] private bool waitUntilDialogueEnds = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        DialogueManager dialogueManager = DialogueManager.instance;
        if (dialogueManager == null)
        {
            Debug.LogError("[TriggerStep_QuestItemDialogue] DialogueManager.instance is missing.", this);
            yield break;
        }

        int currentQuantity = 0;
        if (ItemRuntime.Instance != null && !string.IsNullOrEmpty(itemId))
            currentQuantity = ItemRuntime.Instance.GetQuantity(itemId);

        bool isComplete = currentQuantity >= requiredQuantity;
        Dialogue selected = isComplete ? completeDialogue : defaultDialogue;

        if (selected == null)
        {
            Debug.LogWarning($"[TriggerStep_QuestItemDialogue] Dialogue is empty. (isComplete={isComplete}, itemId={itemId}, qty={currentQuantity})", this);
            yield break;
        }

        ConsumeRequiredItemsIfNeeded(isComplete);

        List<RewardItem> givenRewards = GiveCompletionRewardIfNeeded(isComplete);
        Dialogue dialogueToPlay = givenRewards.Count > 0
            ? BuildRewardDialogue(selected, givenRewards)
            : selected;

        bool shouldRunCompletionRoute = TryReserveCompletionRoute(isComplete);

        dialogueManager.StartDialogue(dialogueToPlay);

        if (waitUntilDialogueEnds || shouldRunCompletionRoute)
        {
            while (dialogueManager != null && dialogueManager.isDialogueActive)
                yield return null;
        }

        if (shouldRunCompletionRoute)
            completionRouter.RequestRoute(completionRouteKey, BuildCompletionContext(ctx));
    }

    private void ConsumeRequiredItemsIfNeeded(bool isComplete)
    {
        if (!isComplete || !consumeRequiredItemsOnComplete)
            return;

        if (consumeRequiredItemsOnlyOnce && hasConsumedRequiredItems)
            return;

        if (ItemRuntime.Instance == null || string.IsNullOrEmpty(itemId))
            return;

        ItemRuntime.Instance.AddQuantity(itemId, -Mathf.Max(1, requiredQuantity));
        hasConsumedRequiredItems = true;
    }

    private bool TryReserveCompletionRoute(bool isComplete)
    {
        if (!isComplete)
            return false;

        if (completionRouter == null || string.IsNullOrWhiteSpace(completionRouteKey))
            return false;

        if (triggerCompletionRouteOnlyOnce && hasTriggeredCompletionRoute)
            return false;

        hasTriggeredCompletionRoute = true;
        return true;
    }

    private TriggerContext BuildCompletionContext(TriggerContext ctx)
    {
        GameObject playerObject = ctx?.instigator;
        PlayerMove playerMove = ctx?.playerMove;
        Collider2D playerCollider = ctx?.instigatorCollider;

        if (playerObject == null)
        {
            playerObject = GameObject.FindWithTag("Player");
            playerMove = playerObject != null ? playerObject.GetComponent<PlayerMove>() : null;
            playerCollider = playerObject != null ? playerObject.GetComponent<Collider2D>() : null;
        }

        return new TriggerContext(
            trigger: null,
            router: completionRouter,
            instigator: playerObject,
            instigatorCollider: playerCollider,
            playerMove: playerMove
        );
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
            Debug.LogWarning("[TriggerStep_QuestItemDialogue] ItemRuntime.Instance is missing; rewards cannot be granted.", this);
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
            lines.Add(new DialogueLine
            {
                text = $"루시는 '{rewardItem.itemSO.displayName}'를 {rewardItem.quantity}개 획득하였다!",
                speakerName = baseDialogue.name,
                focus = PortraitFocus.None
            });
        }

        AddDialogueLines(lines, rewardAfterDialogue);
        rewardDialogue.lines = lines.ToArray();

        return rewardDialogue;
    }

    private static void AddDialogueLines(List<DialogueLine> lines, Dialogue dialogue)
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
