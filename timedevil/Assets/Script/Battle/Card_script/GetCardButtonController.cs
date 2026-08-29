using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GetCardButtonController : MonoBehaviour
{
    private enum CardGrantMode
    {
        GuaranteedCard,
        RandomCard
    }

    [Header("Required")]
    [SerializeField] private CardDatabaseSO cardDatabase;

    [Header("Card")]
    [SerializeField] private CardGrantMode cardGrantMode = CardGrantMode.GuaranteedCard;
    [SerializeField] private string guaranteedCardId = "infit";
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
        if (string.IsNullOrWhiteSpace(guaranteedCardId))
            guaranteedCardId = "infit";

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

        BaseCardSO card = SelectCard();
        if (card == null)
        {
            Debug.LogWarning($"[GetCardButtonController] Failed to select card. Mode: {cardGrantMode}", this);
            return;
        }

        List<string> selectedIds = new() { card.id };

        foreach (string id in selectedIds)
            runtime.AddOwned(id);

        runtime.SetDeck(selectedIds);
        SyncBattleDeckRuntime(selectedIds);

        if (saveAfterClick)
            runtime.SaveNow();

        Debug.Log($"[GetCardButtonController] Card ready: {card.id} ({card.displayName}) / Mode: {cardGrantMode}", this);
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

    private BaseCardSO FindCardById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) || cardDatabase == null || cardDatabase.cards == null)
            return null;

        BaseCardSO card = cardDatabase.GetById(cardId);
        if (card != null)
            return card;

        foreach (BaseCardSO candidate in cardDatabase.cards)
        {
            if (candidate == null)
                continue;

            if (candidate.id == cardId)
                return candidate;
        }

        return null;
    }

    private BaseCardSO SelectCard()
    {
        if (cardGrantMode == CardGrantMode.RandomCard)
            return GetRandomCard();

        string cardId = string.IsNullOrWhiteSpace(guaranteedCardId) ? "infit" : guaranteedCardId.Trim();
        return FindCardById(cardId);
    }

    private BaseCardSO GetRandomCard()
    {
        if (cardDatabase == null || cardDatabase.cards == null)
            return null;

        List<BaseCardSO> candidates = new();
        foreach (BaseCardSO candidate in cardDatabase.cards)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.id))
                continue;

            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void SyncBattleDeckRuntime(List<string> selectedIds)
    {
        BattleDeckRuntime battleDeck = BattleDeckRuntime.Instance;
        if (battleDeck == null)
            return;

        battleDeck.deck.Clear();
        battleDeck.deck.AddRange(selectedIds);
    }
}
