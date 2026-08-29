using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleDeckRuntime : MonoBehaviour
{
    public static BattleDeckRuntime Instance { get; private set; }

    public readonly List<string> deck = new();
    public readonly List<string> hand = new();
    private bool cardSelectionLocked;
    private string cardSelectionLockMessage = string.Empty;

    [Header("Rules")]
    [SerializeField] private int initialHandSize = 3;
    [SerializeField] private int maxHandSize = 3;
    [SerializeField, Min(0)] private int turnStartDrawCount = 2;

    public int MaxHandSize => maxHandSize;
    public int HandCount => hand.Count;
    public int OverCapCount => Mathf.Max(0, hand.Count - maxHandSize);
    public bool CanSelectCards => !cardSelectionLocked;
    public string CardSelectionLockMessage =>
        string.IsNullOrEmpty(cardSelectionLockMessage)
            ? BuildCardSelectionLockMessage(deck.Count + hand.Count)
            : cardSelectionLockMessage;

    public event Action OnHandChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        bool okNow = TryInitOnce();
        if (!okNow) StartCoroutine(CoRetryInit());
    }

    bool TryInitOnce()
    {
        deck.Clear();
        hand.Clear();

        var rt = ResolveCardStateRuntime();
        if (rt != null)
            rt.TryPrepareDeckForBattle(out _);

        var src = rt != null ? rt.Data?.deck : null;
        if (src == null || src.Count == 0)
        {
            UpdateCardSelectionLock(0);
            return false;
        }

        foreach (var id in src)
            if (!string.IsNullOrEmpty(id)) deck.Add(id);

        UpdateCardSelectionLock(deck.Count);

#if UNITY_EDITOR
        Debug.Log($"[BattleDeckRuntime] 덱 로드 완료: {deck.Count}장");
#endif
        Shuffle(deck);
        DrawInitial();
        if (hand.Count == 0) OnHandChanged?.Invoke();
        return true;
    }

    System.Collections.IEnumerator CoRetryInit()
    {
        for (int i = 0; i < 8; i++)
        {
            yield return null;
            if (TryInitOnce()) yield break;
        }
        OnHandChanged?.Invoke();
        Debug.LogWarning("[BattleDeckRuntime] 초기화 재시도 실패(덱 비어 있음)");
    }

    private void UpdateCardSelectionLock(int battleDeckCount)
    {
        int minDeckCount = CardStateRuntime.MIN_BATTLE_DECK;
        cardSelectionLocked = battleDeckCount < minDeckCount;
        cardSelectionLockMessage = cardSelectionLocked
            ? BuildCardSelectionLockMessage(battleDeckCount)
            : string.Empty;

        if (cardSelectionLocked)
            Debug.LogWarning($"[BattleDeckRuntime] {cardSelectionLockMessage}");
    }

    private static string BuildCardSelectionLockMessage(int battleDeckCount)
    {
        return $"덱 카드가 {CardStateRuntime.MIN_BATTLE_DECK}장 미만이라 Card를 선택할 수 없습니다. 현재 {battleDeckCount}장";
    }

    private CardStateRuntime ResolveCardStateRuntime()
    {
        if (CardStateRuntime.Instance != null)
            return CardStateRuntime.Instance;

        CardStateRuntime existing = FindObjectOfType<CardStateRuntime>(true);
        if (existing != null)
            return existing;

        var runtimeObject = new GameObject("CardStateRuntime (Battle Deck Auto)");
        return runtimeObject.AddComponent<CardStateRuntime>();
    }

    public static void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void DrawInitial()
    {
        Draw(Mathf.Min(initialHandSize, maxHandSize));
#if UNITY_EDITOR
        Debug.Log($"[BattleDeckRuntime] 초기 드로우 → [{string.Join(", ", hand)}]");
#endif
    }

    public void DrawOneIfNeeded()
    {
        if (hand.Count < maxHandSize) Draw(1);
    }

    public int DrawTurnStart()
    {
        return Draw(turnStartDrawCount, ignoreHandCap: true);
    }

    // 기본 드로우(최대치 적용)
    public void Draw(int n) => Draw(n, ignoreHandCap: false);

    //  카드 효과용: 최대치 무시 드로우
    public int Draw(int n, bool ignoreHandCap)
    {
        int drawn = 0;
        for (int k = 0; k < n; k++)
        {
            if (!ignoreHandCap && hand.Count >= maxHandSize) break;
            if (deck.Count == 0) break;

            string top = deck[0];
            deck.RemoveAt(0);
            hand.Add(top);
            drawn++;
        }
        if (drawn > 0) OnHandChanged?.Invoke();
        return drawn;
    }

    // 사용 → 덱 맨 밑
    public bool UseCardToBottom(int handIndex)
    {
        if (handIndex < 0 || handIndex >= hand.Count) return false;
        string id = hand[handIndex];
        hand.RemoveAt(handIndex);
        deck.Add(id);
        OnHandChanged?.Invoke();
        return true;
    }

    //  버리기(엔드 초과 처리): 선택 카드 덱 맨 밑으로
    public bool DiscardToBottom(int handIndex)
    {
        return UseCardToBottom(handIndex);
    }

    public IReadOnlyList<string> GetHandIds() => hand;

    public void SetHand(List<string> newHand)
    {
        hand.Clear();
        if (newHand != null)
        {
            foreach (var id in newHand)
                if (!string.IsNullOrEmpty(id)) hand.Add(id);
        }
        OnHandChanged?.Invoke();
    }
}
