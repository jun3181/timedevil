using System.Collections.Generic;
using System.Collections;
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
    [SerializeField, Min(0.01f)] private float cardHeightPerWidth = 1.5f;
    [SerializeField] private float cardSpacing = 180f;

    [Header("Selected Card Emphasis")]
    [SerializeField] private bool emphasizeSelectedCard = true;
    [SerializeField, Min(1f)] private float selectedCardScale = 1.75f;
    [SerializeField, Min(1f)] private float selectedCardScaleAnimationStart = 1.6f;
    [SerializeField, Min(1f)] private float nonSelectedCardScaleAnimationStart = 1.15f;
    [SerializeField] private float cardBottomYOffset = 0f;
    [SerializeField] private bool animateSelectedCardScale = true;
    [SerializeField] private bool animateSelectedCardLayout = true;
    [SerializeField, Min(0.01f)] private float selectedCardLayoutDuration = 0.18f;
    [SerializeField] private AnimationCurve selectedCardLayoutEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Select Overlay")]
    [SerializeField] private RectTransform select;
    [SerializeField] private bool showSelectOverlay = false;

    //  Select 고정 크기
    [Header("Select Overlay Fixed Size")]
    [SerializeField] private bool useFixedSelectSize = true;
    [SerializeField] private Vector2 fixedSelectSize = new Vector2(113.2803f, 161.15f);

    private readonly List<GameObject> spawned = new();
    private readonly List<string> handIdsSnapshot = new();
    public IReadOnlyList<string> VisibleHandIds => handIdsSnapshot;

    private bool selecting = false;
    private bool readOnlySelectMode = false;
    private bool readOnlySelectEmphasisEnabled = true;
    private int selectIndex = -1;
    private int nonSelectedScaleAnimationIndex = -1;
    private Coroutine cardRiseRoutine;
    private Coroutine layoutRoutine;
    private Coroutine scaleRoutine;
    private readonly Dictionary<RectTransform, Vector2> cardRiseTargets = new();
    private bool cardsVisible = true;

    public event System.Action<bool> onSelectModeChanged;
    public event System.Action<int> onSelectIndexChanged;

    public bool IsInSelectMode => selecting;
    public bool IsReadOnlySelectMode => readOnlySelectMode;
    public bool IsReadOnlySelectEmphasisEnabled => readOnlySelectEmphasisEnabled;
    public int CurrentSelectIndex => selectIndex;
    public int CardCount => handIdsSnapshot.Count;
    public CardTemplateView CardTemplateSource =>
        cardPrefab != null ? cardPrefab.GetComponentInChildren<CardTemplateView>(true) : null;

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
        if (id.StartsWith("SupportCard")) return "SupportCard";
        return null;
    }

    void Awake()
    {
        if (!row) row = (RectTransform)transform;
        EnsureCardDatabase();
        HideCards();
        SetSelectOverlayVisible(false);
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

        StopCardRiseAnimation(true);
        StopLayoutAnimation(false);
        StopScaleAnimation(false);
    }

    public void RebuildFromHand()
    {
        if (!row) row = (RectTransform)transform;
        if (!cardPrefab) return;
        var rt = BattleDeckRuntime.Instance;
        if (rt == null) return;

        bool restoreSelectMode = selecting;
        bool restoreReadOnlySelectMode = readOnlySelectMode;
        bool restoreReadOnlySelectEmphasis = readOnlySelectEmphasisEnabled;
        int restoreSelectIndex = selectIndex;

        handIdsSnapshot.Clear();
        var live = rt.GetHandIds();
        if (live != null) handIdsSnapshot.AddRange(live);

        ClearSpawned();

        int n = handIdsSnapshot.Count;

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
            if (!templateView)
                templateView = go.AddComponent<CardTemplateView>();

            if (templateView)
                templateView.Bind(card, fallbackSprite);
            else
            {
                var img = go.GetComponentInChildren<Image>() ?? go.AddComponent<Image>();
                img.sprite = fallbackSprite;
                img.preserveAspect = true;
                img.raycastTarget = true;
            }

            var rootImage = go.GetComponent<Image>();
            if (rootImage)
            {
                rootImage.preserveAspect = true;
                rootImage.raycastTarget = true;
            }

            var rtItem = (RectTransform)go.transform;
            ConfigureCardRect(rtItem);
            rtItem.localScale = Vector3.one;
        }

        ApplyHandCardLayout(true);

        if (restoreSelectMode && n > 0)
        {
            selecting = true;
            readOnlySelectMode = restoreReadOnlySelectMode;
            readOnlySelectEmphasisEnabled = restoreReadOnlySelectEmphasis;
            SetSelectOverlayVisible(true);
            selectIndex = -1;
            SetSelectIndexPublic(Mathf.Clamp(restoreSelectIndex, 0, n - 1));
        }
        else
        {
            ExitSelectMode();
        }

        if (cardsVisible || selecting)
            ShowCards();
        else
            HideCards();
    }

    private void ClearSpawned()
    {
        StopLayoutAnimation(false);
        StopScaleAnimation(false);
        StopCardRiseAnimation(false);
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
    }

    public void ShowCards()
    {
        cardsVisible = true;
        gameObject.SetActive(true);
        RebuildFromRuntimeIfCardsMissing(false);
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) spawned[i].SetActive(true);
    }

    public void HideCards()
    {
        cardsVisible = false;
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) spawned[i].SetActive(false);
        SetSelectOverlayVisible(false);
        selecting = false;
        readOnlySelectMode = false;
        readOnlySelectEmphasisEnabled = true;
        selectIndex = -1;
        nonSelectedScaleAnimationIndex = -1;
        ApplyHandCardLayout(true);
    }

    // ---- 선택모드 공개 API ----
    public void EnterSelectMode()
    {
        EnterSelectMode(false, 0, true);
    }

    public void EnterReadOnlySelectMode(int startIndex = 0, bool emphasizeSelection = true)
    {
        EnterSelectMode(true, startIndex, emphasizeSelection);
    }

    private void EnterSelectMode(bool readOnly, int startIndex, bool emphasizeReadOnlySelection)
    {
        RebuildFromRuntimeIfCardsMissing(false);
        if (CardCount == 0) return;

        StopCardRiseAnimation(true);
        ShowCards();
        selecting = true;
        readOnlySelectMode = readOnly;
        readOnlySelectEmphasisEnabled = !readOnly || emphasizeReadOnlySelection;
        SetSelectOverlayVisible(true);
        onSelectModeChanged?.Invoke(true);

        SetSelectIndexPublic(Mathf.Clamp(startIndex, 0, CardCount - 1)); // 오른쪽 끝부터
    }

    public bool EnsureCardsReady(bool drawIfNeeded = false)
    {
        RebuildFromRuntimeIfCardsMissing(drawIfNeeded);
        return CardCount > 0;
    }

    private void RebuildFromRuntimeIfCardsMissing(bool drawIfNeeded)
    {
        var rt = BattleDeckRuntime.Instance;
        if (rt == null)
            return;

        if (drawIfNeeded && rt.HandCount <= 0)
            rt.DrawOneIfNeeded();

        var ids = rt.GetHandIds();
        if (ids == null || ids.Count == 0)
            return;

        if (spawned.Count > 0 && handIdsSnapshot.Count == ids.Count)
            return;

        RebuildFromHand();
    }

    public void ExitSelectMode()
    {
        ExitSelectMode(false);
    }

    public void ExitSelectMode(bool immediateLayout)
    {
        if (!selecting) return;
        int previousSelection = selectIndex;
        bool wasSelectEmphasisActive = IsSelectEmphasisActive();
        selecting = false;
        readOnlySelectMode = false;
        readOnlySelectEmphasisEnabled = true;
        onSelectModeChanged?.Invoke(false);
        selectIndex = -1;
        SetSelectOverlayVisible(false);
        nonSelectedScaleAnimationIndex = !immediateLayout && wasSelectEmphasisActive ? previousSelection : -1;
        ApplyHandCardLayout(immediateLayout);
        nonSelectedScaleAnimationIndex = -1;
    }

    public void SetReadOnlySelectEmphasis(bool enabled)
    {
        if (!selecting || !readOnlySelectMode)
        {
            readOnlySelectEmphasisEnabled = enabled;
            return;
        }

        if (readOnlySelectEmphasisEnabled == enabled)
            return;

        nonSelectedScaleAnimationIndex = enabled ? -1 : selectIndex;
        readOnlySelectEmphasisEnabled = enabled;
        ApplyHandCardLayout(false);
        nonSelectedScaleAnimationIndex = -1;
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
        int next = Mathf.Clamp(idx, 0, CardCount - 1);
        bool wasSelectEmphasisActive = IsSelectEmphasisActive();
        nonSelectedScaleAnimationIndex = wasSelectEmphasisActive && prev >= 0 && prev != next ? prev : -1;
        selectIndex = next;
        ApplyHandCardLayout(false);
        nonSelectedScaleAnimationIndex = -1;
        if (selectIndex != prev) onSelectIndexChanged?.Invoke(selectIndex);

        RefreshSelectOverlayPosition();
    }

    private void ApplyHandCardLayout(bool immediate)
    {
        ApplyHandCardLayout(immediate, false);
    }

    private void ApplyHandCardLayout(bool immediate, bool animateScaleFromConfiguredStart)
    {
        if (!row) row = (RectTransform)transform;

        int n = spawned.Count;
        if (n <= 0)
            return;

        var rects = new List<RectTransform>(n);
        var targetPositions = new List<Vector2>(n);
        var targetScales = new List<Vector3>(n);

        float[] displayWidths = new float[n];
        float totalCardsWidth = 0f;
        for (int i = 0; i < n; i++)
        {
            RectTransform rt = GetCardRect(i);
            if (!rt) continue;

            float scale = GetCardDisplayScale(i);
            ConfigureCardRect(rt);
            float width = rt.sizeDelta.x * scale;
            displayWidths[i] = width;
            totalCardsWidth += width;
        }

        float gap = ResolveLayoutGap(n, totalCardsWidth);
        float x = leftPadding;

        for (int i = 0; i < n; i++)
        {
            RectTransform rt = GetCardRect(i);
            if (!rt) continue;

            float scale = GetCardDisplayScale(i);
            ConfigureCardRect(rt);

            rects.Add(rt);
            targetPositions.Add(new Vector2(x, GetCardBottomBaselineY(rt)));
            targetScales.Add(new Vector3(scale, scale, 1f));

            x += displayWidths[i] + gap;
        }

        RectTransform selected = GetCardRect(selectIndex);
        if (selecting && selected)
            selected.SetAsLastSibling();

        if (immediate || !Application.isPlaying || !animateSelectedCardLayout)
        {
            StopLayoutAnimation(false);
            StopScaleAnimation(false);
            bool shouldAnimateScale = animateScaleFromConfiguredStart
                && Application.isPlaying
                && animateSelectedCardScale
                && animateSelectedCardLayout;

            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rt = rects[i];
                if (!rt) continue;
                rt.anchoredPosition = targetPositions[i];
                rt.localScale = shouldAnimateScale
                    ? GetAnimatedLayoutStartScale(rt, targetScales[i])
                    : targetScales[i];
            }

            RefreshSelectOverlayPosition();
            if (shouldAnimateScale)
                scaleRoutine = StartCoroutine(Co_AnimateHandCardScale(rects, targetScales));

            return;
        }

        if (layoutRoutine != null)
            StopCoroutine(layoutRoutine);
        StopScaleAnimation(false);

        layoutRoutine = StartCoroutine(Co_AnimateHandCardLayout(rects, targetPositions, targetScales));
    }

    private IEnumerator Co_AnimateHandCardLayout(List<RectTransform> rects, List<Vector2> targetPositions, List<Vector3> targetScales)
    {
        int count = rects.Count;
        var startPositions = new Vector2[count];
        var startScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = rects[i];
            startPositions[i] = rt ? rt.anchoredPosition : Vector2.zero;
            if (rt)
            {
                startScales[i] = GetAnimatedLayoutStartScale(rt, targetScales[i]);
                rt.localScale = startScales[i];
            }
            else
            {
                startScales[i] = Vector3.one;
            }
        }

        float duration = Mathf.Max(0.01f, selectedCardLayoutDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float eased = selectedCardLayoutEase != null ? selectedCardLayoutEase.Evaluate(u) : u;

            for (int i = 0; i < count; i++)
            {
                RectTransform rt = rects[i];
                if (!rt) continue;
                rt.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], targetPositions[i], eased);
                rt.localScale = Vector3.LerpUnclamped(startScales[i], targetScales[i], eased);
            }

            RefreshSelectOverlayPosition();
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = rects[i];
            if (!rt) continue;
            rt.anchoredPosition = targetPositions[i];
            rt.localScale = targetScales[i];
        }

        RefreshSelectOverlayPosition();
        layoutRoutine = null;
    }

    private IEnumerator Co_AnimateHandCardScale(List<RectTransform> rects, List<Vector3> targetScales)
    {
        int count = rects.Count;
        var startScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = rects[i];
            startScales[i] = rt ? rt.localScale : Vector3.one;
        }

        float duration = Mathf.Max(0.01f, selectedCardLayoutDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float eased = selectedCardLayoutEase != null ? selectedCardLayoutEase.Evaluate(u) : u;

            for (int i = 0; i < count; i++)
            {
                RectTransform rt = rects[i];
                if (!rt) continue;
                rt.localScale = Vector3.LerpUnclamped(startScales[i], targetScales[i], eased);
            }

            RefreshSelectOverlayPosition();
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = rects[i];
            if (!rt) continue;
            rt.localScale = targetScales[i];
        }

        RefreshSelectOverlayPosition();
        scaleRoutine = null;
    }

    private void StopLayoutAnimation(bool snapToCurrentTargets)
    {
        if (layoutRoutine == null)
            return;

        StopCoroutine(layoutRoutine);
        layoutRoutine = null;

        if (snapToCurrentTargets)
            ApplyHandCardLayout(true);
    }

    private void StopScaleAnimation(bool snapToCurrentTargets)
    {
        if (scaleRoutine == null)
            return;

        StopCoroutine(scaleRoutine);
        scaleRoutine = null;

        if (snapToCurrentTargets)
            ApplyHandCardLayout(true);
    }

    private float GetCardDisplayScale(int index)
    {
        if (!IsSelectEmphasisActive())
            return 1f;

        float selectedScale = Mathf.Max(1f, selectedCardScale);
        if (index == selectIndex)
            return selectedScale;

        return 1f;
    }

    private Vector3 GetAnimatedLayoutStartScale(RectTransform rt, Vector3 targetScale)
    {
        if (ShouldStartSelectedScaleFromConfiguredSize(rt, targetScale))
        {
            float start = GetSelectedScaleAnimationStart(targetScale.x);
            return new Vector3(start, start, 1f);
        }

        if (ShouldStartNonSelectedScaleFromConfiguredSize(rt, targetScale))
        {
            float start = GetNonSelectedScaleAnimationStart();
            return new Vector3(start, start, 1f);
        }

        return rt ? rt.localScale : Vector3.one;
    }

    private bool ShouldStartSelectedScaleFromConfiguredSize(RectTransform rt, Vector3 targetScale)
    {
        return animateSelectedCardScale
            && IsSelectEmphasisActive()
            && rt
            && rt == GetCardRect(selectIndex)
            && targetScale.x > 1f;
    }

    private bool ShouldStartNonSelectedScaleFromConfiguredSize(RectTransform rt, Vector3 targetScale)
    {
        return animateSelectedCardScale
            && emphasizeSelectedCard
            && rt
            && rt == GetCardRect(nonSelectedScaleAnimationIndex)
            && targetScale.x <= 1.0001f;
    }

    private bool IsSelectEmphasisActive()
    {
        return selecting
            && emphasizeSelectedCard
            && (!readOnlySelectMode || readOnlySelectEmphasisEnabled);
    }

    private float GetSelectedScaleAnimationStart(float targetScale)
    {
        return Mathf.Clamp(selectedCardScaleAnimationStart, 1f, Mathf.Max(1f, targetScale));
    }

    private float GetNonSelectedScaleAnimationStart()
    {
        return Mathf.Max(1f, nonSelectedCardScaleAnimationStart);
    }

    private float ResolveLayoutGap(int count, float totalCardsWidth)
    {
        if (count <= 1)
            return 0f;

        float baseStep = ResolveBaseStep(count);
        float baseGap = baseStep - cardWidth;

        if (!IsSelectEmphasisActive())
            return baseGap;

        float usable = GetUsableRowWidth();
        float maxGapInsideRow = (usable - totalCardsWidth) / (count - 1);
        if (maxGapInsideRow < baseGap)
            return Mathf.Max(0f, maxGapInsideRow);

        return Mathf.Max(0f, baseGap);
    }

    private float ResolveBaseStep(int count)
    {
        if (count <= 1)
            return 0f;

        float usable = GetUsableRowWidth();
        float maxSpan = Mathf.Max(0f, usable - cardWidth);
        float needed = maxSpan / (count - 1);
        return Mathf.Min(cardSpacing, Mathf.Max(0f, needed));
    }

    private float GetUsableRowWidth()
    {
        float rowW = row ? row.rect.width : 0f;
        return Mathf.Max(0f, rowW - leftPadding - rightPadding);
    }

    private void ConfigureCardRect(RectTransform rt)
    {
        if (!rt)
            return;

        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(cardWidth, ResolveCardHeight());
    }

    private float ResolveCardHeight()
    {
        return cardWidth * Mathf.Max(0.01f, cardHeightPerWidth);
    }

    private float GetCardBottomBaselineY(RectTransform rt)
    {
        if (!rt)
            return cardBottomYOffset;

        return cardBottomYOffset - rt.sizeDelta.y * 0.5f;
    }

    private void RefreshSelectOverlayPosition()
    {
        if (!showSelectOverlay)
        {
            SetSelectOverlayVisible(false);
            return;
        }

        if (select && selectIndex >= 0 && selectIndex < spawned.Count)
        {
            var target = (RectTransform)spawned[selectIndex].transform;
            float scale = Mathf.Max(0.0001f, target.localScale.x);
            float displayWidth = target.sizeDelta.x * scale;
            float displayHeight = target.sizeDelta.y * scale;

            // 부모/앵커 설정
            select.SetParent(row, false);

            //  선택 박스는 중앙 pivot 사용
            select.anchorMin = select.anchorMax = new Vector2(0f, 0.5f); // 행의 좌중앙 기준
            select.pivot = new Vector2(0.5f, 0.5f);

            // 카드 pivot(0,0) -> 표시 크기 기준 중앙 좌표
            float centerX = target.anchoredPosition.x + displayWidth * 0.5f;
            float centerY = target.anchoredPosition.y + displayHeight * 0.5f;
            select.anchoredPosition = new Vector2(centerX, centerY);

            //  크기 고정
            if (useFixedSelectSize)
            {
                select.sizeDelta = fixedSelectSize * scale;
            }
            else
            {
                select.sizeDelta = new Vector2(displayWidth, displayHeight);
            }

            select.localScale = Vector3.one;        // 스케일 흔적 제거
            select.SetAsLastSibling();              // 항상 맨 위로
        }
    }

    private void SetSelectOverlayVisible(bool visible)
    {
        if (!select)
            return;

        select.gameObject.SetActive(showSelectOverlay && visible);
    }

    public void PlayCardsRiseStaggered(float startYOffset, float perCardDuration, float perCardStagger, bool fadeAlpha)
    {
        if (!isActiveAndEnabled) return;

        StopCardRiseAnimation(true);
        ShowCards();
        ApplyHandCardLayout(true, true);

        var rects = GetAllCardRects();
        if (rects == null || rects.Count == 0) return;

        rects.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
        cardRiseRoutine = StartCoroutine(Co_PlayCardsRiseStaggered(rects, Mathf.Abs(startYOffset), perCardDuration, perCardStagger, fadeAlpha));
    }

    public void StopCardRiseAnimation(bool snapToTarget)
    {
        if (cardRiseRoutine != null)
        {
            StopCoroutine(cardRiseRoutine);
            cardRiseRoutine = null;
        }

        if (snapToTarget)
        {
            foreach (var pair in cardRiseTargets)
            {
                if (!pair.Key) continue;
                pair.Key.anchoredPosition = pair.Value;

                var cg = pair.Key.GetComponent<CanvasGroup>();
                if (cg) cg.alpha = 1f;
            }

            RefreshSelectOverlayPosition();
        }

        cardRiseTargets.Clear();
    }

    private IEnumerator Co_PlayCardsRiseStaggered(List<RectTransform> rects, float startYOffset, float duration, float stagger, bool fadeAlpha)
    {
        cardRiseTargets.Clear();

        for (int i = 0; i < rects.Count; i++)
        {
            RectTransform rt = rects[i];
            if (!rt) continue;

            Vector2 target = rt.anchoredPosition;
            cardRiseTargets[rt] = target;
            rt.anchoredPosition = target + new Vector2(0f, -startYOffset);

            if (fadeAlpha)
            {
                var cg = rt.GetComponent<CanvasGroup>();
                if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        RefreshSelectOverlayPosition();

        var running = new List<Coroutine>(rects.Count);
        for (int i = 0; i < rects.Count; i++)
        {
            RectTransform rt = rects[i];
            if (rt && cardRiseTargets.TryGetValue(rt, out Vector2 target))
                running.Add(StartCoroutine(Co_TweenCardRise(rt, target, duration, fadeAlpha)));

            if (stagger > 0f)
                yield return new WaitForSeconds(stagger);
        }

        for (int i = 0; i < running.Count; i++)
            if (running[i] != null) yield return running[i];

        cardRiseTargets.Clear();
        cardRiseRoutine = null;
    }

    private IEnumerator Co_TweenCardRise(RectTransform rt, Vector2 target, float duration, bool fadeAlpha)
    {
        if (!rt) yield break;

        Vector2 start = rt.anchoredPosition;
        CanvasGroup cg = fadeAlpha ? rt.GetComponent<CanvasGroup>() : null;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - u, 3f);

            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
            if (cg) cg.alpha = Mathf.LerpUnclamped(0f, 1f, eased);
            if (selecting) RefreshSelectOverlayPosition();

            yield return null;
        }

        rt.anchoredPosition = target;
        if (cg) cg.alpha = 1f;
        if (selecting) RefreshSelectOverlayPosition();
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
