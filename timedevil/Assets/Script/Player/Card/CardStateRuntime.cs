using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class CardStateRuntime : MonoBehaviour
{
    public static CardStateRuntime Instance { get; private set; }

    //  덱 최대 장수
    public const int MAX_DECK = 13;
    private static readonly string[] DefaultOwnedCardIds =
        Enumerable.Range(1, 10).Select(i => $"AttackCard{i}")
            .Concat(Enumerable.Range(1, 10).Select(i => $"DrawCard{i}"))
            .Concat(Enumerable.Range(1, 10).Select(i => $"MoveCard{i}"))
            .ToArray();

    private static readonly string[] ManagedDefaultCardIds =
        Enumerable.Range(1, 10).Select(i => $"AttackCard{i}")
            .Concat(Enumerable.Range(1, 10).Select(i => $"DrawCard{i}"))
            .Concat(Enumerable.Range(1, 10).Select(i => $"MoveCard{i}"))
            .ToArray();

    private static readonly string[] LegacyDefaultDeckIds =
    {
        "AttackCard1", "AttackCard2", "AttackCard3", "AttackCard4",
        "DrawCard1", "DrawCard2", "DrawCard3", "DrawCard4",
        "MoveCard1", "MoveCard2", "MoveCard3", "MoveCard4", "MoveCard5"
    };

    private static readonly string[] PreviousDefaultDeckIds =
    {
        "AttackCard1", "AttackCard2", "AttackCard3", "AttackCard4",
        "DrawCard1", "DrawCard2", "DrawCard3", "DrawCard4", "DrawCard5",
        "MoveCard1", "MoveCard2", "MoveCard3", "MoveCard4"
    };

    [Header("자동 저장 옵션 (기본 꺼짐)")]
    public bool saveOnDisable = false;
    public bool saveOnQuit = false;

    public CardSaveData Data { get; private set; }

    void Awake()
    {
        // 싱글톤 + 씬 유지
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Data = CardSaveStore.Load();
        EnsureDefaultBattleCardsSaved();

#if UNITY_EDITOR
        Debug.Log($"[CardStateRuntime] Loaded. owned={Data.owned?.Count ?? 0}, deck={Data.deck?.Count ?? 0}");
#endif
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnDisable()
    {
        if (!saveOnDisable) return;
        if (ShouldSkipEmptyInitialSave()) return;
        CardSaveStore.Save(Data);
    }

    void OnApplicationQuit()
    {
        if (!saveOnQuit) return;
        if (ShouldSkipEmptyInitialSave()) return;
        CardSaveStore.Save(Data);
    }

    public void SaveNow()
    {
        CardSaveStore.Save(Data);
#if UNITY_EDITOR
        Debug.Log("[CardStateRuntime] SaveNow → " + CardSaveStore.GetPath());
#endif
    }

    public void EnsureDefaultBattleCardsSaved()
    {
        bool changed = EnsureDefaultPlayerCards();
        if (!changed) return;

        CardSaveStore.Save(Data);
    }

    // ----- Owned 관리 -----
    public bool AddOwned(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        if (Data.owned == null) Data.owned = new System.Collections.Generic.List<string>();
        if (!Data.owned.Contains(cardId))
        {
            Data.owned.Add(cardId);
            return true;
        }
        return false;
    }

    public bool RemoveOwned(string cardId)
    {
        if (Data.owned == null) return false;
        bool removed = Data.owned.Remove(cardId);
        if (removed && Data.deck != null)
            Data.deck = Data.deck.Where(id => id != cardId).ToList();
        return removed;
    }

    // ----- Deck 관리 -----
    public int DeckCount => Data.deck?.Count ?? 0;
    public bool DeckContains(string id) => Data.deck != null && Data.deck.Contains(id);

    /// <summary>중복 금지 + 최대 장수 제한</summary>
    public bool TryAddToDeck(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (Data.deck == null) Data.deck = new System.Collections.Generic.List<string>();
        if (Data.deck.Contains(id)) return false;              // 중복 불가
        if (Data.deck.Count >= MAX_DECK) return false;
        Data.deck.Add(id);
        return true;
    }

    public bool RemoveFromDeck(string id)
    {
        if (Data.deck == null) return false;
        return Data.deck.Remove(id);
    }

    public void SetDeck(System.Collections.Generic.IEnumerable<string> ids)
    {
        Data.deck = ids?.ToList() ?? new System.Collections.Generic.List<string>();
        if (Data.deck.Count > MAX_DECK)
            Data.deck = Data.deck.Take(MAX_DECK).ToList();
        // 중복 제거
        Data.deck = Data.deck.Distinct().ToList();
    }

    // --- Helpers ---
    private bool EnsureDefaultPlayerCards()
    {
        if (Data == null) Data = new CardSaveData();

        bool changed = false;
        if (Data.owned == null)
        {
            Data.owned = new List<string>();
            changed = true;
        }

        if (Data.deck == null)
        {
            Data.deck = new List<string>();
            changed = true;
        }

        if (ContainsOnlyManagedDefaultCards(Data.owned))
        {
            var defaults = DefaultOwnedCardIds.ToList();
            if (!Data.owned.SequenceEqual(defaults))
            {
                Data.owned = defaults;
                changed = true;
            }
        }
        else
        {
            foreach (var id in DefaultOwnedCardIds)
            {
                if (Data.owned.Contains(id)) continue;
                Data.owned.Add(id);
                changed = true;
            }
        }

        if (IsLegacyDefaultDeck(Data.deck))
        {
            Data.deck.Clear();
            changed = true;
        }

        var normalizedDeck = Data.deck
            .Where(id => !string.IsNullOrEmpty(id) && Data.owned.Contains(id))
            .Distinct()
            .Take(MAX_DECK)
            .ToList();

        if (!Data.deck.SequenceEqual(normalizedDeck))
        {
            Data.deck = normalizedDeck;
            changed = true;
        }

        return changed;
    }

    private bool ShouldSkipEmptyInitialSave()
    {
        return IsEmpty(Data) && !File.Exists(CardSaveStore.GetPath());
    }

    private static bool IsLegacyDefaultDeck(List<string> deck)
    {
        return MatchesDeck(deck, LegacyDefaultDeckIds)
            || MatchesDeck(deck, PreviousDefaultDeckIds);
    }

    private static bool MatchesDeck(List<string> deck, string[] ids)
    {
        if (deck == null || ids == null || deck.Count != ids.Length) return false;

        return deck.Distinct().Count() == ids.Length
            && ids.All(deck.Contains);
    }

    private static bool ContainsOnlyManagedDefaultCards(List<string> owned)
    {
        return owned == null || owned.All(id => ManagedDefaultCardIds.Contains(id));
    }

    private static bool IsEmpty(CardSaveData d)
    {
        if (d == null) return true;
        int ownedCount = d.owned?.Count ?? 0;
        int deckCount = d.deck?.Count ?? 0;
        return ownedCount == 0 && deckCount == 0;
    }
}
