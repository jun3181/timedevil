using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleTutorialController : MonoBehaviour
{
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
    [SerializeField] private Color blockerColor = new Color(0f, 0f, 0f, 0.22f);
    [SerializeField] private Color windowColor = new Color(0.06f, 0.07f, 0.08f, 0.94f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField, Min(1f)] private float fontSize = 28f;
    [SerializeField] private Vector4 textPadding = new Vector4(28f, 20f, 28f, 20f);

    private int currentStepIndex = -1;
    private bool running;
    private bool waitingForContinueKeyRelease;
    private int lastCompletedFrame = -1;

    void Awake()
    {
        ResolveRefs();
        SetUiVisible(false);
    }

    void OnEnable()
    {
        BattleTutorialGate.OnActionReported += HandleTutorialActionReported;
    }

    void OnDisable()
    {
        BattleTutorialGate.OnActionReported -= HandleTutorialActionReported;
        if (running)
            StopTutorial(false);
    }

    void Start()
    {
        if (autoStart)
            StartTutorial();
    }

    void Update()
    {
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
            CompleteCurrentStep(BattleTutorialAction.Continue);
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
        BattleTutorialGate.Close();
        SetUiVisible(false);

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
        if (!running || currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        if (Time.frameCount == lastCompletedFrame)
            return;

        TutorialStep step = steps[currentStepIndex];
        if (step.advanceMode != BattleTutorialAdvanceMode.WaitAction)
            return;

        if (action == step.requiredAction)
            CompleteCurrentStep(action);
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
        if (!targetCanvas) targetCanvas = GetComponentInParent<Canvas>();
        if (!targetCanvas) targetCanvas = FindObjectOfType<Canvas>(true);

        if (createUiIfMissing && (!root || !window || !messageText))
            CreateRuntimeUi();

        if (root && !rootGroup) rootGroup = root.GetComponent<CanvasGroup>();
        if (root && !blocker) blocker = root.Find("Blocker")?.GetComponent<Image>();
        if (root && !window) window = root.Find("Window") as RectTransform;
        if (window && !messageText) messageText = window.GetComponentInChildren<TMP_Text>(true);
    }

    private void CreateRuntimeUi()
    {
        if (!targetCanvas)
            return;

        if (!root)
        {
            var rootObject = new GameObject("BattleTutorialUI", typeof(RectTransform), typeof(CanvasGroup));
            rootObject.transform.SetParent(targetCanvas.transform, false);
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
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
