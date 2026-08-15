using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuNewGameIntro : MonoBehaviour
{
    [Header("Intro Images")]
    [Tooltip("Images shown after New Game. Press E to advance. If empty, New Game loads Myroom immediately.")]
    [SerializeField] private Sprite[] introImages = new Sprite[2];

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
    [SerializeField] private string[] dialogueLines = new string[2] { "...", "..." };
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.78f);

    [Header("Input")]
    [SerializeField] private KeyCode advanceKey = KeyCode.E;

    private int _currentIndex = -1;
    private bool _isPlaying = false;
    private bool _waitingAdvanceKeyRelease = false;
    private Action _onFinished;

    public bool HasPlayableIntro => FindNextImageIndex(-1) >= 0;

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

        int firstIndex = FindNextImageIndex(-1);
        if (firstIndex < 0)
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
        _currentIndex = firstIndex;
        _waitingAdvanceKeyRelease = advanceKey != KeyCode.None && Input.GetKey(advanceKey);

        if (introRoot != null)
        {
            introRoot.SetActive(true);
            introRoot.transform.SetAsLastSibling();
        }

        ShowCurrentStep();
    }

    private void Advance()
    {
        int nextIndex = FindNextImageIndex(_currentIndex);
        if (nextIndex < 0)
        {
            Finish();
            return;
        }

        _currentIndex = nextIndex;
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (introImage != null)
        {
            introImage.sprite = introImages[_currentIndex];
            introImage.enabled = introImage.sprite != null;
        }

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
            dialogueText.text = ResolveDialogueText(_currentIndex);
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
        _isPlaying = false;
        _currentIndex = -1;
        _waitingAdvanceKeyRelease = false;

        if (dialogueText != null)
            dialogueText.text = "";

        if (nameText != null)
            nameText.text = "";

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (introRoot != null)
            introRoot.SetActive(false);
    }

    private string ResolveDialogueText(int imageIndex)
    {
        if (dialogueLines != null &&
            imageIndex >= 0 &&
            imageIndex < dialogueLines.Length &&
            !string.IsNullOrEmpty(dialogueLines[imageIndex]))
        {
            return dialogueLines[imageIndex];
        }

        return "...";
    }

    private int FindNextImageIndex(int afterIndex)
    {
        if (introImages == null)
            return -1;

        for (int i = afterIndex + 1; i < introImages.Length; i++)
        {
            if (introImages[i] != null)
                return i;
        }

        return -1;
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
