using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMenuPanelCarouselController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private PanelController legacyPanelController;
    [SerializeField] private HandUI handUI;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private CardUseOrchestrator orchestrator;
    [SerializeField] private bool disableLegacyPanelControllerOnAwake = true;
    [SerializeField] private bool restoreLegacyPanelControllerOnDisable = true;

    [Header("Panels")]
    [SerializeField] private bool autoPopulateFromMenuEntries = true;
    [SerializeField] private List<RectTransform> panels = new List<RectTransform>();
    [SerializeField] private bool restorePanelControllerLayoutOnDisable = true;

    [Header("Layout")]
    [SerializeField] private int defaultSelectedIndex = 0;
    [SerializeField] private bool forceDefaultSelectionOnStart = true;
    [SerializeField] private bool useDefaultPanelPositionAsSelected = true;
    [SerializeField] private Vector2 selectedAnchoredPosition = new Vector2(-736.0194f, -457f);
    [SerializeField] private float peekingAnchoredY = -545f;
    [SerializeField] private bool autoMeasureSpacing = true;
    [SerializeField, Min(1f)] private float panelSpacing = 374f;

    [Header("Scale")]
    [SerializeField] private bool controlPanelScale = true;
    [SerializeField, Min(0.01f)] private float panelScaleMultiplier = 1.12f;
    [SerializeField, Min(0.01f)] private float selectedPanelScaleMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float peekingPanelScaleMultiplier = 1f;
    [SerializeField] private bool scaleSpacingWithPanelScale = true;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float animationDuration = 0.28f;
    [SerializeField] private AnimationCurve carouselEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool animateWrapContinuity = true;
    [SerializeField] private bool useWrapGhost = true;

    [Header("Hand Object Rise")]
    [SerializeField] private bool animateHandObjectOnCarouselControl = true;
    [SerializeField] private RectTransform handObject;
    [SerializeField] private bool autoResolveHandObject = true;
    [SerializeField] private bool convertHandPositionFromSeparatedRoot = true;
    [SerializeField] private string separatedHandRootName = "hand01";
    [SerializeField] private Vector2 handHiddenAnchoredPosition = new Vector2(-315.6795f, -820f);
    [SerializeField] private Vector2 handShownAnchoredPosition = new Vector2(-315.6795f, -380f);
    [SerializeField] private bool startHandBelowScreen = true;
    [SerializeField, Min(0.01f)] private float handRiseDuration = 0.3f;
    [SerializeField] private AnimationCurve handRiseEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool staggerCardsOnHandRise = true;
    [SerializeField] private float handCardRiseYOffset = 460f;
    [SerializeField, Min(0.01f)] private float handCardRiseDuration = 0.22f;
    [SerializeField] private float handCardRiseStagger = 0.055f;
    [SerializeField] private bool handCardRiseFade = true;

    private readonly List<Vector3> panelBaseScales = new List<Vector3>();
    private readonly List<Vector2> panelControllerAnchoredPositions = new List<Vector2>();
    private readonly List<Vector3> panelControllerScales = new List<Vector3>();
    private int selectedIndex = -1;
    private Coroutine running;
    private Coroutine handObjectRunning;
    private RectTransform activeWrapGhost;
    private bool handObjectShown;
    private bool defaultsCaptured;
    private bool panelControllerLayoutCaptured;
    private bool disabledLegacyPanelController;
    private bool legacyPanelControllerWasEnabled;
    private bool carouselInitialized;

    void Reset()
    {
        ResolveRefs();
        PopulatePanelsFromMenu();
        CapturePanelControllerLayout();
        CaptureLayoutDefaults();
        ResolveHandObject();
    }

    void Awake()
    {
        ResolveRefs();
        if (!enabled)
        {
            EnableLegacyPanelControllerForDisabledCarousel();
            return;
        }

        InitializeCarousel();
    }

    void OnEnable()
    {
        ResolveRefs();
        InitializeCarousel();
        if (menu) menu.onFocusChanged.AddListener(HandleMenuFocusChanged);
    }

    void Start()
    {
        InitializeCarousel();
        PopulatePanelsFromMenu();
        CapturePanelControllerLayout();
        CaptureLayoutDefaults();

        int startIndex = GetStartIndex();
        if (forceDefaultSelectionOnStart && menu)
            menu.SetFocus(startIndex);

        SetSelected(startIndex, true);
        RefreshHandObject(true);
    }

    void OnDisable()
    {
        if (menu) menu.onFocusChanged.RemoveListener(HandleMenuFocusChanged);
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        if (handObjectRunning != null)
        {
            StopCoroutine(handObjectRunning);
            handObjectRunning = null;
        }

        DestroyActiveWrapGhost();
        RestorePanelControllerLayout();
        RestoreLegacyPanelController();
        carouselInitialized = false;
    }

    void Update()
    {
        RefreshHandObject(false);
    }

    private void HandleMenuFocusChanged(int index)
    {
        SetSelected(index, false);
    }

    private void ResolveRefs()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!legacyPanelController) legacyPanelController = FindObjectOfType<PanelController>(true);
        if (!handUI) handUI = FindObjectOfType<HandUI>(true);
        if (!turnManager) turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (!orchestrator) orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
    }

    private void ResolveHandObject()
    {
        if (!handUI) handUI = FindObjectOfType<HandUI>(true);

        if (handObject && autoResolveHandObject && handUI
            && HandPositionUtility.IsNamedRoot(handObject.transform, separatedHandRootName))
        {
            RectTransform handRect = handUI.GetComponent<RectTransform>();
            if (handRect && handRect.transform.IsChildOf(handObject))
                handObject = handRect;
        }

        if (handObject || !autoResolveHandObject) return;
        if (handUI) handObject = handUI.GetComponent<RectTransform>();
    }

    private void InitializeCarousel()
    {
        if (carouselInitialized) return;

        ResolveRefs();
        ResolveHandObject();

        legacyPanelControllerWasEnabled = legacyPanelController && legacyPanelController.enabled;
        if (disableLegacyPanelControllerOnAwake && legacyPanelController)
        {
            legacyPanelController.enabled = false;
            disabledLegacyPanelController = true;
        }

        PopulatePanelsFromMenu();
        CapturePanelControllerLayout();
        CaptureLayoutDefaults();
        ApplyImmediate(GetStartIndex());
        SetHandObjectShown(startHandBelowScreen ? false : ShouldShowHandObject(), true);

        carouselInitialized = true;
    }

    private void EnableLegacyPanelControllerForDisabledCarousel()
    {
        if (!disableLegacyPanelControllerOnAwake || !legacyPanelController) return;

        legacyPanelController.enabled = true;
        disabledLegacyPanelController = false;
        legacyPanelControllerWasEnabled = true;
    }

    private void RefreshHandObject(bool immediate)
    {
        if (!animateHandObjectOnCarouselControl) return;
        if (!handUI || !turnManager || !orchestrator)
            ResolveRefs();

        bool shouldShow = ShouldShowHandObject();
        if (shouldShow != handObjectShown || immediate)
            SetHandObjectShown(shouldShow, immediate);
    }

    private bool ShouldShowHandObject()
    {
        bool handSelecting = handUI && handUI.IsInSelectMode;
        bool cardResolving = orchestrator && orchestrator.GetIsBusy();
        bool enemyTurn = turnManager && turnManager.currentTurn == TurnState.EnemyTurn;

        return animateHandObjectOnCarouselControl
            && handObject != null
            && !enemyTurn
            && (handSelecting || cardResolving);
    }

    private void SetHandObjectShown(bool shown, bool immediate = false)
    {
        if (!animateHandObjectOnCarouselControl) return;
        ResolveHandObject();
        if (!handObject) return;

        if (handObjectRunning != null)
        {
            StopCoroutine(handObjectRunning);
            handObjectRunning = null;
        }

        handObjectShown = shown;
        PrepareHandCardsForRise(shown, immediate);

        if (immediate) ApplyHandObjectImmediate(shown);
        else handObjectRunning = StartCoroutine(Co_AnimateHandObject(shown));
    }

    private void PrepareHandCardsForRise(bool shown, bool immediate)
    {
        if (!handUI || !staggerCardsOnHandRise) return;

        if (shown && !immediate)
            handUI.PlayCardsRiseStaggered(handCardRiseYOffset, handCardRiseDuration, handCardRiseStagger, handCardRiseFade);
        else if (!shown)
            handUI.StopCardRiseAnimation(true);
    }

    private IEnumerator Co_AnimateHandObject(bool shown)
    {
        Vector2 from = handObject.anchoredPosition;
        Vector2 to = GetHandObjectTargetPosition(shown);

        float t = 0f;
        while (t < handRiseDuration)
        {
            t += Time.deltaTime;
            float k = handRiseDuration <= 0f ? 1f : Mathf.Clamp01(t / handRiseDuration);
            float eased = handRiseEase != null ? handRiseEase.Evaluate(k) : k;
            handObject.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        handObject.anchoredPosition = to;
        handObjectRunning = null;
    }

    private void ApplyHandObjectImmediate(bool shown)
    {
        if (!handObject) return;
        handObject.anchoredPosition = GetHandObjectTargetPosition(shown);
    }

    private Vector2 GetHandObjectTargetPosition(bool shown)
    {
        Vector2 target = shown ? handShownAnchoredPosition : handHiddenAnchoredPosition;
        return HandPositionUtility.ToSeparatedRootLocal(
            handObject,
            target,
            convertHandPositionFromSeparatedRoot,
            separatedHandRootName);
    }

    private void PopulatePanelsFromMenu()
    {
        if (!autoPopulateFromMenuEntries || panels.Count > 0 || !menu) return;

        panels.Clear();
        for (int i = 0; i < menu.EntryCount; i++)
        {
            RectTransform rect = menu.GetEntryRectTransform(i);
            if (rect) panels.Add(rect);
        }
    }

    private void CapturePanelControllerLayout()
    {
        if (panelControllerLayoutCaptured || panels.Count == 0) return;

        panelControllerAnchoredPositions.Clear();
        panelControllerScales.Clear();
        for (int i = 0; i < panels.Count; i++)
        {
            RectTransform panel = panels[i];
            panelControllerAnchoredPositions.Add(panel ? panel.anchoredPosition : Vector2.zero);
            panelControllerScales.Add(panel ? panel.localScale : Vector3.one);
        }

        panelControllerLayoutCaptured = true;
    }

    private void RestorePanelControllerLayout()
    {
        if (!restorePanelControllerLayoutOnDisable || !panelControllerLayoutCaptured) return;

        int count = Mathf.Min(panels.Count, panelControllerAnchoredPositions.Count);
        for (int i = 0; i < count; i++)
        {
            RectTransform panel = panels[i];
            if (!panel) continue;

            panel.anchoredPosition = panelControllerAnchoredPositions[i];
            if (i < panelControllerScales.Count)
                panel.localScale = panelControllerScales[i];
        }

        selectedIndex = -1;
    }

    private void RestoreLegacyPanelController()
    {
        if (!restoreLegacyPanelControllerOnDisable) return;
        if (!disabledLegacyPanelController || !legacyPanelController) return;

        if (legacyPanelControllerWasEnabled)
            legacyPanelController.enabled = true;

        disabledLegacyPanelController = false;
    }

    private void CaptureLayoutDefaults()
    {
        if (defaultsCaptured || panels.Count == 0) return;

        int referenceIndex = NormalizeIndex(defaultSelectedIndex);
        RectTransform referencePanel = panels[referenceIndex];
        if (useDefaultPanelPositionAsSelected && referencePanel)
            selectedAnchoredPosition = referencePanel.anchoredPosition;

        if (autoMeasureSpacing && panels.Count > 1)
        {
            float total = 0f;
            int pairCount = 0;
            for (int i = 1; i < panels.Count; i++)
            {
                if (!panels[i - 1] || !panels[i]) continue;

                float distance = Mathf.Abs(panels[i].anchoredPosition.x - panels[i - 1].anchoredPosition.x);
                if (distance <= 0.01f) continue;

                total += distance;
                pairCount++;
            }

            if (pairCount > 0)
                panelSpacing = total / pairCount;
        }

        CachePanelBaseScales();
        defaultsCaptured = true;
    }

    private void CachePanelBaseScales()
    {
        panelBaseScales.Clear();
        for (int i = 0; i < panels.Count; i++)
            panelBaseScales.Add(panels[i] ? panels[i].localScale : Vector3.one);
    }

    private int GetStartIndex()
    {
        if (panels.Count == 0) return 0;
        int rawIndex = forceDefaultSelectionOnStart || !menu ? defaultSelectedIndex : menu.Index;
        return NormalizeIndex(rawIndex);
    }

    private void SetSelected(int index, bool immediate)
    {
        PopulatePanelsFromMenu();
        CaptureLayoutDefaults();
        if (panels.Count == 0) return;

        int nextIndex = NormalizeIndex(index);
        if (immediate || selectedIndex < 0)
        {
            ApplyImmediate(nextIndex);
            return;
        }

        if (nextIndex == selectedIndex) return;

        int previousIndex = selectedIndex;
        selectedIndex = nextIndex;

        if (running != null)
            StopCoroutine(running);
        DestroyActiveWrapGhost();

        running = StartCoroutine(Co_AnimateSelection(previousIndex, nextIndex));
    }

    private IEnumerator Co_AnimateSelection(int previousIndex, int nextIndex)
    {
        List<Vector2> from = SnapshotCurrent();
        List<Vector2> visibleTargets = BuildTargets(nextIndex);
        List<Vector2> finalTargets = new List<Vector2>(visibleTargets);
        List<Vector3> fromScales = SnapshotScales();
        List<Vector3> visibleTargetScales = BuildTargetScales(nextIndex);
        List<Vector3> finalTargetScales = new List<Vector3>(visibleTargetScales);
        RectTransform wrapGhost = null;
        Vector2 ghostFrom = Vector2.zero;
        Vector2 ghostTo = Vector2.zero;
        Vector3 ghostScaleFrom = Vector3.one;
        Vector3 ghostScaleTo = Vector3.one;

        if (animateWrapContinuity)
        {
            AdjustWrapTravel(
                previousIndex,
                nextIndex,
                from,
                visibleTargets,
                fromScales,
                visibleTargetScales,
                out wrapGhost,
                out ghostFrom,
                out ghostTo,
                out ghostScaleFrom,
                out ghostScaleTo);
        }

        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.deltaTime;
            float k = animationDuration <= 0f ? 1f : Mathf.Clamp01(t / animationDuration);
            float eased = EvaluateEase(k);
            ApplyPositions(from, visibleTargets, eased);
            ApplyScales(fromScales, visibleTargetScales, eased);
            if (wrapGhost)
            {
                wrapGhost.anchoredPosition = Vector2.LerpUnclamped(ghostFrom, ghostTo, eased);
                wrapGhost.localScale = Vector3.LerpUnclamped(ghostScaleFrom, ghostScaleTo, eased);
            }
            yield return null;
        }

        ApplyPositions(finalTargets);
        ApplyScales(finalTargetScales);
        DestroyActiveWrapGhost();
        running = null;
    }

    private void AdjustWrapTravel(
        int previousIndex,
        int nextIndex,
        List<Vector2> from,
        List<Vector2> visibleTargets,
        List<Vector3> fromScales,
        List<Vector3> visibleTargetScales,
        out RectTransform wrapGhost,
        out Vector2 ghostFrom,
        out Vector2 ghostTo,
        out Vector3 ghostScaleFrom,
        out Vector3 ghostScaleTo)
    {
        wrapGhost = null;
        ghostFrom = Vector2.zero;
        ghostTo = Vector2.zero;
        ghostScaleFrom = Vector3.one;
        ghostScaleTo = Vector3.one;

        int direction = GetAdjacentDirection(previousIndex, nextIndex);
        if (direction == 0) return;

        int count = panels.Count;
        float spacing = GetEffectivePanelSpacing();
        float leftWrapX = selectedAnchoredPosition.x - spacing;
        float rightWrapX = selectedAnchoredPosition.x + (spacing * count);

        for (int i = 0; i < count; i++)
        {
            int previousSlot = GetSlot(i, previousIndex);
            int nextSlot = GetSlot(i, nextIndex);

            if (direction > 0 && previousSlot == 0 && nextSlot == count - 1)
            {
                if (useWrapGhost)
                {
                    wrapGhost = CreateWrapGhost(panels[i]);
                    ghostFrom = from[i];
                    ghostTo = new Vector2(leftWrapX, peekingAnchoredY);
                    ghostScaleFrom = fromScales[i];
                    ghostScaleTo = visibleTargetScales[i];

                    Vector2 start = from[i];
                    start.x = rightWrapX;
                    start.y = peekingAnchoredY;
                    from[i] = start;

                    fromScales[i] = visibleTargetScales[i];

                    if (panels[i])
                    {
                        panels[i].anchoredPosition = start;
                        panels[i].localScale = fromScales[i];
                    }
                }
                else
                {
                    Vector2 target = visibleTargets[i];
                    target.x = leftWrapX;
                    visibleTargets[i] = target;
                }

                return;
            }
            else if (direction < 0 && previousSlot == count - 1 && nextSlot == 0)
            {
                if (useWrapGhost)
                {
                    wrapGhost = CreateWrapGhost(panels[i]);
                    ghostFrom = from[i];
                    ghostTo = new Vector2(rightWrapX, peekingAnchoredY);
                    ghostScaleFrom = fromScales[i];
                    ghostScaleTo = GetTargetScale(i, previousIndex, false);
                }

                Vector2 start = from[i];
                start.x = leftWrapX;
                start.y = peekingAnchoredY;
                from[i] = start;
                fromScales[i] = GetTargetScale(i, previousIndex, false);

                if (panels[i])
                {
                    panels[i].anchoredPosition = start;
                    panels[i].localScale = fromScales[i];
                }

                return;
            }
        }
    }

    private RectTransform CreateWrapGhost(RectTransform source)
    {
        if (!source) return null;

        DestroyActiveWrapGhost();

        RectTransform ghost = Instantiate(source, source.parent);
        ghost.name = source.name + "_CarouselGhost";
        ghost.SetAsLastSibling();

        CanvasGroup canvasGroup = ghost.GetComponent<CanvasGroup>();
        if (!canvasGroup)
            canvasGroup = ghost.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        activeWrapGhost = ghost;
        return ghost;
    }

    private void DestroyActiveWrapGhost()
    {
        if (!activeWrapGhost) return;

        Destroy(activeWrapGhost.gameObject);
        activeWrapGhost = null;
    }

    private int GetAdjacentDirection(int previousIndex, int nextIndex)
    {
        int count = panels.Count;
        if (count <= 1) return 0;
        if ((previousIndex + 1) % count == nextIndex) return 1;
        if ((previousIndex - 1 + count) % count == nextIndex) return -1;
        return 0;
    }

    private void ApplyImmediate(int index)
    {
        selectedIndex = NormalizeIndex(index);
        ApplyPositions(BuildTargets(selectedIndex));
        ApplyScales(BuildTargetScales(selectedIndex));
    }

    private List<Vector2> BuildTargets(int focusIndex)
    {
        var targets = new List<Vector2>(panels.Count);
        for (int i = 0; i < panels.Count; i++)
        {
            int slot = GetSlot(i, focusIndex);
            float x = selectedAnchoredPosition.x + (GetEffectivePanelSpacing() * slot);
            float y = i == focusIndex ? selectedAnchoredPosition.y : peekingAnchoredY;
            targets.Add(new Vector2(x, y));
        }

        return targets;
    }

    private List<Vector3> BuildTargetScales(int focusIndex)
    {
        var scales = new List<Vector3>(panels.Count);
        for (int i = 0; i < panels.Count; i++)
            scales.Add(GetTargetScale(i, focusIndex, i == focusIndex));

        return scales;
    }

    private Vector3 GetTargetScale(int panelIndex, int focusIndex, bool selected)
    {
        Vector3 baseScale = panelIndex < panelBaseScales.Count ? panelBaseScales[panelIndex] : Vector3.one;
        if (!controlPanelScale) return baseScale;

        float selectionScale = selected ? selectedPanelScaleMultiplier : peekingPanelScaleMultiplier;
        return baseScale * panelScaleMultiplier * selectionScale;
    }

    private float GetEffectivePanelSpacing()
    {
        if (!controlPanelScale || !scaleSpacingWithPanelScale) return panelSpacing;
        float maxScale = Mathf.Max(selectedPanelScaleMultiplier, peekingPanelScaleMultiplier) * panelScaleMultiplier;
        return panelSpacing * maxScale;
    }

    private int GetSlot(int panelIndex, int focusIndex)
    {
        int count = panels.Count;
        if (count <= 0) return 0;
        return (panelIndex - focusIndex + count) % count;
    }

    private List<Vector2> SnapshotCurrent()
    {
        var list = new List<Vector2>(panels.Count);
        for (int i = 0; i < panels.Count; i++)
            list.Add(panels[i] ? panels[i].anchoredPosition : Vector2.zero);
        return list;
    }

    private List<Vector3> SnapshotScales()
    {
        var list = new List<Vector3>(panels.Count);
        for (int i = 0; i < panels.Count; i++)
            list.Add(panels[i] ? panels[i].localScale : Vector3.one);
        return list;
    }

    private void ApplyPositions(List<Vector2> positions)
    {
        int count = Mathf.Min(panels.Count, positions.Count);
        for (int i = 0; i < count; i++)
        {
            if (!panels[i]) continue;
            panels[i].anchoredPosition = positions[i];
        }
    }

    private void ApplyPositions(List<Vector2> from, List<Vector2> to, float t)
    {
        int count = Mathf.Min(panels.Count, Mathf.Min(from.Count, to.Count));
        for (int i = 0; i < count; i++)
        {
            if (!panels[i]) continue;
            panels[i].anchoredPosition = Vector2.LerpUnclamped(from[i], to[i], t);
        }
    }

    private void ApplyScales(List<Vector3> scales)
    {
        int count = Mathf.Min(panels.Count, scales.Count);
        for (int i = 0; i < count; i++)
        {
            if (!panels[i]) continue;
            panels[i].localScale = scales[i];
        }
    }

    private void ApplyScales(List<Vector3> from, List<Vector3> to, float t)
    {
        int count = Mathf.Min(panels.Count, Mathf.Min(from.Count, to.Count));
        for (int i = 0; i < count; i++)
        {
            if (!panels[i]) continue;
            panels[i].localScale = Vector3.LerpUnclamped(from[i], to[i], t);
        }
    }

    private int NormalizeIndex(int index)
    {
        int count = panels.Count;
        if (count <= 0) return 0;
        return (index % count + count) % count;
    }

    private float EvaluateEase(float t)
    {
        return carouselEase != null ? carouselEase.Evaluate(t) : t;
    }
}
