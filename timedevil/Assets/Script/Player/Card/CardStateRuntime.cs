using UnityEngine;
using System.Linq;
using System.IO;
public class CardStateRuntime : MonoBehaviour
{
    public static CardStateRuntime Instance { get; private set; }

    // ✅ 덱 최대 장수
    public const int MAX_DECK = 13;

    [Header("자동 저장 옵션 (기본 꺼짐)")]
    public bool saveOnDisable = false;
    public bool saveOnQuit = false;

    public CardSaveData Data { get; private set; }

    void Awake()
    {
        // 싱글톤 + 씬 유지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 파일이 없으면 비어있는 상태로 시작
        Data = CardSaveStore.Load();

#if UNITY_EDITOR
        Debug.Log($"[CardStateRuntime] Loaded. owned={Data.owned?.Count ?? 0}, deck={Data.deck?.Count ?? 0}");
#endif
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

    /// <summary>중복 금지 + 최대 13장 제한</summary>
    public bool TryAddToDeck(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (Data.deck == null) Data.deck = new System.Collections.Generic.List<string>();
        if (Data.deck.Contains(id)) return false;              // 중복 불가
        if (Data.deck.Count >= MAX_DECK) return false;         // 13장 제한
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
    private bool ShouldSkipEmptyInitialSave()
    {
        return IsEmpty(Data) && !File.Exists(CardSaveStore.GetPath());
    }

    private static bool IsEmpty(CardSaveData d)
    {
        if (d == null) return true;
        int ownedCount = d.owned?.Count ?? 0;
        int deckCount = d.deck?.Count ?? 0;
        return ownedCount == 0 && deckCount == 0;
    }
}
