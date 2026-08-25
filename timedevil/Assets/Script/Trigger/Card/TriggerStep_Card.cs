using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerStep_Card : TriggerStepBase
{
    [SerializeField]
    [Header("DB")]
    [Tooltip("설정한 카드들이 유효한 지 검증용")]
    private CardDatabaseSO db;

    [SerializeField]
    [Header("Cards")]
    [Tooltip("트리거 작동시 지급될 카드들의 타입과 이름들")]
    private List<BaseCardSO> cards = new();

    [Header("Dialogue")]
    [Tooltip("카드 획득 안내 대화창을 표시합니다.")]
    [SerializeField] private bool showAcquisitionDialogue = true;

    [Tooltip("획득 안내 뒤에 이어서 재생할 대화입니다. 비워 두면 획득 안내만 표시합니다.")]
    [SerializeField] private Dialogue afterDialogue;

    [Tooltip("대화가 끝날 때까지 다음 Route Step 실행을 기다립니다.")]
    [SerializeField] private bool waitUntilDialogueEnds = true;

    void Awake() {
        HashSet<BaseCardSO> registered_cards = new();
        for(int i = 0; i < cards.Count; i++) {
            if(cards[i]==null || !db.GetById(cards[i].id)) {
                cards.RemoveAt(i);
                i--;
                continue;
            }
            registered_cards.Add(cards[i]);
        }

        cards = new List<BaseCardSO>(registered_cards);
    }

    public override IEnumerator Execute(TriggerContext ctx) {
        if(CardStateRuntime.Instance == null) yield break;

        List<DialogueLine> lines = new();

        foreach(var card in cards) {
            if (card == null)
                continue;

            bool added = CardStateRuntime.Instance.AddOwned(card.id);
            if (!added)
                continue;

            lines.Add(new DialogueLine
            {
                text = $"루시는 '{GetCardDisplayName(card)}' 카드를 획득했습니다.",
                focus = PortraitFocus.None
            });
        }

        if (!showAcquisitionDialogue || lines.Count == 0)
            yield break;

        AppendDialogue(lines, afterDialogue);

        DialogueManager dialogueManager = DialogueManager.instance;
        if (dialogueManager == null)
        {
            Debug.LogWarning("[TriggerStep_Card] DialogueManager.instance가 없어 획득 대화를 표시할 수 없습니다.", this);
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

    private static string GetCardDisplayName(BaseCardSO card)
    {
        if (card == null)
            return string.Empty;

        if (IsGenericCardName(card.displayName))
        {
            string label = ExtractBracketLabel(card.display);
            if (!string.IsNullOrWhiteSpace(label))
                return label;
        }

        if (!string.IsNullOrWhiteSpace(card.displayName))
            return card.displayName;

        if (!string.IsNullOrWhiteSpace(card.id))
            return card.id;

        return card.name;
    }

    private static bool IsGenericCardName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return HasNumericSuffix(value, "AttackCard")
            || HasNumericSuffix(value, "DrawCard")
            || HasNumericSuffix(value, "MoveCard")
            || HasNumericSuffix(value, "Card");
    }

    private static bool HasNumericSuffix(string value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(prefix))
            return false;

        if (value.Length <= prefix.Length)
            return false;

        for (int i = prefix.Length; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
                return false;
        }

        return true;
    }

    private static string ExtractBracketLabel(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        int start = text.IndexOf('[');
        if (start < 0)
            return string.Empty;

        int end = text.IndexOf(']', start + 1);
        if (end <= start + 1)
            return string.Empty;

        return text.Substring(start + 1, end - start - 1);
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
