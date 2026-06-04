using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform row;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardDatabaseSO cardDatabase;
    [SerializeField] private string resourcesFolder = "my_asset";

    [Header("Layout (single row, left aligned)")]
    [SerializeField] private float leftPadding = 8f;
    [SerializeField] private float rightPadding = 8f;
    [SerializeField] private float cardWidth = 120f;
    [SerializeField] private float cardSpacing = 150f;

    [Header("Select Overlay")]
    [SerializeField] private RectTransform select;

    //  Select 고정 크기
    [Header("Select Overlay Fixed Size")]
    [SerializeField] private bool useFixedSelectSize = true;
    [SerializeField] private Vector2 fixedSelectSize = new Vector2(113.2803f, 161.15f);

    private readonly List<GameObject> spawned = new();
    private readonly List<string> handIdsSnapshot = new();
    public IReadOnlyList<string> VisibleHandIds => handIdsSnapshot;

    private bool selecting = false;
    private int selectIndex = -1;

    public event System.Action<bool> onSelectModeChanged;
    public event System.Action<int> onSelectIndexChanged;

    public bool IsInSelectMode => selecting;
    public int CurrentSelectIndex => selectIndex;
    public int CardCount => handIdsSnapshot.Count;

    private void EnsureCardDatabase()
    {
        if (cardDatabase) return;

        var orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (orchestrator && orchestrator.CardDatabase)
            cardDatabase = orchestrator.CardDatabase;
    }

    private BaseCardSO GetCardById(string id)
    {
        EnsureCardDatabase();
        return cardDatabase ? cardDatabase.GetById(id) : null;
    }

    private Sprite GetFallbackSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        Sprite sprite = Resources.Load<Sprite>($"{resourcesFolder}/{id}");
        if (sprite) return sprite;

        string typeFolder = GetCardTypeFolder(id);
        return !string.IsNullOrEmpty(typeFolder)
            ? Resources.Load<Sprite>($"{resourcesFolder}/{typeFolder}/{id}")
            : null;
    }

    private static string GetCardTypeFolder(string id)
    {
        if (id.StartsWith("AttackCard")) return "AttackCard";
        if (id.StartsWith("DrawCard")) return "DrawCard";
        if (id.StartsWith("MoveCard")) return "MoveCard";
        return null;
    }

    void Awake()
    {
        if (!row) row = (RectTransform)transform;
        EnsureCardDatabase();
        HideCards();
        if (select) select.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (BattleDeckRuntime.Instance != null)
            BattleDeckRuntime.Instance.OnHandChanged += RebuildFromHand;

        RebuildFromHand();
    }

    void OnDisable()
    {
        if (BattleDeckRuntime.Instance != null)
            BattleDeckRuntime.Instance.OnHandChanged -= RebuildFromHand;
    }

    public void RebuildFromHand()
    {
        if (!row) row = (RectTransform)transform;
        if (!cardPrefab) return;
        var rt = BattleDeckRuntime.Instance;
        if (rt == null) return;

        bool restoreSelectMode = selecting;
        int restoreSelectIndex = selectIndex;

        handIdsSnapshot.Clear();
        var live = rt.GetHandIds();
        if (live != null) handIdsSnapshot.AddRange(live);

        ClearSpawned();

        float rowW = row.rect.width;
        float usable = Mathf.Max(0f, rowW - leftPadding - rightPadding);
        int n = handIdsSnapshot.Count;

        float step = 0f;
        if (n <= 1)
        {
            step = 0f;
        }
        else
        {
            float maxSpan = Mathf.Max(0f, usable - cardWidth);
            float needed = maxSpan / (n - 1);
            step = Mathf.Min(cardSpacing, Mathf.Max(0f, needed));
        }

        ClearSpawned();
        for (int i = 0; i < n; i++)
        {
            string id = handIdsSnapshot[i];
            var go = Instantiate(cardPrefab, row);
            go.name = $"HandCard_{id}";
            spawned.Add(go);

            BaseCardSO card = GetCardById(id);
            Sprite fallbackSprite = card && card.mainArtwork ? card.mainArtwork : GetFallbackSprite(id);

            var templateView = go.GetComponentInChildren<CardTemplateView>(true);
            if (templateView)
            {
                templateView.Bind(card, fallbackSprite);
            }
            else
            {
                var img = go.GetComponentInChildren<Image>() ?? go.AddComponent<Image>();
                img.sprite = fallbackSprite;
                img.preserveAspect = true;
                img.raycastTarget = true;
            }

            var rtItem = (RectTransform)go.transform;
            rtItem.anchorMin = rtItem.anchorMax = new Vector2(0f, 0.5f);
            rtItem.pivot = new Vector2(0f, 0.5f);

            float x = leftPadding + step * i;
            rtItem.anchoredPosition = new Vector2(x, 0f);

            rtItem.sizeDelta = new Vector2(cardWidth, rtItem.sizeDelta.y);
        }

        if (restoreSelectMode && n > 0)
        {
            selecting = true;
            if (select) select.gameObject.SetActive(true);
            selectIndex = -1;
            SetSelectIndexPublic(Mathf.Clamp(restoreSelectIndex, 0, n - 1));
        }
        else
        {
            ExitSelectMode();
        }

        ShowCards();
    }

    private void ClearSpawned()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
    }

    public void ShowCards()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) spawned[i].SetActive(true);
    }

    public void HideCards()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) spawned[i].SetActive(false);
        if (select) select.gameObject.SetActive(false);
        selecting = false;
        selectIndex = -1;
    }

    // ---- 선택모드 공개 API ----
    public void EnterSelectMode()
    {
        if (CardCount == 0) return;

        ShowCards();
        selecting = true;
        if (select) select.gameObject.SetActive(true);
        onSelectModeChanged?.Invoke(true);

        SetSelectIndexPublic(0); // 오른쪽 끝부터
    }

    public void ExitSelectMode()
    {
        if (!selecting) return;
        selecting = false;
        onSelectModeChanged?.Invoke(false);
        selectIndex = -1;
        if (select) select.gameObject.SetActive(false);
    }

    public void MoveSelect(int delta)
    {
        if (!selecting || CardCount == 0) return;
        int next = selectIndex + delta;
        next = (next % CardCount + CardCount) % CardCount; // 래핑
        SetSelectIndexPublic(next);
    }

    public void SetSelectIndexPublic(int idx)
    {
        if (CardCount == 0) return;

        int prev = selectIndex;
        selectIndex = Mathf.Clamp(idx, 0, CardCount - 1);
        if (selectIndex != prev) onSelectIndexChanged?.Invoke(selectIndex);

        if (select && selectIndex >= 0 && selectIndex < spawned.Count)
        {
            var target = (RectTransform)spawned[selectIndex].transform;

            // 부모/앵커 설정
            select.SetParent(row, false);

            //  선택 박스는 중앙 pivot 사용
            select.anchorMin = select.anchorMax = new Vector2(0f, 0.5f); // 행의 좌중앙 기준
            select.pivot = new Vector2(0.5f, 0.5f);

            //  카드 pivot(0,0.5) → 중앙 좌표 = anchoredX + cardWidth/2
            float centerX = target.anchoredPosition.x + target.sizeDelta.x * 0.5f;
            select.anchoredPosition = new Vector2(centerX, 0f);

            //  크기 고정
            if (useFixedSelectSize)
            {
                select.sizeDelta = fixedSelectSize;
            }
            else
            {
                // (옵션 분기: 필요시 예전 로직으로)
                select.sizeDelta = new Vector2(target.sizeDelta.x, target.sizeDelta.y);
            }

            select.localScale = Vector3.one;        // 스케일 흔적 제거
            select.SetAsLastSibling();              // 항상 맨 위로
        }
    }

    public RectTransform GetCardRect(int index)
    {
        if (index < 0 || index >= spawned.Count || !spawned[index]) return null;
        return (RectTransform)spawned[index].transform;
    }

    public string GetVisibleIdAt(int index)
    {
        if (index < 0 || index >= handIdsSnapshot.Count) return null;
        return handIdsSnapshot[index];
    }

    public List<RectTransform> GetAllCardRects()
    {
        var list = new List<RectTransform>();
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) list.Add((RectTransform)spawned[i].transform);
        return list;
    }
}
