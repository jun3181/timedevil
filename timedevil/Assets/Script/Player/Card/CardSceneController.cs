using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// 카드/덱 화면에서 슬롯 그려주고 선택/이동 처리.
/// - Card 패널: 보유(owned) 중 덱에 없는 카드들
/// - Deck 패널: 덱 목록 그대로
/// - E키: 현재 영역에서 반대 영역으로 한 장 이동
/// - 덱은 중복 불가 + 최대 13장 제한
/// - W키: 이전(메인) 씬으로 복귀 (PlayerReturnContext 사용)
/// </summary>
public class CardSceneController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private Transform cardPanel;    // 보유 카드 영역
    [SerializeField] private Transform deckPanel;    // 덱 영역
    [SerializeField] private Image explainImage;     // 확대 미리보기
    [SerializeField] private RectTransform selector; // 주황 박스

    [Header("Prefab & Resources")]
    [SerializeField] private GameObject cardSlotPrefab;              // Image 하나 들어있는 프리팹
    [SerializeField] private CardDatabaseSO cardDatabase;

    [Header("Paging")]
    [SerializeField] private int cardsPerPage = 25;

    [Header("Return Settings")]
    [SerializeField] private float graceSeconds = 1.0f;              // 복귀 직후 충돌 무적 시간(옵션)
    [SerializeField] private string worldVcamName = "CM vcam1";      // 복귀 씬에서 쓸 vcam 이름(없으면 자동 탐색)
    [SerializeField] private bool useFaderIfExists = true;           // SceneFader 있으면 사용

    // 내부 상태
    private readonly List<CardSlot> cardSlots = new();
    private readonly List<CardSlot> deckSlots = new();
    private int currentIndex = 0;
    private int cardPageIndex = 0;
    private int deckPageIndex = 0;
    private bool inDeck = false; // false = Card영역, true = Deck영역
    private CardTemplateView explainTemplateView;

    void Start()
    {
        ResolveCardDatabase();

        var runtime = EnsureCardStateRuntime();
        if (runtime != null)
        {
            PruneUnknownCards(runtime);
            runtime.EnsureDefaultBattleCardsSaved();
        }

        var data = runtime != null ? runtime.Data : new CardSaveData();

        var owned = FilterKnownCardIds(data.owned);
        var deck = FilterKnownCardIds(data.deck);
        var deckSet = new HashSet<string>(deck);

        // Card 패널: owned - deck
        foreach (var id in owned.Where(id => !deckSet.Contains(id)))
            AddSlotToPanel(cardPanel, cardSlots, id);

        // Deck 패널: deck 그대로
        foreach (var id in deck)
            AddSlotToPanel(deckPanel, deckSlots, id);

        UpdateSelector();
        UpdateExplain();
    }

    void Update()
    {
        HandleInput();
    }

    // ----------------------------------------------------

    void HandleInput()
    {
        // W: 이전 씬(메인)으로 복귀 — PlayerReturnContext 기반
        if (Input.GetKeyDown(KeyCode.W))
        {
            // 복귀 대상이 세팅되어 있으면 표준 복귀 루트 사용
            if (HasBattleReturnRequest())
            {
                // 카메라 재바인딩 요청
                PlayerReturnContext.CameraRebindRequested = true;
                PlayerReturnContext.TargetVcamName = string.IsNullOrWhiteSpace(worldVcamName) ? null : worldVcamName;

                // (옵션) 복귀 직후 무적
                if (graceSeconds > 0f)
                {
                    PlayerReturnContext.IsInGracePeriod = true;
                    PlayerReturnContext.GraceSecondsPending = graceSeconds;
                }
                else
                {
                    PlayerReturnContext.IsInGracePeriod = false;
                    PlayerReturnContext.GraceSecondsPending = 0f;
                }

                // 페이더 우선 복귀
                ApplyPreferredReturnVcam();
                SceneTransitionService.ReturnFromBattle(graceSeconds, useFaderIfExists);
            }
            else
            {
                // 폴백: 마지막 씬 기록이 있으면 그리로, 아니면 경고
                if (!string.IsNullOrEmpty(SceneHistory.LastSceneName))
                {
                    SceneTransitionService.LoadDefault(SceneHistory.LastSceneName, useFaderIfExists);
                }
                else
                {
                    Debug.LogWarning("[CardScene] 복귀 대상(ReturnSceneName)이 설정되지 않았습니다.");
                }

            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchPanel();
            return;
        }

        var list = inDeck ? deckSlots : cardSlots;
        if (list.Count == 0)
        {
            UpdateSelector();
            UpdateExplain();
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelector(1, 0);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelector(-1, 0);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelector(0, -1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelector(0, 1);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (!inDeck) MoveCard_toDeck_and_RemoveFromCard();
            else MoveCard_toCard_and_RemoveFromDeck();
        }
    }

    private bool HasBattleReturnRequest()
    {
        bool hasArrivalReturn =
            SceneArrivalContext.TryPeek(out SceneArrivalRequest request) &&
            request != null &&
            request.kind == SceneArrivalKind.BattleReturn &&
            !string.IsNullOrWhiteSpace(request.targetSceneName);

        return hasArrivalReturn || !string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName);
    }

    private void ApplyPreferredReturnVcam()
    {
        string preferredVcam = string.IsNullOrWhiteSpace(worldVcamName) ? null : worldVcamName;

        if (SceneArrivalContext.TryPeek(out SceneArrivalRequest request) &&
            request != null &&
            request.kind == SceneArrivalKind.BattleReturn)
        {
            request.requestCameraRebind = true;
            request.targetVcamName = preferredVcam;
            if (request.camera.hasCamera)
                request.camera.preferredVcamName = preferredVcam;
            return;
        }

        PlayerReturnContext.CameraRebindRequested = true;
        PlayerReturnContext.TargetVcamName = preferredVcam;
    }

    void SwitchPanel()
    {
        inDeck = !inDeck;
        currentIndex = 0;
        UpdateSelector();
        UpdateExplain();
    }

    void MoveSelector(int deltaColumn, int deltaRow)
    {
        var list = inDeck ? deckSlots : cardSlots;
        if (list.Count == 0) return;

        int columns = GetCurrentColumnCount();
        int pageSize = GetPageSize();
        int pageStart = (currentIndex / pageSize) * pageSize;
        int pageEnd = Mathf.Min(pageStart + pageSize, list.Count);
        int visibleIndex = currentIndex - pageStart;
        int currentColumn = visibleIndex % columns;

        if (deltaColumn < 0 && currentColumn == 0) return;
        if (deltaColumn > 0 && currentColumn >= columns - 1) return;

        int next = currentIndex + deltaColumn + (deltaRow * columns);
        if (deltaColumn != 0 && (next < pageStart || next >= pageEnd)) return;

        if (deltaRow > 0 && next >= pageEnd && pageEnd < list.Count)
        {
            int nextPageCount = Mathf.Min(pageSize, list.Count - pageEnd);
            next = pageEnd + Mathf.Min(currentColumn, nextPageCount - 1);
        }
        else if (deltaRow < 0 && next < pageStart && pageStart > 0)
        {
            int previousPageEnd = pageStart;
            next = Mathf.Min(previousPageEnd - 1, previousPageEnd - columns + currentColumn);
        }
        else if (next < pageStart || next >= pageEnd)
        {
            return;
        }

        currentIndex = next;
        UpdateSelector();
        UpdateExplain();
    }

    int GetCurrentColumnCount()
    {
        Transform panel = inDeck ? deckPanel : cardPanel;
        var grid = panel ? panel.GetComponent<GridLayoutGroup>() : null;
        if (grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, grid.constraintCount);

        return 1;
    }

    int GetPageSize()
    {
        return Mathf.Max(1, cardsPerPage);
    }

    void SetCurrentPageFromIndex()
    {
        int page = currentIndex / GetPageSize();
        if (inDeck) deckPageIndex = page;
        else cardPageIndex = page;
    }

    int ClampPageIndex(List<CardSlot> list, int pageIndex)
    {
        if (list == null || list.Count == 0) return 0;
        int maxPage = (list.Count - 1) / GetPageSize();
        return Mathf.Clamp(pageIndex, 0, maxPage);
    }

    void UpdatePageVisibility()
    {
        cardPageIndex = ClampPageIndex(cardSlots, cardPageIndex);
        deckPageIndex = ClampPageIndex(deckSlots, deckPageIndex);

        SetPageVisible(cardSlots, cardPageIndex);
        SetPageVisible(deckSlots, deckPageIndex);

        Canvas.ForceUpdateCanvases();
        RebuildPanelLayout(cardPanel);
        RebuildPanelLayout(deckPanel);
    }

    void SetPageVisible(List<CardSlot> list, int pageIndex)
    {
        int start = pageIndex * GetPageSize();
        int end = Mathf.Min(start + GetPageSize(), list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            list[i].gameObject.SetActive(i >= start && i < end);
        }
    }

    void RebuildPanelLayout(Transform panel)
    {
        if (panel is RectTransform rect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    // ----- 이동 로직 -----

    // Card → Deck (중복 불가 + 13장 제한)
    void MoveCard_toDeck_and_RemoveFromCard()
    {
        if (cardSlots.Count == 0) return;

        var slot = cardSlots[currentIndex];
        var id = slot.cardId;
        if (string.IsNullOrEmpty(id)) return;

        var rt = CardStateRuntime.Instance;
        if (rt == null) { Debug.LogWarning("[CardScene] CardStateRuntime 없음"); return; }

        if (!rt.TryAddToDeck(id))
        {
            if (rt.DeckContains(id))
                Debug.LogWarning("[CardScene] 이미 덱에 있는 카드입니다.");
            else if (rt.DeckCount >= CardStateRuntime.MAX_DECK)
                Debug.LogWarning($"[CardScene] 덱이 가득 찼습니다. (최대 {CardStateRuntime.MAX_DECK}장)");
            return;
        }

        // 덱 UI에 추가
        AddSlotToPanel(deckPanel, deckSlots, id);

        // 카드 패널에서 제거
        var removedGO = slot.gameObject;
        cardSlots.RemoveAt(currentIndex);
        Destroy(removedGO);

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, cardSlots.Count - 1));
        UpdateSelector();
        UpdateExplain();
    }

    // Deck → Card (되돌리기)
    void MoveCard_toCard_and_RemoveFromDeck()
    {
        if (deckSlots.Count == 0) return;

        var slot = deckSlots[currentIndex];
        var id = slot.cardId;
        if (string.IsNullOrEmpty(id)) return;

        var rt = CardStateRuntime.Instance;
        if (rt != null) rt.RemoveFromDeck(id);

        AddSlotToPanel(cardPanel, cardSlots, id);

        var removedGO = slot.gameObject;
        deckSlots.RemoveAt(currentIndex);
        Destroy(removedGO);

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, deckSlots.Count - 1));
        UpdateSelector();
        UpdateExplain();
    }

    // ----- 슬롯/UI -----

    void AddSlotToPanel(Transform parent, List<CardSlot> list, string cardId)
    {
        var go = Instantiate(cardSlotPrefab, parent);
        var slot = go.GetComponent<CardSlot>();
        if (!slot) slot = go.AddComponent<CardSlot>();

        BaseCardSO card = GetCardById(cardId);
        slot.Setup(cardId, card);

        list.Add(slot);
    }

    private BaseCardSO GetCardById(string cardId)
    {
        ResolveCardDatabase();

        BaseCardSO card = cardDatabase ? cardDatabase.GetById(cardId) : null;
        if (!card)
            Debug.LogWarning($"[CardScene] Card SO not found in CardDatabase: {cardId}");

        return card;
    }

    private bool IsKnownCardId(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
            return false;

        ResolveCardDatabase();
        return cardDatabase != null && cardDatabase.GetById(cardId) != null;
    }

    private List<string> FilterKnownCardIds(IEnumerable<string> ids)
    {
        var result = new List<string>();
        if (ids == null) return result;

        ResolveCardDatabase();
        if (cardDatabase == null)
        {
            foreach (string id in ids)
            {
                if (string.IsNullOrEmpty(id))
                    continue;
                if (!result.Contains(id))
                    result.Add(id);
            }

            return result;
        }

        foreach (string id in ids)
        {
            if (!IsKnownCardId(id))
                continue;
            if (!result.Contains(id))
                result.Add(id);
        }

        return result;
    }

    private void PruneUnknownCards(CardStateRuntime runtime)
    {
        if (runtime == null || runtime.Data == null)
            return;

        ResolveCardDatabase();
        if (cardDatabase == null)
            return;

        List<string> owned = FilterKnownCardIds(runtime.Data.owned);
        HashSet<string> ownedSet = new HashSet<string>(owned);
        List<string> deck = FilterKnownCardIds(runtime.Data.deck)
            .Where(id => ownedSet.Contains(id))
            .Take(CardStateRuntime.MAX_DECK)
            .ToList();

        bool changed =
            runtime.Data.owned == null || !runtime.Data.owned.SequenceEqual(owned) ||
            runtime.Data.deck == null || !runtime.Data.deck.SequenceEqual(deck);

        if (!changed)
            return;

        runtime.Data.owned = owned;
        runtime.Data.deck = deck;
        Debug.Log("[CardScene] Removed card ids that are not registered in CardDatabase.");
    }

    CardStateRuntime EnsureCardStateRuntime()
    {
        if (CardStateRuntime.Instance != null)
            return CardStateRuntime.Instance;

        var go = new GameObject("CardStateRuntime (Auto)");
        var runtime = go.AddComponent<CardStateRuntime>();
        Debug.Log("[CardScene] Auto-created CardStateRuntime.");
        return runtime;
    }

    private void ResolveCardDatabase()
    {
        if (cardDatabase != null) return;

        var orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (orchestrator != null && orchestrator.CardDatabase != null)
        {
            cardDatabase = orchestrator.CardDatabase;
            return;
        }

        CardDatabaseSO resourceDatabase = Resources.Load<CardDatabaseSO>("CardDatabase");
        if (resourceDatabase != null)
        {
            cardDatabase = resourceDatabase;
            return;
        }

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardDatabaseSO");
        if (guids == null || guids.Length == 0) return;

        string selectedPath = null;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith("/CardDatabase.asset"))
            {
                selectedPath = path;
                break;
            }

            selectedPath ??= path;
        }

        if (string.IsNullOrEmpty(selectedPath)) return;

        cardDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDatabaseSO>(selectedPath);
        if (cardDatabase != null && !Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    void UpdateSelector()
    {
        var list = inDeck ? deckSlots : cardSlots;
        if (list.Count == 0)
        {
            UpdatePageVisibility();
            if (!selector) return;
            selector.gameObject.SetActive(false);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, list.Count - 1);
        SetCurrentPageFromIndex();
        UpdatePageVisibility();

        if (!selector) return;
        selector.gameObject.SetActive(true);
        selector.position = list[currentIndex].transform.position;
    }

    void UpdateExplain()
    {
        var list = inDeck ? deckSlots : cardSlots;
        if (explainImage == null) return;

        EnsureExplainTemplate();

        if (list.Count == 0)
        {
            if (explainTemplateView) explainTemplateView.Clear();
            else explainImage.sprite = null;
            return;
        }

        var slot = list[currentIndex];
        BaseCardSO card = slot != null ? slot.Card : null;
        if (!card && slot != null)
            card = GetCardById(slot.cardId);

        if (explainTemplateView) explainTemplateView.Bind(card);
        else explainImage.sprite = null;
    }

    private void EnsureExplainTemplate()
    {
        if (!explainImage || explainTemplateView)
            return;

        explainTemplateView = explainImage.GetComponent<CardTemplateView>();
        if (!explainTemplateView)
            explainTemplateView = explainImage.gameObject.AddComponent<CardTemplateView>();
    }
}
