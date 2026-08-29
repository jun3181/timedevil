// Assets/Script/Battle/EnemyHandUI.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class EnemyHandUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform row;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardDatabaseSO cardDatabase;
    [SerializeField] private string resourcesFolder = "my_asset";

    [Header("Layout")]
    [SerializeField] private float leftPadding = 8f;
    [SerializeField] private float rightPadding = 8f; //  추가

    [SerializeField] private float cardWidth = 120f;
    [SerializeField, Min(0.01f)] private float cardHeightPerWidth = 1.5f;
    [SerializeField] private float cardSpacing = 180f;
    [SerializeField] private float cardBottomYOffset = 0f;
    [SerializeField] private bool rightAlignCards = true;
    [SerializeField] private bool revealFromRight = true;

    [Header("Read Only Selection")]
    [SerializeField] private bool emphasizeSelectedCard = true;
    [SerializeField, Min(1f)] private float selectedCardScale = 1.75f;
    [SerializeField, Min(1f)] private float selectedCardScaleAnimationStart = 1.6f;
    [SerializeField, Min(1f)] private float nonSelectedCardScaleAnimationStart = 1.15f;
    [SerializeField] private bool animateSelectedCardLayout = true;
    [SerializeField, Min(0.01f)] private float selectedCardLayoutDuration = 0.18f;
    [SerializeField] private AnimationCurve selectedCardLayoutEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Reveal")]
    [SerializeField] private bool revealFaces = true;      // false면 뒷면만
    [SerializeField] private Sprite cardBackSprite;        // 뒷면 스프라이트

    private readonly List<GameObject> spawned = new();
    private readonly List<string> handIdsSnapshot = new();
    private Coroutine cardRiseRoutine;
    private Coroutine layoutRoutine;
    private readonly Dictionary<RectTransform, Vector2> cardRiseTargets = new();

    private bool selecting;
    private bool readOnlySelectEmphasisEnabled = true;
    private int selectIndex = -1;
    private int nonSelectedScaleAnimationIndex = -1;

    public IReadOnlyList<string> VisibleHandIds => handIdsSnapshot;
    public bool IsInSelectMode => selecting;
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

    private Sprite GetFaceSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        BaseCardSO card = GetCardById(id);
        if (card && card.mainArtwork) return card.mainArtwork;

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
        HideAll();
    }

    void OnEnable()
    {
        if (EnemyDeckRuntime.Instance != null)
            EnemyDeckRuntime.Instance.OnHandChanged += RebuildFromHand;

        RebuildFromHand();
    }

    void OnDisable()
    {
        if (EnemyDeckRuntime.Instance != null)
            EnemyDeckRuntime.Instance.OnHandChanged -= RebuildFromHand;

        StopCardRiseAnimation(true);
        StopLayoutAnimation(false);
    }

    public void RebuildFromHand()
    {
        if (!row) row = (RectTransform)transform;
        if (!cardPrefab) return;

        var rt = EnemyDeckRuntime.Instance;
        if (rt == null) { HideAll(); return; }

        var ids = rt.GetHandIds();
        bool restoreSelectMode = selecting;
        bool restoreReadOnlySelectEmphasis = readOnlySelectEmphasisEnabled;
        int restoreSelectIndex = selectIndex;

        handIdsSnapshot.Clear();
        if (ids != null) handIdsSnapshot.AddRange(ids);

        ClearSpawned();

        int n = handIdsSnapshot.Count;

        ClearSpawned();
        for (int i = 0; i < n; i++)
        {
            string id = handIdsSnapshot[i];
            var go = Instantiate(cardPrefab, row);
            go.name = $"EnemyHand_{(string.IsNullOrEmpty(id) ? "NULL" : id)}";
            spawned.Add(go);

            BaseCardSO card = revealFaces ? GetCardById(id) : null;
            Sprite faceSprite = revealFaces ? GetFaceSprite(id) : null;

            var templateView = go.GetComponentInChildren<CardTemplateView>(true);
            if (!templateView)
                templateView = go.AddComponent<CardTemplateView>();

            if (templateView)
                templateView.Bind(revealFaces ? card : null, revealFaces ? faceSprite : cardBackSprite);
            else
            {
                var img = go.GetComponentInChildren<Image>() ?? go.AddComponent<Image>();
                img.sprite = revealFaces ? faceSprite : cardBackSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            var rootImage = go.GetComponent<Image>();
            if (rootImage)
                rootImage.raycastTarget = false;

            var rtItem = (RectTransform)go.transform;
            ConfigureCardRect(rtItem);
            rtItem.localScale = Vector3.one;
        }

        selecting = restoreSelectMode && n > 0;
        readOnlySelectEmphasisEnabled = restoreReadOnlySelectEmphasis;
        selectIndex = -1;
        if (selecting)
            SetSelectIndexPublic(Mathf.Clamp(restoreSelectIndex, 0, n - 1));
        else
            ApplyHandCardLayout(true);

        ShowAll();
    }

    private void ClearSpawned()
    {
        StopCardRiseAnimation(false);
        StopLayoutAnimation(false);
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
    }

    public void ShowAll()
    {
        var rt = EnemyDeckRuntime.Instance;
        if (spawned.Count == 0 && rt != null && rt.GetHandIds().Count > 0)
            RebuildFromHand();

        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) spawned[i].SetActive(true);
        gameObject.SetActive(true);
    }

    public void HideAll()
    {
        if (selecting)
        {
            selecting = false;
            selectIndex = -1;
            nonSelectedScaleAnimationIndex = -1;
            ApplyHandCardLayout(true);
        }
        readOnlySelectEmphasisEnabled = true;
        StopLayoutAnimation(false);
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) spawned[i].SetActive(false);
        gameObject.SetActive(false);
    }

    public void EnterReadOnlySelectMode(int startIndex = -1, bool emphasizeSelection = true)
    {
        if (CardCount == 0) return;

        StopCardRiseAnimation(true);
        ShowAll();
        selecting = false;
        ApplyHandCardLayout(true);
        selecting = true;
        readOnlySelectEmphasisEnabled = emphasizeSelection;
        int resolvedStartIndex = startIndex >= 0
            ? startIndex
            : (rightAlignCards ? CardCount - 1 : 0);
        SetSelectIndexPublic(Mathf.Clamp(resolvedStartIndex, 0, CardCount - 1));
    }

    public void ExitSelectMode()
    {
        if (!selecting) return;

        int previousSelection = selectIndex;
        bool wasSelectEmphasisActive = IsSelectEmphasisActive();
        selecting = false;
        readOnlySelectEmphasisEnabled = true;
        selectIndex = -1;
        nonSelectedScaleAnimationIndex = wasSelectEmphasisActive ? previousSelection : -1;
        ApplyHandCardLayout(false);
        nonSelectedScaleAnimationIndex = -1;
    }

    public void SetReadOnlySelectEmphasis(bool enabled)
    {
        if (!selecting)
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
        next = (next % CardCount + CardCount) % CardCount;
        SetSelectIndexPublic(next);
    }

    public void SetSelectIndexPublic(int index)
    {
        if (CardCount == 0) return;

        int prev = selectIndex;
        int next = Mathf.Clamp(index, 0, CardCount - 1);
        bool wasSelectEmphasisActive = IsSelectEmphasisActive();
        nonSelectedScaleAnimationIndex = wasSelectEmphasisActive && prev >= 0 && prev != next ? prev : -1;
        selectIndex = next;
        ApplyHandCardLayout(false);
        nonSelectedScaleAnimationIndex = -1;
    }

    private void ApplyHandCardLayout(bool immediate)
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

            ConfigureCardRect(rt);
            float scale = GetCardDisplayScale(i);
            displayWidths[i] = rt.sizeDelta.x * scale;
            totalCardsWidth += displayWidths[i];
        }

        float gap = ResolveLayoutGap(n, totalCardsWidth);
        float totalWidth = totalCardsWidth + gap * Mathf.Max(0, n - 1);
        float x = rightAlignCards
            ? Mathf.Max(leftPadding, row.rect.width - rightPadding - totalWidth)
            : leftPadding;

        for (int i = 0; i < n; i++)
        {
            RectTransform rt = GetCardRect(i);
            if (!rt) continue;

            float scale = GetCardDisplayScale(i);
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
            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rt = rects[i];
                if (!rt) continue;
                rt.anchoredPosition = targetPositions[i];
                rt.localScale = targetScales[i];
            }
            return;
        }

        if (layoutRoutine != null)
            StopCoroutine(layoutRoutine);

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
            startScales[i] = rt ? GetAnimatedLayoutStartScale(rt, targetScales[i]) : Vector3.one;
            if (rt) rt.localScale = startScales[i];
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

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = rects[i];
            if (!rt) continue;
            rt.anchoredPosition = targetPositions[i];
            rt.localScale = targetScales[i];
        }

        layoutRoutine = null;
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

    private float GetCardDisplayScale(int index)
    {
        return selecting
            && IsSelectEmphasisActive()
            && index == selectIndex
            ? Mathf.Max(1f, selectedCardScale)
            : 1f;
    }

    private Vector3 GetAnimatedLayoutStartScale(RectTransform rt, Vector3 targetScale)
    {
        if (!rt)
            return rt ? rt.localScale : Vector3.one;

        if (selecting
            && IsSelectEmphasisActive()
            && rt == GetCardRect(selectIndex)
            && targetScale.x > 1f)
        {
            float start = Mathf.Clamp(selectedCardScaleAnimationStart, 1f, Mathf.Max(1f, targetScale.x));
            return new Vector3(start, start, 1f);
        }

        if (emphasizeSelectedCard
            && rt == GetCardRect(nonSelectedScaleAnimationIndex)
            && targetScale.x <= 1.0001f)
        {
            float start = Mathf.Max(1f, nonSelectedCardScaleAnimationStart);
            return new Vector3(start, start, 1f);
        }

        return rt.localScale;
    }

    private bool IsSelectEmphasisActive()
    {
        return selecting
            && emphasizeSelectedCard
            && readOnlySelectEmphasisEnabled;
    }

    private float ResolveLayoutGap(int count, float totalCardsWidth)
    {
        if (count <= 1)
            return 0f;

        float baseStep = ResolveBaseStep(count);
        float baseGap = baseStep - cardWidth;
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
        if (!rt) return;

        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(cardWidth, ResolveCardHeight(rt));
    }

    private float ResolveCardHeight(RectTransform rt)
    {
        float height = cardWidth * Mathf.Max(0.01f, cardHeightPerWidth);
        if (height > 0f)
            return height;

        return rt ? Mathf.Max(1f, rt.sizeDelta.y) : 1f;
    }

    private float GetCardBottomBaselineY(RectTransform rt)
    {
        if (!rt)
            return cardBottomYOffset;

        return cardBottomYOffset - rt.sizeDelta.y * 0.5f;
    }

    public void PlayCardsRiseStaggered(float startYOffset, float perCardDuration, float perCardStagger, bool fadeAlpha)
    {
        if (!isActiveAndEnabled) return;

        StopCardRiseAnimation(true);
        ShowAll();

        var rects = GetAllCardRects();
        if (rects == null || rects.Count == 0) return;

        SortRectsForReveal(rects);
        cardRiseRoutine = StartCoroutine(Co_PlayCardsRiseStaggered(rects, Mathf.Abs(startYOffset), perCardDuration, perCardStagger, fadeAlpha));
    }

    public IEnumerator PlayCardsRiseStaggeredAndWait(float startYOffset, float perCardDuration, float perCardStagger, bool fadeAlpha)
    {
        if (!isActiveAndEnabled) yield break;

        StopCardRiseAnimation(true);
        ShowAll();

        var rects = GetAllCardRects();
        if (rects == null || rects.Count == 0) yield break;

        SortRectsForReveal(rects);
        cardRiseRoutine = StartCoroutine(Co_PlayCardsRiseStaggered(rects, Mathf.Abs(startYOffset), perCardDuration, perCardStagger, fadeAlpha));
        yield return cardRiseRoutine;
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
        }

        cardRiseTargets.Clear();
    }

    private IEnumerator Co_PlayCardsRiseStaggered(List<RectTransform> rects, float startYOffset, float duration, float stagger, bool fadeAlpha)
    {
        cardRiseTargets.Clear();
        Canvas.ForceUpdateCanvases();

        int count = rects.Count;
        var starts = new Vector2[count];
        var targets = new Vector2[count];
        var groups = new CanvasGroup[count];

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = rects[i];
            if (!rt) continue;

            Vector2 target = rt.anchoredPosition;
            Vector2 start = target + new Vector2(0f, -startYOffset);

            starts[i] = start;
            targets[i] = target;
            cardRiseTargets[rt] = target;
            rt.anchoredPosition = start;

            if (fadeAlpha)
            {
                var cg = rt.GetComponent<CanvasGroup>();
                if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                groups[i] = cg;
            }
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        float safeStagger = Mathf.Max(0f, stagger);
        float totalDuration = safeDuration + safeStagger * Mathf.Max(0, count - 1);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < count; i++)
            {
                RectTransform rt = rects[i];
                if (!rt) continue;

                float localTime = elapsed - safeStagger * i;
                if (localTime < 0f) continue;

                float u = Mathf.Clamp01(localTime / safeDuration);
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                rt.anchoredPosition = Vector2.LerpUnclamped(starts[i], targets[i], eased);

                CanvasGroup cg = groups[i];
                if (cg) cg.alpha = Mathf.LerpUnclamped(0f, 1f, eased);
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = rects[i];
            if (!rt) continue;

            rt.anchoredPosition = targets[i];
            CanvasGroup cg = groups[i];
            if (cg) cg.alpha = 1f;
        }

        cardRiseTargets.Clear();
        cardRiseRoutine = null;
    }

    private void SortRectsForReveal(List<RectTransform> rects)
    {
        if (revealFromRight)
            rects.Sort((a, b) => b.anchoredPosition.x.CompareTo(a.anchoredPosition.x));
        else
            rects.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
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

            yield return null;
        }

        rt.anchoredPosition = target;
        if (cg) cg.alpha = 1f;
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
