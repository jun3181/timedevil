using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleTutorialController : MonoBehaviour
{
    public static BattleTutorialController Instance { get; private set; }
    private const string RuntimeRootName = "BattleTutorialUI";

    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(2, 6)] public string message = "Enter tutorial text.";
        public Vector2 windowAnchoredPosition = new Vector2(0f, 120f);
        public Vector2 windowSize = new Vector2(720f, 220f);
        public BattleTutorialAdvanceMode advanceMode = BattleTutorialAdvanceMode.PressE;
        public BattleTutorialAction requiredAction = BattleTutorialAction.None;
        public bool allowMenuNavigation = true;
        public bool allowCardSelectionNavigation = true;
        public bool allowStateNavigation = true;
        public bool allowCancelInput = false;
        public bool endTutorialAfterStep = false;
        public int nextStepIndex = -1;
    }

    [Header("Flow")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool playOnlyOnce = false;
    [SerializeField] private string playerPrefsKey = "BattleTutorial_Default_Seen";
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();
    [SerializeField] private KeyCode continueKey = KeyCode.E;

    [Header("UI Refs")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Image blocker;
    [SerializeField] private RectTransform window;
    [SerializeField] private TMP_Text messageText;

    [Header("Runtime UI Defaults")]
    [SerializeField] private bool createUiIfMissing = true;
    [SerializeField] private bool forceDedicatedRuntimeUi = true;
    [SerializeField] private Color blockerColor = new Color(0f, 0f, 0f, 0.22f);
    [SerializeField] private Color windowColor = new Color(0.06f, 0.07f, 0.08f, 0.94f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField, Min(1f)] private float fontSize = 28f;
    [SerializeField] private Vector4 textPadding = new Vector4(28f, 20f, 28f, 20f);

    private int currentStepIndex = -1;
    private bool running;
    private bool waitingForContinueKeyRelease;
    private int lastCompletedFrame = -1;
    private bool externalPromptActive;
    private bool waitingForExternalContinueKeyRelease;
    private BattleTutorialAdvanceMode externalPromptAdvanceMode = BattleTutorialAdvanceMode.PressE;
    private BattleTutorialAction externalPromptRequiredAction = BattleTutorialAction.Continue;
    private bool externalPromptAllowMenuNavigation;
    private bool externalPromptAllowCardSelectionNavigation;
    private bool externalPromptAllowStateNavigation;
    private bool externalPromptAllowCancelInput;

    public bool IsRunningTutorial => running;
    public bool IsExternalPromptActive => externalPromptActive;
    public bool IsPromptVisible => running || externalPromptActive;

    void Awake()
    {
        if (Instance == null || !Instance.isActiveAndEnabled)
            Instance = this;

        ResolveRefs();
        SetUiVisible(false);
    }

    void OnEnable()
    {
        Instance = this;
        BattleTutorialGate.OnActionReported += HandleTutorialActionReported;
    }

    void OnDisable()
    {
        BattleTutorialGate.OnActionReported -= HandleTutorialActionReported;
        if (running)
            StopTutorial(false);
        if (externalPromptActive)
            ClearExternalPrompt(false);

        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (autoStart)
            StartTutorial();
    }

    void Update()
    {
        if (externalPromptActive)
        {
            UpdateExternalPrompt();
            return;
        }

        if (!running || currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        TutorialStep step = steps[currentStepIndex];
        if (step.advanceMode != BattleTutorialAdvanceMode.PressE)
            return;

        if (waitingForContinueKeyRelease)
        {
            if (!Input.GetKey(continueKey))
                waitingForContinueKeyRelease = false;
            return;
        }

        if (Input.GetKeyDown(continueKey))
        {
            BattleTutorialGate.MarkInputConsumedThisFrame(BattleTutorialAction.Continue);
            CompleteCurrentStep(BattleTutorialAction.Continue);
        }
    }

    public void StartTutorial()
    {
        if (running)
            return;

        if (steps == null || steps.Count == 0)
            return;

        if (playOnlyOnce && !string.IsNullOrEmpty(playerPrefsKey) && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)
            return;

        running = true;
        ShowStep(0);
    }

    public void StopTutorial()
    {
        StopTutorial(true);
    }

    private void StopTutorial(bool markSeen)
    {
        running = false;
        currentStepIndex = -1;

        if (externalPromptActive)
        {
            ApplyExternalPromptGate();
        }
        else
        {
            BattleTutorialGate.Close();
            SetUiVisible(false);
        }

        if (markSeen && playOnlyOnce && !string.IsNullOrEmpty(playerPrefsKey))
        {
            PlayerPrefs.SetInt(playerPrefsKey, 1);
            PlayerPrefs.Save();
        }
    }

    public void ResetSeenFlag()
    {
        if (string.IsNullOrEmpty(playerPrefsKey))
            return;

        PlayerPrefs.DeleteKey(playerPrefsKey);
        PlayerPrefs.Save();
    }

    public bool ShowExternalPrompt(string message, Vector2 windowAnchoredPosition, Vector2 windowSize)
    {
        return ShowExternalPrompt(
            message,
            windowAnchoredPosition,
            windowSize,
            BattleTutorialAdvanceMode.PressE,
            BattleTutorialAction.Continue,
            false,
            false,
            false,
            false);
    }

    public bool ShowExternalPrompt(
        string message,
        Vector2 windowAnchoredPosition,
        Vector2 windowSize,
        BattleTutorialAdvanceMode advanceMode,
        BattleTutorialAction requiredAction,
        bool allowMenuNavigation,
        bool allowCardSelectionNavigation,
        bool allowStateNavigation,
        bool allowCancelInput)
    {
        ResolveRefs();
        if (!root || !window || !messageText)
        {
            Debug.LogWarning("[BattleTutorial] External tutorial UI is missing.");
            return false;
        }

        externalPromptActive = true;
        externalPromptAdvanceMode = advanceMode;
        externalPromptRequiredAction = requiredAction;
        externalPromptAllowMenuNavigation = allowMenuNavigation;
        externalPromptAllowCardSelectionNavigation = allowCardSelectionNavigation;
        externalPromptAllowStateNavigation = allowStateNavigation;
        externalPromptAllowCancelInput = allowCancelInput;

        root.SetAsLastSibling();
        window.anchoredPosition = windowAnchoredPosition;
        window.sizeDelta = windowSize;
        messageText.text = message ?? string.Empty;

        SetUiVisible(true);
        ApplyExternalPromptGate();
        waitingForExternalContinueKeyRelease = advanceMode == BattleTutorialAdvanceMode.PressE
            && continueKey != KeyCode.None
            && Input.GetKey(continueKey);
        return true;
    }

    public bool ShowExternalPrompt(string message)
    {
        return ShowExternalPrompt(message, new Vector2(0f, 120f), new Vector2(720f, 220f));
    }

    public void ClearExternalPrompt()
    {
        ClearExternalPrompt(true);
    }

    public static bool IsAnyPromptVisible()
    {
        return Instance != null && Instance.IsPromptVisible;
    }

    private void UpdateExternalPrompt()
    {
        if (externalPromptAdvanceMode != BattleTutorialAdvanceMode.PressE)
            return;

        if (waitingForExternalContinueKeyRelease)
        {
            if (!Input.GetKey(continueKey))
                waitingForExternalContinueKeyRelease = false;
            return;
        }

        if (continueKey != KeyCode.None && Input.GetKeyDown(continueKey))
        {
            BattleTutorialGate.MarkInputConsumedThisFrame(BattleTutorialAction.Continue);
            ClearExternalPrompt(true);
        }
    }

    private void ClearExternalPrompt(bool restoreTutorialStep)
    {
        externalPromptActive = false;
        waitingForExternalContinueKeyRelease = false;
        externalPromptAdvanceMode = BattleTutorialAdvanceMode.PressE;
        externalPromptRequiredAction = BattleTutorialAction.Continue;
        externalPromptAllowMenuNavigation = false;
        externalPromptAllowCardSelectionNavigation = false;
        externalPromptAllowStateNavigation = false;
        externalPromptAllowCancelInput = false;

        if (restoreTutorialStep && running && currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            ShowStep(currentStepIndex);
            return;
        }

        BattleTutorialGate.Close();
        SetUiVisible(false);
    }

    private void ApplyExternalPromptGate()
    {
        if (externalPromptAdvanceMode == BattleTutorialAdvanceMode.PressE)
        {
            BattleTutorialGate.OpenPressE();
            if (blocker) blocker.raycastTarget = true;
            return;
        }

        BattleTutorialGate.OpenWaitAction(
            externalPromptRequiredAction,
            externalPromptAllowMenuNavigation,
            externalPromptAllowCardSelectionNavigation,
            externalPromptAllowStateNavigation,
            externalPromptAllowCancelInput);

        if (blocker) blocker.raycastTarget = false;
    }

    private void ShowStep(int index)
    {
        if (steps == null || index < 0 || index >= steps.Count)
        {
            StopTutorial();
            return;
        }

        ResolveRefs();
        if (!root || !window || !messageText)
        {
            Debug.LogWarning("[BattleTutorial] Tutorial UI is missing.");
            StopTutorial();
            return;
        }

        currentStepIndex = index;
        TutorialStep step = steps[currentStepIndex];

        root.SetAsLastSibling();
        window.anchoredPosition = step.windowAnchoredPosition;
        window.sizeDelta = step.windowSize;
        messageText.text = step.message ?? string.Empty;

        SetUiVisible(true);
        ApplyGate(step);
        waitingForContinueKeyRelease = step.advanceMode == BattleTutorialAdvanceMode.PressE && Input.GetKey(continueKey);
    }

    private void ApplyGate(TutorialStep step)
    {
        if (step.advanceMode == BattleTutorialAdvanceMode.PressE)
        {
            BattleTutorialGate.OpenPressE();
            if (blocker) blocker.raycastTarget = true;
            return;
        }

        BattleTutorialGate.OpenWaitAction(
            step.requiredAction,
            step.allowMenuNavigation,
            step.allowCardSelectionNavigation,
            step.allowStateNavigation,
            step.allowCancelInput);

        if (blocker) blocker.raycastTarget = false;
    }

    private void HandleTutorialActionReported(BattleTutorialAction action)
    {
        if (externalPromptActive && externalPromptAdvanceMode == BattleTutorialAdvanceMode.WaitAction)
        {
            if (action == externalPromptRequiredAction)
            {
                BattleTutorialGate.MarkInputConsumedThisFrame(action);
                ClearExternalPrompt(true);
            }
            return;
        }

        if (!running || currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        if (Time.frameCount == lastCompletedFrame)
            return;

        TutorialStep step = steps[currentStepIndex];
        if (step.advanceMode != BattleTutorialAdvanceMode.WaitAction)
            return;

        if (action == step.requiredAction)
        {
            BattleTutorialGate.MarkInputConsumedThisFrame(action);
            CompleteCurrentStep(action);
        }
    }

    private void CompleteCurrentStep(BattleTutorialAction completedAction)
    {
        if (!running || currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        lastCompletedFrame = Time.frameCount;

        TutorialStep step = steps[currentStepIndex];
        if (step.endTutorialAfterStep)
        {
            StopTutorial();
            return;
        }

        int next = step.nextStepIndex >= 0 ? step.nextStepIndex : currentStepIndex + 1;
        if (next < 0 || next >= steps.Count)
        {
            StopTutorial();
            return;
        }

        ShowStep(next);
    }

    private void ResolveRefs()
    {
        if (!IsUsableCanvas(targetCanvas))
            targetCanvas = GetComponentInParent<Canvas>();
        if (!IsUsableCanvas(targetCanvas))
            targetCanvas = FindBestCanvas();

        if (createUiIfMissing && forceDedicatedRuntimeUi)
            EnsureDedicatedRuntimeUi();
        else if (createUiIfMissing && (!root || !window || !messageText))
            CreateRuntimeUi();

        if (root && !rootGroup) rootGroup = root.GetComponent<CanvasGroup>();
        if (root && !blocker) blocker = root.Find("Blocker")?.GetComponent<Image>();
        if (root && !window) window = root.Find("Window") as RectTransform;
        if (window && !messageText) messageText = window.GetComponentInChildren<TMP_Text>(true);
    }

    private void EnsureDedicatedRuntimeUi()
    {
        if (!targetCanvas)
            return;

        if (root && root.name == RuntimeRootName && root.GetComponentInParent<Canvas>(true) != targetCanvas)
        {
            root.SetParent(targetCanvas.transform, false);
            ResetRootRect(root);
        }

        if (!IsDedicatedRuntimeRoot(root))
        {
            Transform existing = targetCanvas.transform.Find(RuntimeRootName);
            root = existing as RectTransform;
            rootGroup = null;
            blocker = null;
            window = null;
            messageText = null;
        }

        CreateRuntimeUi();
    }

    private bool IsDedicatedRuntimeRoot(RectTransform candidate)
    {
        return candidate
            && candidate.name == RuntimeRootName
            && targetCanvas
            && candidate.GetComponentInParent<Canvas>(true) == targetCanvas;
    }

    private void CreateRuntimeUi()
    {
        if (!targetCanvas)
            return;

        if (!root)
        {
            var rootObject = new GameObject(RuntimeRootName, typeof(RectTransform), typeof(CanvasGroup));
            rootObject.transform.SetParent(targetCanvas.transform, false);
            root = rootObject.GetComponent<RectTransform>();
            ResetRootRect(root);
            rootGroup = rootObject.GetComponent<CanvasGroup>();
        }

        if (!blocker)
        {
            var blockerObject = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
            blockerObject.transform.SetParent(root, false);
            var blockerRect = blockerObject.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            blocker = blockerObject.GetComponent<Image>();
            blocker.color = blockerColor;
        }

        if (!window)
        {
            var windowObject = new GameObject("Window", typeof(RectTransform), typeof(Image));
            windowObject.transform.SetParent(root, false);
            window = windowObject.GetComponent<RectTransform>();
            window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            var windowImage = windowObject.GetComponent<Image>();
            windowImage.color = windowColor;
        }

        if (!messageText)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(window, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(textPadding.x, textPadding.w);
            textRect.offsetMax = new Vector2(-textPadding.z, -textPadding.y);

            messageText = textObject.GetComponent<TextMeshProUGUI>();
            messageText.alignment = TextAlignmentOptions.MidlineLeft;
            messageText.enableWordWrapping = true;
            messageText.raycastTarget = false;
            messageText.fontSize = fontSize;
            messageText.color = textColor;
        }

        root.SetAsLastSibling();
    }

    private static void ResetRootRect(RectTransform rect)
    {
        if (!rect)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static bool IsUsableCanvas(Canvas canvas)
    {
        return canvas
            && canvas.isActiveAndEnabled
            && canvas.gameObject.activeInHierarchy
            && IsVisibleThroughCanvasGroups(canvas.transform);
    }

    private static Canvas FindBestCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas best = null;
        int bestScore = int.MinValue;

        foreach (Canvas canvas in canvases)
        {
            if (!canvas || !canvas.gameObject.scene.IsValid())
                continue;

            int score = 0;
            if (canvas.isActiveAndEnabled && canvas.gameObject.activeInHierarchy) score += 1000;
            else score -= 1000;
            if (IsVisibleThroughCanvasGroups(canvas.transform)) score += 300;
            else score -= 600;
            if (canvas.GetComponent<GraphicRaycaster>()) score += 200;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) score += 80;
            if (canvas.transform.parent == null) score += 40;
            if (canvas.name == "Canvas") score += 30;
            score += canvas.sortingOrder;

            if (best == null || score > bestScore)
            {
                best = canvas;
                bestScore = score;
            }
        }

        return best;
    }

    private static bool IsVisibleThroughCanvasGroups(Transform transform)
    {
        Transform cursor = transform;
        while (cursor)
        {
            CanvasGroup group = cursor.GetComponent<CanvasGroup>();
            if (group && group.alpha <= 0.01f)
                return false;
            cursor = cursor.parent;
        }

        return true;
    }

    private void SetUiVisible(bool visible)
    {
        if (!root)
            return;

        root.gameObject.SetActive(visible);
        if (!rootGroup)
            return;

        rootGroup.alpha = visible ? 1f : 0f;
        rootGroup.interactable = visible;
        rootGroup.blocksRaycasts = visible;
    }
}
