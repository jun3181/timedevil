using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class StatePanelController : MonoBehaviour
{
    [System.Serializable]
    private class StateTarget
    {
        public string label;
        public Faction faction;
        public Graphic highlightGraphic;
        public SpriteRenderer highlightSprite;
        public Color selectedColor = new Color(0.7f, 1f, 0.7f, 1f);
    }

    [Header("Refs")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private PanelController panelController;
    [SerializeField] private DescriptionPanelController descriptionPanel;
    [SerializeField] private PlayerDataRuntime playerRuntime;
    [SerializeField] private EnemyRuntime enemyRuntime;
    [SerializeField] private HandUI playerHand;
    [SerializeField] private EnemyHandUI enemyHand;
    [SerializeField] private CardDatabaseSO cardDatabase;

    [Header("State Menu")]
    [SerializeField] private int stateIndex = 2;
    [SerializeField] private bool wrap = true;
    [SerializeField] private List<StateTarget> targets = new List<StateTarget>();

    [Header("State Hand Reveal")]
    [SerializeField] private bool animateEnemyHandOnEnter = true;
    [SerializeField] private float enemyHandRiseYOffset = 260f;
    [SerializeField, Min(0.01f)] private float enemyHandRiseDuration = 0.22f;
    [SerializeField] private float enemyHandRiseStagger = 0.055f;
    [SerializeField] private bool enemyHandRiseFade = true;

    [Header("State Hand Inspect")]
    [SerializeField] private bool allowHandInspect = true;
    [SerializeField] private bool hideSpeechBubbleDuringHandInspect = true;
    [SerializeField] private string playerHandInspectMessage = "내 손패를 확인합니다.";
    [SerializeField] private string enemyHandInspectMessage = "상대 손패를 확인합니다.";

    [Header("Speech Bubbles")]
    [SerializeField] private bool useSpeechBubbles = true;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform enemyTarget;
    [SerializeField] private Transform speechBubble;
    [SerializeField] private Vector2 speechBubbleSize = new Vector2(4.8f, 3.3f);
    [SerializeField] private bool applySpeechBubbleSize = true;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector2 bubbleGap = new Vector2(0.55f, 0.45f);
    [SerializeField] private Vector2 screenPaddingWorld = new Vector2(0.55f, 0.55f);
    [SerializeField] private bool avoidSpeechBubbleUi = true;
    [SerializeField] private List<RectTransform> speechBubbleAvoidUiRects = new List<RectTransform>();
    [SerializeField] private float speechBubbleAvoidUiPadding = 0.25f;
    [SerializeField] private float bottomUiSafeWorldY = -2.65f;
    [SerializeField] private float speechBubbleZ = -1f;
    [SerializeField] private Color connectorColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private float connectorWidth = 0.035f;
    [SerializeField] private float connectorDashLength = 0.16f;
    [SerializeField] private float connectorGapLength = 0.1f;
    [SerializeField] private int connectorSortingOrder = 20;

    private readonly List<Color> originalColors = new List<Color>();
    private readonly List<Color> originalSpriteColors = new List<Color>();
    private readonly List<LineRenderer> connectorSegments = new List<LineRenderer>();
    private bool active;
    private bool menuHideRequested;
    private bool handShowRequested;
    private int currentIndex;
    private SpriteRenderer bubbleRenderer;
    private Material connectorMaterial;
    private Vector3 speechBubbleDefaultLocalPosition;
    private Vector3 speechBubbleDefaultLocalScale;
    private bool capturedSpeechBubbleDefault;
    private bool handInspectActive;
    private Faction handInspectFaction;
    private int stateInputBlockedFrame = -1;

    void Reset()
    {
        ResolveRefs();
    }

    void Awake()
    {
        ResolveRefs();
        EnsureDefaultTargets();
        CacheOriginalColors();
        HideSpeechBubbles();
    }

    void OnEnable()
    {
        if (menu) menu.onSubmit.AddListener(OnMenuSubmit);
    }

    void OnDisable()
    {
        if (menu) menu.onSubmit.RemoveListener(OnMenuSubmit);
        ExitStateMode(false);
        HideSpeechBubbles();
    }

    void OnDestroy()
    {
        if (connectorMaterial)
            Destroy(connectorMaterial);
    }

    void Update()
    {
        if (!active) return;

        if (Time.frameCount == stateInputBlockedFrame)
        {
            UpdateSpeechBubble();
            return;
        }

        if (handInspectActive)
        {
            HandleHandInspectInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.StateCancel))
                return;

            ExitStateMode(true);
            BattleTutorialGate.Report(BattleTutorialAction.StateCancel);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.StateHandInspect))
                return;

            bool wasInspecting = handInspectActive;
            EnterHandInspectMode();
            if (!wasInspecting && handInspectActive)
                BattleTutorialGate.Report(BattleTutorialAction.StateHandInspect);
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.StateTargetMove))
                return;

            MoveTarget(+1);
            BattleTutorialGate.Report(BattleTutorialAction.StateTargetMove);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.StateTargetMove))
                return;

            MoveTarget(-1);
            BattleTutorialGate.Report(BattleTutorialAction.StateTargetMove);
        }

        UpdateSpeechBubble();
    }

    private void OnMenuSubmit(int index)
    {
        if (active || index != ResolveStateIndex()) return;
        if (!BattleTutorialGate.Allows(BattleTutorialAction.StatePanelInteract)) return;
        EnterStateMode();
        BattleTutorialGate.Report(BattleTutorialAction.StatePanelInteract);
    }

    private void EnterStateMode()
    {
        ResolveRefs();
        EnsureDefaultTargets();
        CacheOriginalColors();

        active = true;
        handInspectActive = false;
        stateInputBlockedFrame = Time.frameCount;
        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, targets.Count - 1));

        if (menu) menu.EnableInput(false);
        if (panelController)
        {
            panelController.PushBattleMenuHideRequest();
            menuHideRequested = true;
            panelController.PushHandObjectShowRequest();
            handShowRequested = true;
            panelController.SetGameplayViewDelayed(true, 0.02f);
        }

        RefreshStateView();

        if (animateEnemyHandOnEnter && enemyHand)
            enemyHand.PlayCardsRiseStaggered(enemyHandRiseYOffset, enemyHandRiseDuration, enemyHandRiseStagger, enemyHandRiseFade);
    }

    private void ExitStateMode(bool restorePanelView)
    {
        if (!active) return;

        ExitHandInspectMode(false);
        active = false;
        RestoreHighlights();
        HideSpeechBubbles();
        descriptionPanel?.ExitStateView();
        ReleaseMenuHideRequest();
        ReleaseHandShowRequest();

        if (menu) menu.EnableInput(true);
        if (restorePanelView && panelController) panelController.SetGameplayView(false);
    }

    private void ReleaseMenuHideRequest()
    {
        if (!menuHideRequested) return;
        menuHideRequested = false;
        if (panelController) panelController.PopBattleMenuHideRequest();
    }

    private void ReleaseHandShowRequest()
    {
        if (!handShowRequested) return;
        handShowRequested = false;
        if (panelController) panelController.PopHandObjectShowRequest();
    }

    private void MoveTarget(int direction)
    {
        if (targets.Count == 0) return;

        int next = currentIndex + direction;
        if (wrap)
            next = (next % targets.Count + targets.Count) % targets.Count;
        else
            next = Mathf.Clamp(next, 0, targets.Count - 1);

        if (next == currentIndex) return;
        currentIndex = next;
        RefreshStateView();
    }

    private void RefreshStateView()
    {
        ApplyHighlights();
        EnsureStateHandsVisible();
        descriptionPanel?.EnterStateView(BuildCurrentStateText(), true, true, GetCurrentTargetFaction());
        UpdateSpeechBubble();
    }

    private void HandleHandInspectInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.StateCancel))
                return;

            ExitHandInspectMode(true);
            BattleTutorialGate.Report(BattleTutorialAction.StateCancel);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.StateHandCardMove))
                return;

            MoveHandInspectSelection(+1);
            BattleTutorialGate.Report(BattleTutorialAction.StateHandCardMove);
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.StateHandCardMove))
                return;

            MoveHandInspectSelection(-1);
            BattleTutorialGate.Report(BattleTutorialAction.StateHandCardMove);
            return;
        }

        if (!hideSpeechBubbleDuringHandInspect)
            UpdateSpeechBubble();
    }

    private void EnterHandInspectMode()
    {
        if (!allowHandInspect || targets.Count == 0)
            return;

        ResolveRefs();
        EnsureStateHandsVisible();

        StateTarget target = targets[Mathf.Clamp(currentIndex, 0, targets.Count - 1)];
        handInspectFaction = target.faction;

        bool canInspect = handInspectFaction == Faction.Player
            ? playerHand && playerHand.CardCount > 0
            : enemyHand && enemyHand.CardCount > 0;

        if (!canInspect)
        {
            descriptionPanel?.EnterStateView("확인할 카드가 없습니다.", true, true, handInspectFaction);
            return;
        }

        handInspectActive = true;
        ApplyHighlights();

        if (handInspectFaction == Faction.Player)
        {
            enemyHand?.ExitSelectMode();
            if (playerHand)
            {
                playerHand.ShowCards();
                playerHand.EnterReadOnlySelectMode(0, true);
            }
        }
        else
        {
            playerHand?.ExitSelectMode();
            enemyHand?.ShowAll();
            enemyHand?.EnterReadOnlySelectMode(-1, true);
        }

        if (hideSpeechBubbleDuringHandInspect)
            HideSpeechBubbles();
        else
            UpdateSpeechBubble();

        RefreshHandInspectText();
    }

    private void EnsureStateHandsVisible()
    {
        playerHand?.ShowCards();
        enemyHand?.ShowAll();
    }

    private void ExitHandInspectMode(bool refreshStateView)
    {
        if (!handInspectActive)
            return;

        handInspectActive = false;
        playerHand?.ExitSelectMode();
        enemyHand?.ExitSelectMode();

        if (refreshStateView && active)
            RefreshStateView();
    }

    private void MoveHandInspectSelection(int direction)
    {
        if (!handInspectActive)
            return;

        if (handInspectFaction == Faction.Player)
            playerHand?.MoveSelect(direction);
        else
            enemyHand?.MoveSelect(direction);

        RefreshHandInspectText();
    }

    private void RefreshHandInspectText()
    {
        descriptionPanel?.EnterStateView(BuildCurrentHandInspectText(), true, true, handInspectFaction);
    }

    private string BuildCurrentHandInspectText()
    {
        string id = handInspectFaction == Faction.Player
            ? playerHand?.GetVisibleIdAt(playerHand.CurrentSelectIndex)
            : enemyHand?.GetVisibleIdAt(enemyHand.CurrentSelectIndex);

        string fallback = handInspectFaction == Faction.Player
            ? playerHandInspectMessage
            : enemyHandInspectMessage;

        if (string.IsNullOrEmpty(id))
            return fallback;

        BaseCardSO card = GetCardById(id);
        if (!card)
            return $"{fallback}\n{id}";

        if (!string.IsNullOrEmpty(card.display))
            return card.display;

        string name = string.IsNullOrWhiteSpace(card.displayName) ? card.id : card.displayName;
        return string.IsNullOrEmpty(card.EffectText) ? name : $"{name}\n{card.EffectText}";
    }

    private BaseCardSO GetCardById(string id)
    {
        EnsureCardDatabase();
        return cardDatabase ? cardDatabase.GetById(id) : null;
    }

    private Faction GetCurrentTargetFaction()
    {
        if (targets.Count == 0)
            return Faction.Player;

        return targets[Mathf.Clamp(currentIndex, 0, targets.Count - 1)].faction;
    }

    private void EnsureCardDatabase()
    {
        if (cardDatabase) return;

        var orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (orchestrator && orchestrator.CardDatabase)
            cardDatabase = orchestrator.CardDatabase;
    }

    private string BuildCurrentStateText()
    {
        if (targets.Count == 0)
            return "표시할 대상이 없습니다.";

        StateTarget target = targets[Mathf.Clamp(currentIndex, 0, targets.Count - 1)];
        string label = !string.IsNullOrWhiteSpace(target.label) ? target.label : target.faction.ToString();
        return target.faction == Faction.Player
            ? BuildPlayerStateText(label)
            : BuildEnemyStateText(label);
    }

    private string BuildPlayerStateText(string label)
    {
        var data = playerRuntime ? playerRuntime.Data : PlayerDataRuntime.Instance?.Data;
        if (data == null)
            return $"{label}\n상태 정보를 찾을 수 없습니다.";

        var sb = new StringBuilder();
        sb.AppendLine(label);
        sb.AppendLine($"HP : {Mathf.Max(0, data.currentHP)} / {Mathf.Max(1, data.maxHP)}");
        sb.AppendLine($"ATK : {data.attack}    DEF : {data.defense}    SPD : {data.speed}");
        sb.AppendLine($"Emotion : +{data.emotionPositive} / -{data.emotionNegative}");
        sb.Append("현재 플레이어의 전투 상태입니다.");
        return sb.ToString();
    }

    private string BuildEnemyStateText(string label)
    {
        var enemy = enemyRuntime ? enemyRuntime : EnemyRuntime.Instance;
        if (enemy == null)
            return $"{label}\n상태 정보를 찾을 수 없습니다.";

        var sb = new StringBuilder();
        sb.AppendLine(string.IsNullOrWhiteSpace(enemy.enemyName) ? label : enemy.enemyName);
        sb.AppendLine($"HP : {Mathf.Max(0, enemy.currentHP)} / {Mathf.Max(1, enemy.maxHP)}");
        sb.AppendLine($"ATK : {enemy.attack}    DEF : {enemy.defense}    SPD : {enemy.speed}");
        sb.Append("현재 적의 전투 상태입니다.");
        return sb.ToString();
    }

    private void UpdateSpeechBubble()
    {
        if (!active || !useSpeechBubbles)
        {
            HideSpeechBubbles();
            return;
        }

        ResolveSpeechBubbleRefs();

        if (targets.Count == 0)
        {
            HideSpeechBubbles();
            return;
        }

        StateTarget target = targets[Mathf.Clamp(currentIndex, 0, targets.Count - 1)];
        Transform focus = GetTargetTransform(target.faction);
        Transform bubble = speechBubble;
        SpriteRenderer renderer = bubbleRenderer;

        if (!focus || !bubble || !renderer)
        {
            HideSpeechBubbles();
            return;
        }

        ApplySpeechBubbleSize();
        SetBubbleRenderer(bubbleRenderer, true);

        Bounds targetBounds = GetWorldBounds(focus);
        Rect targetRect = BoundsToRect(targetBounds);
        Vector2 bubbleSize = GetBubbleWorldSize(bubble, renderer);
        Vector3 bubblePosition = ComputeBubblePosition(target.faction, targetRect, targetBounds.center.z, bubbleSize);
        bubble.position = bubblePosition;

        Rect bubbleRect = MakeRect(new Vector2(bubblePosition.x, bubblePosition.y), bubbleSize);
        DrawDottedConnector(targetRect, bubbleRect, bubblePosition.z);
    }

    private void ResolveSpeechBubbleRefs()
    {
        if (!worldCamera) worldCamera = Camera.main;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].faction == Faction.Player && !playerTarget && targets[i].highlightSprite)
                playerTarget = targets[i].highlightSprite.transform;
            else if (targets[i].faction == Faction.Enemy && !enemyTarget && targets[i].highlightSprite)
                enemyTarget = targets[i].highlightSprite.transform;
        }

        if (!speechBubble)
        {
            GameObject go = GameObject.Find("Square22");
            if (go) speechBubble = go.transform;
        }

        if (!bubbleRenderer && speechBubble)
            bubbleRenderer = speechBubble.GetComponent<SpriteRenderer>();

        CaptureBubbleDefaults();
    }

    private void CaptureBubbleDefaults()
    {
        if (speechBubble && !capturedSpeechBubbleDefault)
        {
            speechBubbleDefaultLocalPosition = speechBubble.localPosition;
            speechBubbleDefaultLocalScale = speechBubble.localScale;
            capturedSpeechBubbleDefault = true;
        }
    }

    private void HideSpeechBubbles()
    {
        ResolveSpeechBubbleRefs();

        SetBubbleRenderer(bubbleRenderer, false);

        if (speechBubble && capturedSpeechBubbleDefault)
        {
            speechBubble.localPosition = speechBubbleDefaultLocalPosition;
            speechBubble.localScale = speechBubbleDefaultLocalScale;
        }

        DisableConnectorSegments(0);
    }

    private void SetBubbleRenderer(SpriteRenderer renderer, bool visible)
    {
        if (!renderer) return;
        renderer.enabled = visible;
        if (visible) renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, connectorSortingOrder + 1);
    }

    private void ApplySpeechBubbleSize()
    {
        if (!applySpeechBubbleSize || !speechBubble) return;

        Vector2 desired = new Vector2(
            Mathf.Max(0.05f, speechBubbleSize.x),
            Mathf.Max(0.05f, speechBubbleSize.y));

        Vector3 localScale = speechBubble.localScale;
        if (bubbleRenderer && bubbleRenderer.sprite)
        {
            Vector3 spriteSize = bubbleRenderer.sprite.bounds.size;
            Vector3 parentScale = speechBubble.parent ? speechBubble.parent.lossyScale : Vector3.one;
            float parentX = Mathf.Max(0.001f, Mathf.Abs(parentScale.x));
            float parentY = Mathf.Max(0.001f, Mathf.Abs(parentScale.y));

            localScale.x = desired.x / Mathf.Max(0.001f, Mathf.Abs(spriteSize.x) * parentX);
            localScale.y = desired.y / Mathf.Max(0.001f, Mathf.Abs(spriteSize.y) * parentY);
        }
        else
        {
            localScale.x = desired.x;
            localScale.y = desired.y;
        }

        speechBubble.localScale = localScale;
    }

    private Transform GetTargetTransform(Faction faction)
    {
        if (faction == Faction.Player) return playerTarget;
        return enemyTarget;
    }

    private Bounds GetWorldBounds(Transform target)
    {
        if (!target) return new Bounds(Vector3.zero, Vector3.one);

        Renderer renderer = target.GetComponent<Renderer>();
        if (!renderer) renderer = target.GetComponentInChildren<Renderer>();
        if (renderer) return renderer.bounds;

        return new Bounds(target.position, Vector3.one);
    }

    private Vector2 GetBubbleWorldSize(Transform bubble, SpriteRenderer renderer)
    {
        Vector2 size = Vector2.one;
        if (renderer && renderer.sprite)
        {
            Vector3 spriteSize = renderer.sprite.bounds.size;
            Vector3 scale = bubble.lossyScale;
            size = new Vector2(Mathf.Abs(spriteSize.x * scale.x), Mathf.Abs(spriteSize.y * scale.y));
        }
        else if (renderer)
        {
            size = new Vector2(Mathf.Abs(renderer.bounds.size.x), Mathf.Abs(renderer.bounds.size.y));
        }

        size.x = Mathf.Max(0.25f, size.x);
        size.y = Mathf.Max(0.25f, size.y);
        return size;
    }

    private Vector3 ComputeBubblePosition(Faction faction, Rect targetRect, float targetZ, Vector2 bubbleSize)
    {
        Rect safe = GetWorldSafeRect(targetZ);
        List<Rect> avoidRects = BuildUiAvoidRects(targetZ);
        bool preferLeft = faction == Faction.Player;

        if (TryBuildSideCandidate(preferLeft, targetRect, bubbleSize, safe, avoidRects, out Vector2 side))
            return new Vector3(side.x, side.y, speechBubbleZ);

        if (TryBuildSideCandidate(!preferLeft, targetRect, bubbleSize, safe, avoidRects, out side))
            return new Vector3(side.x, side.y, speechBubbleZ);

        bool preferAbove = targetRect.center.y < safe.center.y;
        if (TryBuildVerticalCandidate(preferAbove, targetRect, bubbleSize, safe, avoidRects, out Vector2 vertical))
            return new Vector3(vertical.x, vertical.y, speechBubbleZ);

        if (TryBuildVerticalCandidate(!preferAbove, targetRect, bubbleSize, safe, avoidRects, out vertical))
            return new Vector3(vertical.x, vertical.y, speechBubbleZ);

        Vector2 fallback = FindLeastOverlappingFallback(targetRect, bubbleSize, safe, avoidRects);
        return new Vector3(fallback.x, fallback.y, speechBubbleZ);
    }

    private bool TryBuildSideCandidate(bool leftSide, Rect targetRect, Vector2 bubbleSize, Rect safe, List<Rect> avoidRects, out Vector2 position)
    {
        float halfX = bubbleSize.x * 0.5f;
        float halfY = bubbleSize.y * 0.5f;
        float x = leftSide
            ? targetRect.xMin - bubbleGap.x - halfX
            : targetRect.xMax + bubbleGap.x + halfX;
        float y = ClampInside(targetRect.center.y, safe.yMin + halfY, safe.yMax - halfY);

        position = new Vector2(x, y);
        Rect rect = MakeRect(position, bubbleSize);
        return ContainsRect(safe, rect)
            && !rect.Overlaps(InflateRect(targetRect, 0.02f))
            && !OverlapsAny(rect, avoidRects);
    }

    private bool TryBuildVerticalCandidate(bool above, Rect targetRect, Vector2 bubbleSize, Rect safe, List<Rect> avoidRects, out Vector2 position)
    {
        float halfX = bubbleSize.x * 0.5f;
        float halfY = bubbleSize.y * 0.5f;
        float x = ClampInside(targetRect.center.x, safe.xMin + halfX, safe.xMax - halfX);
        float y = above
            ? targetRect.yMax + bubbleGap.y + halfY
            : targetRect.yMin - bubbleGap.y - halfY;

        position = new Vector2(x, y);
        Rect rect = MakeRect(position, bubbleSize);
        return ContainsRect(safe, rect)
            && !rect.Overlaps(InflateRect(targetRect, 0.02f))
            && !OverlapsAny(rect, avoidRects);
    }

    private Rect GetWorldSafeRect(float z)
    {
        Camera cam = worldCamera ? worldCamera : Camera.main;
        if (!cam)
        {
            float minY = Mathf.Max(-4.5f + screenPaddingWorld.y, bottomUiSafeWorldY);
            return Rect.MinMaxRect(-8f + screenPaddingWorld.x, minY, 8f - screenPaddingWorld.x, 4.5f - screenPaddingWorld.y);
        }

        float distance = Mathf.Abs(cam.transform.position.z - z);
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distance));

        float xMin = Mathf.Min(bottomLeft.x, topRight.x) + screenPaddingWorld.x;
        float xMax = Mathf.Max(bottomLeft.x, topRight.x) - screenPaddingWorld.x;
        float yMin = Mathf.Max(Mathf.Min(bottomLeft.y, topRight.y) + screenPaddingWorld.y, bottomUiSafeWorldY);
        float yMax = Mathf.Max(yMin + 0.1f, Mathf.Max(bottomLeft.y, topRight.y) - screenPaddingWorld.y);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private List<Rect> BuildUiAvoidRects(float z)
    {
        var rects = new List<Rect>();
        if (!avoidSpeechBubbleUi || speechBubbleAvoidUiRects == null || speechBubbleAvoidUiRects.Count == 0)
            return rects;

        Camera cam = worldCamera ? worldCamera : Camera.main;
        if (!cam) return rects;

        for (int i = 0; i < speechBubbleAvoidUiRects.Count; i++)
        {
            RectTransform rt = speechBubbleAvoidUiRects[i];
            if (!rt || !rt.gameObject.activeInHierarchy) continue;

            if (TryProjectUiRectToWorld(rt, cam, z, out Rect rect))
                rects.Add(InflateRect(rect, Mathf.Max(0f, speechBubbleAvoidUiPadding)));
        }

        return rects;
    }

    private bool TryProjectUiRectToWorld(RectTransform rt, Camera cam, float z, out Rect rect)
    {
        rect = default;
        if (!rt || !cam) return false;

        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera ? canvas.worldCamera : Camera.main;

        float distance = Mathf.Abs(cam.transform.position.z - z);
        float xMin = float.PositiveInfinity;
        float xMax = float.NegativeInfinity;
        float yMin = float.PositiveInfinity;
        float yMax = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, distance));
            xMin = Mathf.Min(xMin, world.x);
            xMax = Mathf.Max(xMax, world.x);
            yMin = Mathf.Min(yMin, world.y);
            yMax = Mathf.Max(yMax, world.y);
        }

        if (IsInvalidFloat(xMin) || IsInvalidFloat(xMax) || IsInvalidFloat(yMin) || IsInvalidFloat(yMax))
            return false;

        rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        return rect.width > 0.001f && rect.height > 0.001f;
    }

    private Vector2 FindLeastOverlappingFallback(Rect targetRect, Vector2 bubbleSize, Rect safe, List<Rect> avoidRects)
    {
        Vector2 center = targetRect.center;
        float halfX = bubbleSize.x * 0.5f;
        float halfY = bubbleSize.y * 0.5f;
        float left = targetRect.xMin - bubbleGap.x - halfX;
        float right = targetRect.xMax + bubbleGap.x + halfX;
        float above = targetRect.yMax + bubbleGap.y + halfY;
        float below = targetRect.yMin - bubbleGap.y - halfY;

        Vector2 best = new Vector2(
            ClampInside(center.x, safe.xMin + halfX, safe.xMax - halfX),
            ClampInside(center.y, safe.yMin + halfY, safe.yMax - halfY));
        float bestScore = float.PositiveInfinity;

        ScoreCandidate(new Vector2(left, center.y), bubbleSize, safe, targetRect, avoidRects, ref best, ref bestScore);
        ScoreCandidate(new Vector2(right, center.y), bubbleSize, safe, targetRect, avoidRects, ref best, ref bestScore);
        ScoreCandidate(new Vector2(center.x, above), bubbleSize, safe, targetRect, avoidRects, ref best, ref bestScore);
        ScoreCandidate(new Vector2(center.x, below), bubbleSize, safe, targetRect, avoidRects, ref best, ref bestScore);
        ScoreCandidate(new Vector2(left, above), bubbleSize, safe, targetRect, avoidRects, ref best, ref bestScore);
        ScoreCandidate(new Vector2(right, above), bubbleSize, safe, targetRect, avoidRects, ref best, ref bestScore);
        ScoreCandidate(new Vector2(left, below), bubbleSize, safe, targetRect, avoidRects, ref best, ref bestScore);
        ScoreCandidate(new Vector2(right, below), bubbleSize, safe, targetRect, avoidRects, ref best, ref bestScore);

        return best;
    }

    private void ScoreCandidate(Vector2 candidate, Vector2 bubbleSize, Rect safe, Rect targetRect, List<Rect> avoidRects, ref Vector2 best, ref float bestScore)
    {
        Vector2 clamped = new Vector2(
            ClampInside(candidate.x, safe.xMin + bubbleSize.x * 0.5f, safe.xMax - bubbleSize.x * 0.5f),
            ClampInside(candidate.y, safe.yMin + bubbleSize.y * 0.5f, safe.yMax - bubbleSize.y * 0.5f));
        Rect rect = MakeRect(clamped, bubbleSize);

        float score = Vector2.SqrMagnitude(clamped - targetRect.center);
        score += OverlapArea(rect, InflateRect(targetRect, 0.02f)) * 10000f;
        if (!ContainsRect(safe, rect)) score += 100000f;

        if (avoidRects != null)
        {
            for (int i = 0; i < avoidRects.Count; i++)
                score += OverlapArea(rect, avoidRects[i]) * 10000f;
        }

        if (score < bestScore)
        {
            bestScore = score;
            best = clamped;
        }
    }

    private void DrawDottedConnector(Rect targetRect, Rect bubbleRect, float z)
    {
        Vector2 start = ClosestPointOnRectEdge(targetRect, bubbleRect.center);
        Vector2 end = ClosestPointOnRectEdge(bubbleRect, targetRect.center);
        Vector2 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.001f)
        {
            DisableConnectorSegments(0);
            return;
        }

        Vector2 dir = delta / length;
        float dash = Mathf.Max(0.02f, connectorDashLength);
        float gap = Mathf.Max(0.01f, connectorGapLength);
        float cursor = 0f;
        int segmentIndex = 0;

        while (cursor < length)
        {
            float next = Mathf.Min(cursor + dash, length);
            LineRenderer line = GetConnectorSegment(segmentIndex++);
            ConfigureConnectorSegment(line);
            line.SetPosition(0, new Vector3(start.x + dir.x * cursor, start.y + dir.y * cursor, z));
            line.SetPosition(1, new Vector3(start.x + dir.x * next, start.y + dir.y * next, z));
            cursor += dash + gap;
        }

        DisableConnectorSegments(segmentIndex);
    }

    private LineRenderer GetConnectorSegment(int index)
    {
        while (connectorSegments.Count <= index)
        {
            var go = new GameObject("StateBubbleConnectorSegment");
            go.transform.SetParent(transform, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            connectorSegments.Add(line);
        }

        return connectorSegments[index];
    }

    private void ConfigureConnectorSegment(LineRenderer line)
    {
        if (!line) return;

        line.enabled = true;
        line.positionCount = 2;
        line.startWidth = connectorWidth;
        line.endWidth = connectorWidth;
        line.startColor = connectorColor;
        line.endColor = connectorColor;
        line.sortingOrder = connectorSortingOrder;
        line.numCapVertices = 0;

        Material material = GetConnectorMaterial();
        if (material) line.sharedMaterial = material;
    }

    private Material GetConnectorMaterial()
    {
        if (connectorMaterial) return connectorMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (!shader) shader = Shader.Find("UI/Default");
        if (shader) connectorMaterial = new Material(shader);

        return connectorMaterial;
    }

    private void DisableConnectorSegments(int startIndex)
    {
        for (int i = Mathf.Max(0, startIndex); i < connectorSegments.Count; i++)
            if (connectorSegments[i]) connectorSegments[i].enabled = false;
    }

    private static Rect BoundsToRect(Bounds bounds)
    {
        return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
    }

    private static Rect MakeRect(Vector2 center, Vector2 size)
    {
        Vector2 half = size * 0.5f;
        return Rect.MinMaxRect(center.x - half.x, center.y - half.y, center.x + half.x, center.y + half.y);
    }

    private static Rect InflateRect(Rect rect, float amount)
    {
        return Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);
    }

    private static bool ContainsRect(Rect outer, Rect inner)
    {
        return inner.xMin >= outer.xMin
            && inner.xMax <= outer.xMax
            && inner.yMin >= outer.yMin
            && inner.yMax <= outer.yMax;
    }

    private static bool OverlapsAny(Rect rect, List<Rect> others)
    {
        if (others == null) return false;
        for (int i = 0; i < others.Count; i++)
            if (rect.Overlaps(others[i])) return true;
        return false;
    }

    private static float OverlapArea(Rect a, Rect b)
    {
        float width = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
        float height = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
        return width * height;
    }

    private static bool IsInvalidFloat(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value);
    }

    private static float ClampInside(float value, float min, float max)
    {
        if (min > max) return (min + max) * 0.5f;
        return Mathf.Clamp(value, min, max);
    }

    private static Vector2 ClosestPointOnRectEdge(Rect rect, Vector2 toward)
    {
        Vector2 center = rect.center;
        Vector2 dir = toward - center;
        float xScore = Mathf.Abs(dir.x) / Mathf.Max(0.001f, rect.width);
        float yScore = Mathf.Abs(dir.y) / Mathf.Max(0.001f, rect.height);

        if (xScore > yScore)
        {
            float x = dir.x >= 0f ? rect.xMax : rect.xMin;
            float margin = rect.height * 0.2f;
            float y = ClampInside(toward.y, rect.yMin + margin, rect.yMax - margin);
            return new Vector2(x, y);
        }

        {
            float y = dir.y >= 0f ? rect.yMax : rect.yMin;
            float margin = rect.width * 0.2f;
            float x = ClampInside(toward.x, rect.xMin + margin, rect.xMax - margin);
            return new Vector2(x, y);
        }
    }

    private void ApplyHighlights()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var graphic = targets[i].highlightGraphic;
            var sprite = targets[i].highlightSprite;

            if (graphic)
            {
                Color original = i < originalColors.Count ? originalColors[i] : graphic.color;
                graphic.color = i == currentIndex ? targets[i].selectedColor : original;
            }

            if (sprite)
            {
                Color original = i < originalSpriteColors.Count ? originalSpriteColors[i] : sprite.color;
                sprite.color = i == currentIndex ? targets[i].selectedColor : original;
            }
        }
    }

    private void RestoreHighlights()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var graphic = targets[i].highlightGraphic;
            if (graphic && i < originalColors.Count)
                graphic.color = originalColors[i];

            var sprite = targets[i].highlightSprite;
            if (sprite && i < originalSpriteColors.Count)
                sprite.color = originalSpriteColors[i];
        }
    }

    private void CacheOriginalColors()
    {
        originalColors.Clear();
        originalSpriteColors.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            originalColors.Add(targets[i].highlightGraphic ? targets[i].highlightGraphic.color : Color.white);
            originalSpriteColors.Add(targets[i].highlightSprite ? targets[i].highlightSprite.color : Color.white);
        }
    }

    private int ResolveStateIndex()
    {
        return menu != null && menu.EntryCount >= 5 ? stateIndex : -1;
    }

    private void ResolveRefs()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!panelController) panelController = FindObjectOfType<PanelController>(true);
        if (!descriptionPanel) descriptionPanel = FindObjectOfType<DescriptionPanelController>(true);
        if (!playerRuntime) playerRuntime = PlayerDataRuntime.Instance ?? FindObjectOfType<PlayerDataRuntime>(true);
        if (!enemyRuntime) enemyRuntime = EnemyRuntime.Instance ?? FindObjectOfType<EnemyRuntime>(true);
        if (!playerHand) playerHand = FindObjectOfType<HandUI>(true);
        if (!enemyHand) enemyHand = FindObjectOfType<EnemyHandUI>(true);
        EnsureCardDatabase();
        ResolveSpeechBubbleRefs();
    }

    private void EnsureDefaultTargets()
    {
        if (targets.Count > 0) return;

        targets.Add(new StateTarget { label = "Player", faction = Faction.Player });
        targets.Add(new StateTarget { label = "Enemy", faction = Faction.Enemy });
    }
}
