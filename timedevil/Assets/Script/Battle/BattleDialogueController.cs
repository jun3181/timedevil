using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BattleDialogueTutorialPrompt
{
    public bool enabled = false;

    [TextArea(2, 6)]
    public string message = "";

    public Vector2 windowAnchoredPosition = new Vector2(0f, 120f);
    public Vector2 windowSize = new Vector2(720f, 220f);

    public bool HasMessage => enabled && !string.IsNullOrWhiteSpace(message);
}

[System.Serializable]
public class BattleDialogueLine
{
    [TextArea(2, 5)]
    public string text = "";

    [Tooltip("비우면 BattleDialogue.defaultSpeakerName 사용")]
    public string speakerName = "";

    [Header("Portrait Override")]
    public Sprite leftPortrait;
    public Sprite rightPortrait;
    public PortraitFocus focus = PortraitFocus.None;

    [Header("Tutorial Prompt")]
    [Tooltip("이 대사가 표시되는 도중 같이 띄울 튜토리얼 설명창")]
    public BattleDialogueTutorialPrompt lineTutorial = new BattleDialogueTutorialPrompt();

    [Tooltip("이 대사를 넘기려는 순간 먼저 띄울 튜토리얼 설명창")]
    public BattleDialogueTutorialPrompt advanceTutorial = new BattleDialogueTutorialPrompt();
}

[System.Serializable]
public class BattleDialogue
{
    public string defaultSpeakerName = "";
    public Sprite defaultLeftPortrait;
    public Sprite defaultRightPortrait;
    public BattleDialogueLine[] lines;
}

public class BattleDialogueController : MonoBehaviour
{
    public static BattleDialogueController Instance { get; private set; }

    [Header("Flow")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private BattleDialogue startDialogue = new BattleDialogue();
    [SerializeField] private KeyCode advanceKey = KeyCode.E;
    [SerializeField] private bool lockBattleInputWhileActive = true;
    [SerializeField] private bool createTutorialControllerIfMissing = true;

    [Header("UI Refs")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform window;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image leftPortraitImage;
    [SerializeField] private Image rightPortraitImage;

    [Header("Runtime UI Defaults")]
    [SerializeField] private bool createUiIfMissing = true;
    [SerializeField] private Vector2 windowAnchoredPosition = new Vector2(0f, 58f);
    [SerializeField] private Vector2 windowSize = new Vector2(1180f, 220f);
    [SerializeField] private Color windowColor = new Color(0.04f, 0.045f, 0.055f, 0.95f);
    [SerializeField] private Color nameTextColor = new Color(0.95f, 0.9f, 0.72f, 1f);
    [SerializeField] private Color dialogueTextColor = Color.white;
    [SerializeField, Min(1f)] private float nameFontSize = 30f;
    [SerializeField, Min(1f)] private float dialogueFontSize = 28f;

    [Header("Portraits")]
    [SerializeField] private bool dimInactivePortrait = true;
    [SerializeField, Range(0f, 1f)] private float activePortraitAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float inactivePortraitAlpha = 0.35f;

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField, Min(1f)] private float charactersPerSecond = 38f;
    [SerializeField, Min(0f)] private float punctuationExtraDelay = 0.08f;
    [SerializeField, Min(0f)] private float lineAdvanceLockSeconds = 0.08f;

    private BattleDialogue currentDialogue;
    private int currentLineIndex = -1;
    private bool isDialogueActive;
    private bool waitingForAdvanceKeyRelease;
    private bool lineTutorialShown;
    private bool advanceTutorialShown;
    private bool ownsInputGate;
    private Coroutine typingRoutine;
    private bool isTyping;
    private float nextAdvanceAllowedUnscaledTime;
    private Sprite currentLeftPortrait;
    private Sprite currentRightPortrait;
    private BattleTutorialController tutorialController;

    public bool IsDialogueActive => isDialogueActive;
    public bool IsTyping => isTyping;

    void Awake()
    {
        Instance = this;
        ResolveRefs();
        SetUiVisible(false);
    }

    void OnEnable()
    {
        Instance = this;
    }

    void OnDisable()
    {
        if (isDialogueActive)
            EndDialogue(false);

        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (playOnStart)
            Play(startDialogue);
    }

    void Update()
    {
        if (!isDialogueActive)
            return;

        EnsureDialogueGate();

        if (BattleTutorialGate.WasInputConsumedThisFrame)
            return;

        if (IsTutorialPromptBlockingDialogue())
            return;

        if (waitingForAdvanceKeyRelease)
        {
            if (!Input.GetKey(advanceKey))
                waitingForAdvanceKeyRelease = false;
            return;
        }

        if (advanceKey != KeyCode.None && Input.GetKeyDown(advanceKey))
            HandleAdvanceInput();
    }

    public void Play(BattleDialogue dialogue)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
            return;

        currentDialogue = dialogue;
        currentLineIndex = -1;
        currentLeftPortrait = dialogue.defaultLeftPortrait;
        currentRightPortrait = dialogue.defaultRightPortrait;
        isDialogueActive = true;
        waitingForAdvanceKeyRelease = advanceKey != KeyCode.None && Input.GetKey(advanceKey);

        ResolveRefs();
        SetUiVisible(true);
        OpenDialogueGate();
        ShowNextLine();
    }

    public void PlayConfiguredDialogue()
    {
        Play(startDialogue);
    }

    public void StopDialogue()
    {
        EndDialogue(true);
    }

    public static bool TryPlay(BattleDialogue dialogue)
    {
        if (Instance == null)
            return false;

        Instance.Play(dialogue);
        return Instance.IsDialogueActive;
    }

    private void HandleAdvanceInput()
    {
        if (Time.unscaledTime < nextAdvanceAllowedUnscaledTime)
            return;

        BattleTutorialGate.MarkInputConsumedThisFrame(BattleTutorialAction.Continue);

        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        if (TryShowAdvanceTutorial())
            return;

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        currentLineIndex++;
        if (currentDialogue == null || currentDialogue.lines == null || currentLineIndex >= currentDialogue.lines.Length)
        {
            EndDialogue(true);
            return;
        }

        BattleDialogueLine line = currentDialogue.lines[currentLineIndex];
        if (line == null || string.IsNullOrWhiteSpace(line.text))
        {
            ShowNextLine();
            return;
        }

        lineTutorialShown = false;
        advanceTutorialShown = false;

        string speaker = string.IsNullOrWhiteSpace(line.speakerName)
            ? currentDialogue.defaultSpeakerName
            : line.speakerName;

        if (nameText)
            nameText.text = speaker ?? string.Empty;

        if (line.leftPortrait != null)
            currentLeftPortrait = line.leftPortrait;
        if (line.rightPortrait != null)
            currentRightPortrait = line.rightPortrait;

        ApplyPortraits(currentLeftPortrait, currentRightPortrait, line.focus);

        nextAdvanceAllowedUnscaledTime = Time.unscaledTime + Mathf.Max(0f, lineAdvanceLockSeconds);
        if (useTypewriter)
            StartTyping(line.text);
        else if (dialogueText)
        {
            dialogueText.text = line.text;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        TryShowLineTutorial();
    }

    private bool TryShowLineTutorial()
    {
        if (lineTutorialShown || !TryGetCurrentLine(out BattleDialogueLine line))
            return false;

        lineTutorialShown = true;
        return TryShowTutorialPrompt(line.lineTutorial);
    }

    private bool TryShowAdvanceTutorial()
    {
        if (advanceTutorialShown || !TryGetCurrentLine(out BattleDialogueLine line))
            return false;

        if (line.advanceTutorial == null || !line.advanceTutorial.HasMessage)
            return false;

        advanceTutorialShown = true;
        return TryShowTutorialPrompt(line.advanceTutorial);
    }

    private bool TryShowTutorialPrompt(BattleDialogueTutorialPrompt prompt)
    {
        if (prompt == null || !prompt.HasMessage)
            return false;

        BattleTutorialController controller = ResolveTutorialController();
        if (controller == null)
            return false;

        return controller.ShowExternalPrompt(
            prompt.message,
            prompt.windowAnchoredPosition,
            prompt.windowSize);
    }

    private BattleTutorialController ResolveTutorialController()
    {
        if (tutorialController != null && tutorialController.isActiveAndEnabled)
            return tutorialController;

        tutorialController = BattleTutorialController.Instance;
        if (tutorialController != null && tutorialController.isActiveAndEnabled)
            return tutorialController;

        tutorialController = FindObjectOfType<BattleTutorialController>();
        if (tutorialController != null && tutorialController.isActiveAndEnabled)
            return tutorialController;

        if (!createTutorialControllerIfMissing)
            return null;

        var go = new GameObject("BattleTutorialController_Runtime");
        tutorialController = go.AddComponent<BattleTutorialController>();
        return tutorialController;
    }

    private bool IsTutorialPromptBlockingDialogue()
    {
        BattleTutorialController controller = BattleTutorialController.Instance;
        return controller != null && controller.IsPromptVisible;
    }

    private bool TryGetCurrentLine(out BattleDialogueLine line)
    {
        line = null;
        if (currentDialogue == null || currentDialogue.lines == null)
            return false;
        if (currentLineIndex < 0 || currentLineIndex >= currentDialogue.lines.Length)
            return false;

        line = currentDialogue.lines[currentLineIndex];
        return line != null;
    }

    private void OpenDialogueGate()
    {
        if (!lockBattleInputWhileActive || IsTutorialPromptBlockingDialogue())
            return;

        BattleTutorialGate.OpenPressE();
        ownsInputGate = true;
    }

    private void EnsureDialogueGate()
    {
        if (!lockBattleInputWhileActive || IsTutorialPromptBlockingDialogue())
            return;

        if (!BattleTutorialGate.IsActive)
            OpenDialogueGate();
    }

    private void EndDialogue(bool consumeInputFrame)
    {
        CompleteTyping();

        isDialogueActive = false;
        currentDialogue = null;
        currentLineIndex = -1;
        lineTutorialShown = false;
        advanceTutorialShown = false;
        SetUiVisible(false);

        if (nameText)
            nameText.text = string.Empty;
        if (dialogueText)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        if (consumeInputFrame)
            BattleTutorialGate.MarkInputConsumedThisFrame(BattleTutorialAction.Continue);

        if (ownsInputGate && !IsTutorialPromptBlockingDialogue())
            BattleTutorialGate.Close();
        ownsInputGate = false;
    }

    private void StartTyping(string fullText)
    {
        if (!dialogueText)
            return;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(CoType(fullText ?? string.Empty));
    }

    private IEnumerator CoType(string fullText)
    {
        isTyping = true;
        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int totalChars = dialogueText.textInfo.characterCount;
        if (totalChars <= 0)
        {
            dialogueText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            typingRoutine = null;
            yield break;
        }

        float interval = 1f / Mathf.Max(1f, charactersPerSecond);
        for (int i = 0; i < totalChars; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;
            yield return new WaitForSecondsRealtime(interval);

            if (punctuationExtraDelay > 0f)
            {
                char c = dialogueText.textInfo.characterInfo[i].character;
                if (c == '.' || c == '!' || c == '?' || c == '…')
                    yield return new WaitForSecondsRealtime(punctuationExtraDelay);
                else if (c == ',' || c == '，')
                    yield return new WaitForSecondsRealtime(punctuationExtraDelay * 0.5f);
            }
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        typingRoutine = null;
    }

    private void CompleteTyping()
    {
        if (!dialogueText)
            return;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    private void ResolveRefs()
    {
        if (!targetCanvas)
            targetCanvas = GetComponentInParent<Canvas>();
        if (!targetCanvas)
            targetCanvas = FindObjectOfType<Canvas>(true);

        if (createUiIfMissing && (!root || !window || !dialogueText))
            CreateRuntimeUi();

        if (root && !rootGroup)
            rootGroup = root.GetComponent<CanvasGroup>();
        if (root && !window)
            window = root.Find("Window") as RectTransform;
        if (window && !nameText)
            nameText = window.Find("NameText")?.GetComponent<TMP_Text>();
        if (window && !dialogueText)
            dialogueText = window.Find("DialogueText")?.GetComponent<TMP_Text>();
        if (window && !leftPortraitImage)
            leftPortraitImage = window.Find("LeftPortrait")?.GetComponent<Image>();
        if (window && !rightPortraitImage)
            rightPortraitImage = window.Find("RightPortrait")?.GetComponent<Image>();
    }

    private void CreateRuntimeUi()
    {
        if (!targetCanvas)
            return;

        if (!root)
        {
            var rootObject = new GameObject("BattleDialogueUI", typeof(RectTransform), typeof(CanvasGroup));
            rootObject.transform.SetParent(targetCanvas.transform, false);
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            rootGroup = rootObject.GetComponent<CanvasGroup>();
        }

        if (!window)
        {
            var windowObject = new GameObject("Window", typeof(RectTransform), typeof(Image));
            windowObject.transform.SetParent(root, false);
            window = windowObject.GetComponent<RectTransform>();
            window.anchorMin = window.anchorMax = new Vector2(0.5f, 0f);
            window.pivot = new Vector2(0.5f, 0f);
            window.anchoredPosition = windowAnchoredPosition;
            window.sizeDelta = windowSize;

            Image windowImage = windowObject.GetComponent<Image>();
            windowImage.color = windowColor;
        }

        if (!leftPortraitImage)
            leftPortraitImage = CreatePortrait("LeftPortrait", new Vector2(28f, 0.5f), new Vector2(170f, 170f));
        if (!rightPortraitImage)
            rightPortraitImage = CreatePortrait("RightPortrait", new Vector2(-28f, 0.5f), new Vector2(170f, 170f), true);

        if (!nameText)
        {
            var nameObject = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObject.transform.SetParent(window, false);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.offsetMin = new Vector2(220f, -64f);
            nameRect.offsetMax = new Vector2(-220f, -18f);

            nameText = nameObject.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.fontSize = nameFontSize;
            nameText.color = nameTextColor;
            nameText.raycastTarget = false;
        }

        if (!dialogueText)
        {
            var textObject = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(window, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(220f, 26f);
            textRect.offsetMax = new Vector2(-220f, -72f);

            dialogueText = textObject.GetComponent<TextMeshProUGUI>();
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.enableWordWrapping = true;
            dialogueText.fontSize = dialogueFontSize;
            dialogueText.color = dialogueTextColor;
            dialogueText.raycastTarget = false;
        }

        root.SetAsLastSibling();
    }

    private Image CreatePortrait(string objectName, Vector2 edgeOffset, Vector2 size, bool rightSide = false)
    {
        var portraitObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(window, false);
        RectTransform rect = portraitObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rightSide ? new Vector2(1f, edgeOffset.y) : new Vector2(0f, edgeOffset.y);
        rect.pivot = rightSide ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(edgeOffset.x, 0f);
        rect.sizeDelta = size;

        Image image = portraitObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image;
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

    private void ApplyPortraits(Sprite left, Sprite right, PortraitFocus focus)
    {
        if (leftPortraitImage)
        {
            leftPortraitImage.gameObject.SetActive(left != null);
            leftPortraitImage.sprite = left;
        }
        if (rightPortraitImage)
        {
            rightPortraitImage.gameObject.SetActive(right != null);
            rightPortraitImage.sprite = right;
        }

        float leftAlpha = activePortraitAlpha;
        float rightAlpha = activePortraitAlpha;
        if (dimInactivePortrait)
        {
            if (focus == PortraitFocus.Left)
                rightAlpha = inactivePortraitAlpha;
            else if (focus == PortraitFocus.Right)
                leftAlpha = inactivePortraitAlpha;
        }

        if (leftPortraitImage)
            SetAlpha(leftPortraitImage, leftAlpha);
        if (rightPortraitImage)
            SetAlpha(rightPortraitImage, rightAlpha);
    }

    private void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
