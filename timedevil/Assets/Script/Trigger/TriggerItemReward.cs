using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class TriggerItemReward : MonoBehaviour
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
    [Tooltip("획득 안내 뒤에 이어서 재생할 대화입니다. 비워 두면 획득 안내만 표시합니다.")]
    [SerializeField] private Dialogue afterDialogue;

    [Header("Trigger")]
    [Tooltip("켜져 있으면 이 트리거는 한 번만 지급합니다.")]
    [SerializeField] private bool rewardOnlyOnce = true;

    [Tooltip("플레이어의 PlayerMove 컴포넌트로 진입 대상을 확인합니다.")]
    [SerializeField] private bool requirePlayerMove = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool hasGivenReward;
    private Collider2D triggerCollider;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
            if (debugLog)
                Debug.LogWarning("[TriggerItemReward] Collider2D.isTrigger가 꺼져 있어서 켰습니다.", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (rewardOnlyOnce && hasGivenReward)
            return;

        if (requirePlayerMove && other.GetComponentInParent<PlayerMove>() == null)
            return;

        GiveReward();
    }

    private void GiveReward()
    {
        if (ItemRuntime.Instance == null)
        {
            Debug.LogWarning("[TriggerItemReward] ItemRuntime.Instance가 없어 아이템을 지급할 수 없습니다.", this);
            return;
        }

        List<DialogueLine> lines = new();

        foreach (RewardItem rewardItem in rewardItems)
        {
            if (rewardItem.itemSO == null)
                continue;

            int quantity = Mathf.Max(1, rewardItem.quantity);
            ItemRuntime.Instance.AddQuantity(rewardItem.itemSO.id, quantity);
            lines.Add(new DialogueLine
            {
                text = $"루시는 '{rewardItem.itemSO.displayName}'를 {quantity}개 획득하였다!",
                focus = PortraitFocus.None
            });
        }

        if (lines.Count == 0)
        {
            if (debugLog)
                Debug.LogWarning("[TriggerItemReward] 지급할 아이템이 설정되지 않았습니다.", this);
            return;
        }

        hasGivenReward = true;

        if (saveAfterReward)
            ItemRuntime.Instance.SaveToDisk();

        AppendDialogue(lines, afterDialogue);
        ShowDialogue(lines);

        if (debugLog)
            Debug.Log($"[TriggerItemReward] {lines.Count}개의 획득 안내 대화를 생성했습니다.", this);

        if (rewardOnlyOnce && triggerCollider != null)
            triggerCollider.enabled = false;
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

    private void ShowDialogue(List<DialogueLine> lines)
    {
        if (DialogueManager.instance == null)
        {
            Debug.LogWarning("[TriggerItemReward] DialogueManager.instance가 없어 획득 대화를 표시할 수 없습니다.", this);
            return;
        }

        Dialogue dialogue = new()
        {
            name = afterDialogue != null ? afterDialogue.name : string.Empty,
            leftPortrait = afterDialogue != null ? afterDialogue.leftPortrait : null,
            rightPortrait = afterDialogue != null ? afterDialogue.rightPortrait : null,
            lines = lines.ToArray()
        };

        DialogueManager.instance.StartDialogue(dialogue);
    }
}
