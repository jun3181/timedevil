using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class CardStateRuntime : MonoBehaviour
{
    public static CardStateRuntime Instance { get; private set; }

    //  덱 최대 장수
    public const int MAX_DECK = 13;
    public const int MIN_BATTLE_DECK = 10;
    private static readonly string[] LegacyAutoOwnedCardIds =
        Enumerable.Range(1, 4).Select(i => $"AttackCard{i}")
            .Concat(Enumerable.Range(1, 5).Select(i => $"DrawCard{i}"))
            .Concat(Enumerable.Range(1, 4).Select(i => $"MoveCard{i}"))
            .ToArray();

    private static readonly string[] LegacyDefaultDeckIds =
    {
        "AttackCard1", "AttackCard2", "AttackCard3", "AttackCard4",
        "DrawCard1", "DrawCard2", "DrawCard3", "DrawCard4",
        "MoveCard1", "MoveCard2", "MoveCard3", "MoveCard4", "MoveCard5"
    };

    private static readonly Dictionary<string, string> LegacyCardIdMap = new Dictionary<string, string>
    {
        { "AttackCard1", "unrest" },
        { "AttackCard2", "celebration" },
        { "AttackCard3", "obsession" },
        { "AttackCard4", "calm" },
        { "AttackCard5", "jealousy" },
        { "AttackCard6", "cynicism" },
        { "AttackCard7", "confidence" },
        { "AttackCard8", "trust" },
        { "AttackCard9", "guilt" },
        { "AttackCard10", "brave" },
        { "DrawCard1", "panic" },
        { "DrawCard2", "discard" },
        { "DrawCard3", "longing" },
        { "DrawCard4", "attachment" },
        { "DrawCard5", "question" },
        { "DrawCard6", "excitement" },
        { "DrawCard7", "contempt" },
        { "DrawCard8", "relief" },
        { "DrawCard9", "humiliation" },
        { "DrawCard10", "reassurance" },
        { "MoveCard1", "hope" },
        { "MoveCard2", "passion" },
        { "MoveCard3", "attachment" },
        { "MoveCard4", "sympathy" },
        { "MoveCard5", "fascination" },
        { "MoveCard6", "content" },
        { "MoveCard7", "regret" },
        { "MoveCard8", "depression" },
        { "MoveCard9", "hatred" },
        { "MoveCard10", "fear" },
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

    public void LoadFromDisk()
    {
        Data = CardSaveStore.Load();
        EnsureDefaultBattleCardsSaved();
#if UNITY_EDITOR
        Debug.Log($"[CardStateRuntime] LoadFromDisk. owned={Data.owned?.Count ?? 0}, deck={Data.deck?.Count ?? 0}");
#endif
    }

    public void EnsureDefaultBattleCardsSaved()
    {
        bool changed = EnsureCardDataInitialized();
        if (!changed) return;

        CardSaveStore.Save(Data);
    }

    // ----- Owned 관리 -----
    public bool AddOwned(string cardId)
    {
        cardId = NormalizeCardId(cardId);
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
        cardId = NormalizeCardId(cardId);
        if (Data.owned == null) return false;
        bool removed = Data.owned.Remove(cardId);
        if (removed && Data.deck != null)
            Data.deck = Data.deck.Where(id => id != cardId).ToList();
        return removed;
    }

    // ----- Deck 관리 -----
    public int DeckCount => Data.deck?.Count ?? 0;
    public bool DeckContains(string id) => Data.deck != null && Data.deck.Contains(NormalizeCardId(id));

    /// <summary>중복 금지 + 최대 장수 제한</summary>
    public bool TryAddToDeck(string id)
    {
        id = NormalizeCardId(id);
        if (string.IsNullOrEmpty(id)) return false;
        if (Data.deck == null) Data.deck = new System.Collections.Generic.List<string>();
        if (Data.deck.Contains(id)) return false;              // 중복 불가
        if (Data.deck.Count >= MAX_DECK) return false;
        Data.deck.Add(id);
        return true;
    }

    public bool RemoveFromDeck(string id)
    {
        id = NormalizeCardId(id);
        if (Data.deck == null) return false;
        return Data.deck.Remove(id);
    }

    public void SetDeck(System.Collections.Generic.IEnumerable<string> ids)
    {
        Data.deck = ids?.Select(NormalizeCardId).ToList() ?? new System.Collections.Generic.List<string>();
        if (Data.deck.Count > MAX_DECK)
            Data.deck = Data.deck.Take(MAX_DECK).ToList();
        // 중복 제거
        Data.deck = Data.deck.Distinct().ToList();
    }

    public bool TryPrepareDeckForBattle(out string failureReason)
    {
        failureReason = string.Empty;

        bool changed = EnsureCardDataInitialized();
        int targetCount = Mathf.Min(MIN_BATTLE_DECK, MAX_DECK);

        if (Data.deck.Count < targetCount)
        {
            HashSet<string> deckSet = new HashSet<string>(Data.deck);
            foreach (string ownedId in Data.owned)
            {
                if (Data.deck.Count >= targetCount)
                    break;

                string id = NormalizeCardId(ownedId);
                if (string.IsNullOrEmpty(id) || deckSet.Contains(id))
                    continue;

                Data.deck.Add(id);
                deckSet.Add(id);
                changed = true;
            }
        }

        if (changed)
            CardSaveStore.Save(Data);

        if (Data.deck.Count >= targetCount)
            return true;

        int ownedCount = Data.owned?.Count ?? 0;
        failureReason = $"덱 카드가 {targetCount}장 미만입니다. 배틀은 시작되지만 Card 선택은 잠깁니다. 현재 덱 {Data.deck.Count}장 / 보유 {ownedCount}장";
        return false;
    }

    // --- Helpers ---
    private bool EnsureCardDataInitialized()
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

        if (HasOnlyLegacyAutoDefaults(Data))
        {
            Data.owned.Clear();
            Data.deck.Clear();
            changed = true;
            return changed;
        }

        changed |= NormalizeCardIdsInPlace(Data.owned);
        changed |= NormalizeCardIdsInPlace(Data.deck);

        var normalizedOwned = Data.owned
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        if (!Data.owned.SequenceEqual(normalizedOwned))
        {
            Data.owned = normalizedOwned;
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

    private static string NormalizeCardId(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
            return cardId;

        return LegacyCardIdMap.TryGetValue(cardId, out string normalizedId) ? normalizedId : cardId;
    }

    private static bool NormalizeCardIdsInPlace(List<string> cardIds)
    {
        if (cardIds == null)
            return false;

        bool changed = false;
        for (int i = 0; i < cardIds.Count; i++)
        {
            string normalizedId = NormalizeCardId(cardIds[i]);
            if (cardIds[i] == normalizedId)
                continue;

            cardIds[i] = normalizedId;
            changed = true;
        }

        return changed;
    }

    private bool ShouldSkipEmptyInitialSave()
    {
        return IsEmpty(Data) && !File.Exists(CardSaveStore.GetPath());
    }

    private static bool HasOnlyLegacyAutoDefaults(CardSaveData data)
    {
        if (data == null || data.owned == null || data.deck == null)
            return false;

        return HasSameCards(data.owned, LegacyAutoOwnedCardIds)
            && (HasSameCards(data.deck, LegacyAutoOwnedCardIds) || HasSameCards(data.deck, LegacyDefaultDeckIds));
    }

    private static bool HasSameCards(List<string> cards, string[] expected)
    {
        if (cards == null || expected == null) return false;

        var normalizedCards = cards
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var normalizedExpected = expected
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        return normalizedCards.SequenceEqual(normalizedExpected);
    }

    private static bool IsEmpty(CardSaveData d)
    {
        if (d == null) return true;
        int ownedCount = d.owned?.Count ?? 0;
        int deckCount = d.deck?.Count ?? 0;
        return ownedCount == 0 && deckCount == 0;
    }
}
