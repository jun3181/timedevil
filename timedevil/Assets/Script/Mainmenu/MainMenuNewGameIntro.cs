using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuNewGameIntro : MonoBehaviour
{
    [Serializable]
    private class IntroStep
    {
        public Sprite image = null;

        [TextArea(2, 6)]
        public string[] dialogueLines = Array.Empty<string>();

        public bool overrideFade = false;
        [Min(0f)] public float fadeOutSeconds = 0.25f;
        [Min(0f)] public float fadeInSeconds = 0.25f;
    }

    [Header("Intro Steps")]
    [Tooltip("Each step shows one image and advances through its dialogue lines before the next image.")]
    [SerializeField] private IntroStep[] introSteps = Array.Empty<IntroStep>();

    [Header("Legacy Intro Data")]
    [SerializeField, HideInInspector] private Sprite[] introImages = new Sprite[2];
    [SerializeField, HideInInspector] private string[] dialogueLines = new string[2] { "...", "..." };

    [Header("Auto UI")]
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private GameObject introRoot;
    [SerializeField] private Image introImage;
    [SerializeField] private bool autoCreateUiIfMissing = true;

    [Header("Dialogue UI")]
    [SerializeField] private bool showDialoguePanel = true;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private string speakerName = "";
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.78f);

    [Header("Image Fade")]
    [SerializeField] private bool useImageFade = true;
    [SerializeField, Min(0f)] private float defaultFadeOutSeconds = 0.25f;
    [SerializeField, Min(0f)] private float defaultFadeInSeconds = 0.25f;
    [SerializeField] private bool fadeInFirstStep = true;
    [SerializeField] private bool fadeOutBeforeFinish = true;

    [Header("Input")]
    [SerializeField] private KeyCode advanceKey = KeyCode.E;

    private int _currentStepIndex = -1;
    private int _currentLineIndex = -1;
    private bool _isPlaying = false;
    private bool _isTransitioning = false;
    private bool _waitingAdvanceKeyRelease = false;
    private Coroutine _transitionCo;
    private Action _onFinished;

    public bool HasPlayableIntro => FindNextStepIndex(-1) >= 0;

    private void Awake()
    {
        if (introRoot != null)
            introRoot.SetActive(false);
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

        if (_waitingAdvanceKeyRelease)
        {
            if (Input.GetKey(advanceKey))
                return;

            _waitingAdvanceKeyRelease = false;
        }

        if (_isTransitioning)
            return;

        if (Input.GetKeyDown(advanceKey))
            Advance();
    }

    private void OnDisable()
    {
        if (_isPlaying)
            HideIntro();
    }

    public void Play(Action onFinished)
    {
        if (_isPlaying)
            return;

        _onFinished = onFinished;

        int firstStepIndex = FindNextStepIndex(-1);
        if (firstStepIndex < 0)
        {
            Finish();
            return;
        }

        if (!ResolveUi())
        {
            Debug.LogWarning("[MainMenuNewGameIntro] Could not create intro UI. Continuing New Game immediately.", this);
            Finish();
            return;
        }

        _isPlaying = true;
        _currentStepIndex = firstStepIndex;
        _currentLineIndex = 0;
        _isTransitioning = false;
        _waitingAdvanceKeyRelease = advanceKey != KeyCode.None && Input.GetKey(advanceKey);

        if (introRoot != null)
        {
            introRoot.SetActive(true);
            introRoot.transform.SetAsLastSibling();
        }

        SetIntroImageAlpha(ShouldFadeInFirstStep() ? 0f : 1f);
        ShowCurrentStep();

        if (ShouldFadeInFirstStep())
            StartCurrentStepFadeIn();
    }

    private void Advance()
    {
        if (_currentLineIndex + 1 < GetDialogueLineCount(_currentStepIndex))
        {
            _currentLineIndex++;
            ShowCurrentDialogueLine();
            return;
        }

        int nextStepIndex = FindNextStepIndex(_currentStepIndex);
        if (nextStepIndex < 0)
        {
            if (ShouldFadeOutBeforeFinish())
            {
                StartFinishFadeOut();
                return;
            }

            Finish();
            return;
        }

        StartStepTransition(nextStepIndex);
    }

    private void ShowCurrentStep()
    {
        if (introImage != null)
        {
            introImage.sprite = GetStepImage(_currentStepIndex);
            introImage.enabled = introImage.sprite != null;
        }

        ShowCurrentDialogueLine();
    }

    private void ShowCurrentDialogueLine()
    {
        if (!showDialoguePanel)
            return;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (nameText != null)
        {
            nameText.text = speakerName;
            nameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(speakerName));
        }

        if (dialogueText != null)
            dialogueText.text = ResolveDialogueText(_currentStepIndex, _currentLineIndex);
    }

    private void Finish()
    {
        HideIntro();

        Action callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();
    }

    private void HideIntro()
    {
        if (_transitionCo != null)
        {
            StopCoroutine(_transitionCo);
            _transitionCo = null;
        }

        _isPlaying = false;
        _isTransitioning = false;
        _currentStepIndex = -1;
        _currentLineIndex = -1;
        _waitingAdvanceKeyRelease = false;

        SetIntroImageAlpha(1f);

        if (dialogueText != null)
            dialogueText.text = "";

        if (nameText != null)
            nameText.text = "";

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (introRoot != null)
            introRoot.SetActive(false);
    }

    private string ResolveDialogueText(int stepIndex, int lineIndex)
    {
        string[] lines = GetStepDialogueLines(stepIndex);

        if (lines != null &&
            lineIndex >= 0 &&
            lineIndex < lines.Length &&
            !string.IsNullOrEmpty(lines[lineIndex]))
        {
            return lines[lineIndex];
        }

        return "...";
    }

    private int GetDialogueLineCount(int stepIndex)
    {
        string[] lines = GetStepDialogueLines(stepIndex);
        return lines == null || lines.Length == 0 ? 1 : lines.Length;
    }

    private int FindNextStepIndex(int afterIndex)
    {
        if (HasStructuredIntroSteps())
        {
            for (int i = afterIndex + 1; i < introSteps.Length; i++)
            {
                if (introSteps[i] != null && introSteps[i].image != null)
                    return i;
            }

            return -1;
        }

        if (introImages == null) return -1;

        for (int i = afterIndex + 1; i < introImages.Length; i++)
        {
            if (introImages[i] != null)
                return i;
        }

        return -1;
    }

    private Sprite GetStepImage(int stepIndex)
    {
        if (HasStructuredIntroSteps())
        {
            if (stepIndex < 0 || stepIndex >= introSteps.Length || introSteps[stepIndex] == null)
                return null;

            return introSteps[stepIndex].image;
        }

        if (introImages == null || stepIndex < 0 || stepIndex >= introImages.Length)
            return null;

        return introImages[stepIndex];
    }

    private string[] GetStepDialogueLines(int stepIndex)
    {
        if (HasStructuredIntroSteps())
        {
            if (stepIndex < 0 || stepIndex >= introSteps.Length || introSteps[stepIndex] == null)
                return null;

            return introSteps[stepIndex].dialogueLines;
        }

        if (dialogueLines == null || stepIndex < 0 || stepIndex >= dialogueLines.Length)
            return null;

        return new[] { dialogueLines[stepIndex] };
    }

    private bool HasStructuredIntroSteps()
    {
        if (introSteps == null) return false;

        foreach (IntroStep step in introSteps)
        {
            if (step != null && step.image != null)
                return true;
        }

        return false;
    }

    private void StartCurrentStepFadeIn()
    {
        if (_transitionCo != null)
            StopCoroutine(_transitionCo);

        _transitionCo = StartCoroutine(CoCurrentStepFadeIn());
    }

    private IEnumerator CoCurrentStepFadeIn()
    {
        _isTransitioning = true;
        yield return CoFadeIntroImage(0f, 1f, ResolveFadeInSeconds(_currentStepIndex));
        _isTransitioning = false;
        _transitionCo = null;
    }

    private void StartStepTransition(int nextStepIndex)
    {
        if (_transitionCo != null)
            StopCoroutine(_transitionCo);

        _transitionCo = StartCoroutine(CoStepTransition(nextStepIndex));
    }

    private IEnumerator CoStepTransition(int nextStepIndex)
    {
        _isTransitioning = true;

        yield return CoFadeIntroImage(GetIntroImageAlpha(), 0f, ResolveFadeOutSeconds(_currentStepIndex));

        _currentStepIndex = nextStepIndex;
        _currentLineIndex = 0;
        ShowCurrentStep();

        yield return CoFadeIntroImage(0f, 1f, ResolveFadeInSeconds(_currentStepIndex));

        _isTransitioning = false;
        _transitionCo = null;
    }

    private void StartFinishFadeOut()
    {
        if (_transitionCo != null)
            StopCoroutine(_transitionCo);

        _transitionCo = StartCoroutine(CoFinishFadeOut());
    }

    private IEnumerator CoFinishFadeOut()
    {
        _isTransitioning = true;
        yield return CoFadeIntroImage(GetIntroImageAlpha(), 0f, ResolveFadeOutSeconds(_currentStepIndex));

        _isTransitioning = false;
        _transitionCo = null;
        Finish();
    }

    private IEnumerator CoFadeIntroImage(float fromAlpha, float toAlpha, float duration)
    {
        if (introImage == null)
            yield break;

        if (!useImageFade || duration <= 0f)
        {
            SetIntroImageAlpha(toAlpha);
            yield break;
        }

        float elapsed = 0f;
        fromAlpha = Mathf.Clamp01(fromAlpha);
        toAlpha = Mathf.Clamp01(toAlpha);
        SetIntroImageAlpha(fromAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetIntroImageAlpha(Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetIntroImageAlpha(toAlpha);
    }

    private bool ShouldFadeInFirstStep()
    {
        return useImageFade && fadeInFirstStep && ResolveFadeInSeconds(_currentStepIndex) > 0f;
    }

    private bool ShouldFadeOutBeforeFinish()
    {
        return useImageFade && fadeOutBeforeFinish && ResolveFadeOutSeconds(_currentStepIndex) > 0f;
    }

    private float ResolveFadeOutSeconds(int stepIndex)
    {
        IntroStep step = GetStructuredStep(stepIndex);
        if (step != null && step.overrideFade)
            return step.fadeOutSeconds;

        return defaultFadeOutSeconds;
    }

    private float ResolveFadeInSeconds(int stepIndex)
    {
        IntroStep step = GetStructuredStep(stepIndex);
        if (step != null && step.overrideFade)
            return step.fadeInSeconds;

        return defaultFadeInSeconds;
    }

    private IntroStep GetStructuredStep(int stepIndex)
    {
        if (!HasStructuredIntroSteps()) return null;
        if (stepIndex < 0 || stepIndex >= introSteps.Length) return null;

        return introSteps[stepIndex];
    }

    private float GetIntroImageAlpha()
    {
        return introImage != null ? introImage.color.a : 1f;
    }

    private void SetIntroImageAlpha(float alpha)
    {
        if (introImage == null) return;

        Color color = introImage.color;
        color.a = Mathf.Clamp01(alpha);
        introImage.color = color;
    }

    private bool ResolveUi()
    {
        if (introImage != null && (!showDialoguePanel || dialoguePanel != null || !autoCreateUiIfMissing))
            return true;

        if (!autoCreateUiIfMissing)
            return introImage != null;

        ResolveParentCanvas();

        if (parentCanvas == null)
            return false;

        if (introRoot == null)
            introRoot = CreateIntroRoot(parentCanvas.transform);

        if (introImage == null)
            introImage = CreateIntroImage(introRoot.transform);

        if (showDialoguePanel && (dialoguePanel == null || dialogueText == null))
            CreateDialogueUi(introRoot.transform);

        return introImage != null;
    }

    private void ResolveParentCanvas()
    {
        if (parentCanvas != null)
            return;

        parentCanvas = FindObjectOfType<Canvas>(true);
        if (parentCanvas != null)
            return;

        GameObject canvasObject = new GameObject("MainMenuIntroCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        parentCanvas = canvasObject.GetComponent<Canvas>();
        parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        parentCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private GameObject CreateIntroRoot(Transform parent)
    {
        GameObject root = new GameObject("NewGameIntroRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();

        RectTransform rect = root.GetComponent<RectTransform>();
        Stretch(rect);

        Image background = root.GetComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = true;
        group.interactable = true;

        root.SetActive(false);
        return root;
    }

    private Image CreateIntroImage(Transform parent)
    {
        GameObject imageObject = new GameObject("Intro Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;

        return image;
    }

    private void CreateDialogueUi(Transform parent)
    {
        GameObject panelObject = new GameObject("UI_Dialogue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        panelObject.transform.SetAsLastSibling();

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.055f);
        panelRect.anchorMax = new Vector2(0.92f, 0.295f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

        dialoguePanel = panelObject;

        nameText = CreateText(panelObject.transform, "Name Text", new Vector2(0.035f, 0.72f), new Vector2(0.97f, 0.94f), 30f, TextAlignmentOptions.Left);
        dialogueText = CreateText(panelObject.transform, "Dialogue Text", new Vector2(0.035f, 0.16f), new Vector2(0.97f, 0.70f), 34f, TextAlignmentOptions.TopLeft);
    }

    private TMP_Text CreateText(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
