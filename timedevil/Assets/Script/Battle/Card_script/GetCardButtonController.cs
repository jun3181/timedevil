using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GetCardButtonController : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private CardDatabaseSO cardDatabase;

    [Header("Deck")]
    [SerializeField, Min(1)] private int cardsToDeck = CardStateRuntime.MAX_DECK;
    [SerializeField] private bool saveAfterClick = false;

    private Button button;

    private void Awake()
    {
        EnsureButton();
        EnsureEventSystem();
    }

    private void OnEnable()
    {
        EnsureButton();

        if (button == null) return;
        button.onClick.RemoveListener(HandleGetCardClicked);
        button.onClick.AddListener(HandleGetCardClicked);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleGetCardClicked);
    }

    private void OnValidate()
    {
        cardsToDeck = Mathf.Clamp(cardsToDeck, 1, CardStateRuntime.MAX_DECK);
        if (cardDatabase == null)
            cardDatabase = Resources.Load<CardDatabaseSO>("CardDatabase");
    }

    public void HandleGetCardClicked()
    {
        if (cardDatabase == null)
            cardDatabase = Resources.Load<CardDatabaseSO>("CardDatabase");

        if (cardDatabase == null || cardDatabase.cards == null || cardDatabase.cards.Count == 0)
        {
            Debug.LogWarning("[GetCardButtonController] CardDatabaseSO is missing or empty.", this);
            return;
        }

        CardStateRuntime runtime = EnsureCardStateRuntime();
        if (runtime == null || runtime.Data == null)
        {
            Debug.LogWarning("[GetCardButtonController] CardStateRuntime is missing.", this);
            return;
        }

        List<string> candidateIds = CollectUniqueCardIds();
        if (candidateIds.Count == 0)
        {
            Debug.LogWarning("[GetCardButtonController] No valid card ids in CardDatabaseSO.", this);
            return;
        }

        Shuffle(candidateIds);

        int takeCount = Mathf.Min(Mathf.Min(cardsToDeck, CardStateRuntime.MAX_DECK), candidateIds.Count);
        List<string> selectedIds = candidateIds.GetRange(0, takeCount);

        foreach (string id in selectedIds)
            runtime.AddOwned(id);

        runtime.SetDeck(selectedIds);
        SyncBattleDeckRuntime(selectedIds);

        if (saveAfterClick)
            runtime.SaveNow();

        if (takeCount < cardsToDeck)
            Debug.LogWarning($"[GetCardButtonController] Only {takeCount} valid cards were available.", this);

        Debug.Log($"[GetCardButtonController] Random deck ready: {runtime.DeckCount}/{CardStateRuntime.MAX_DECK} ({string.Join(", ", selectedIds)})", this);
    }

    private void EnsureButton()
    {
        if (button == null)
            button = GetComponentInChildren<Button>(true);
    }

    private CardStateRuntime EnsureCardStateRuntime()
    {
        if (CardStateRuntime.Instance != null)
            return CardStateRuntime.Instance;

        CardStateRuntime existing = FindObjectOfType<CardStateRuntime>(true);
        if (existing != null)
            return existing;

        GameObject runtimeObject = new GameObject("CardStateRuntime (GetCard Auto)");
        return runtimeObject.AddComponent<CardStateRuntime>();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private List<string> CollectUniqueCardIds()
    {
        List<string> ids = new();
        HashSet<string> seen = new();

        foreach (BaseCardSO card in cardDatabase.cards)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.id))
                continue;

            if (seen.Add(card.id))
                ids.Add(card.id);
        }

        return ids;
    }

    private void SyncBattleDeckRuntime(List<string> selectedIds)
    {
        BattleDeckRuntime battleDeck = BattleDeckRuntime.Instance;
        if (battleDeck == null)
            return;

        battleDeck.deck.Clear();
        battleDeck.deck.AddRange(selectedIds);
        BattleDeckRuntime.Shuffle(battleDeck.deck);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
