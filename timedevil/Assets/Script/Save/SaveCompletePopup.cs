using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SaveCompletePopup : MonoBehaviour
{
    private const string RuntimeObjectName = "SaveCompletePopupRuntime";
    private static readonly Vector2 ReferenceResolution = new Vector2(1040f, 720f);
    private static SaveCompletePopup s_instance;

    private Canvas _canvas;
    private CanvasGroup _group;
    private TMP_Text _messageText;
    private TMP_Text _closeKeyText;
    private KeyCode _closeKey = KeyCode.E;
    private bool _isVisible;
    private bool _waitForCloseKeyRelease;
    private bool _heldActionLock;

    public static void Show(
        string message,
        KeyCode closeKey,
        TMP_FontAsset font,
        bool lockPlayerInput)
    {
        SaveCompletePopup popup = EnsureInstance();
        popup.ShowInternal(message, closeKey, font, lockPlayerInput);
    }

    private static SaveCompletePopup EnsureInstance()
    {
        if (s_instance != null)
            return s_instance;

        var root = new GameObject(RuntimeObjectName, typeof(RectTransform), typeof(SaveCompletePopup));
        s_instance = root.GetComponent<SaveCompletePopup>();
        return s_instance;
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        EnsureUi(null);
        HideImmediate();
    }

    private void Update()
    {
        if (!_isVisible || _closeKey == KeyCode.None)
            return;

        if (_waitForCloseKeyRelease)
        {
            if (!Input.GetKey(_closeKey))
                _waitForCloseKeyRelease = false;

            return;
        }

        if (Input.GetKeyDown(_closeKey))
            Hide();
    }

    private void OnDisable()
    {
        ReleaseActionLockIfHeld();
    }

    private void OnDestroy()
    {
        ReleaseActionLockIfHeld();

        if (s_instance == this)
            s_instance = null;
    }

    private void ShowInternal(
        string message,
        KeyCode closeKey,
        TMP_FontAsset font,
        bool lockPlayerInput)
    {
        EnsureUi(font);

        _closeKey = closeKey;
        _messageText.text = string.IsNullOrWhiteSpace(message) ? "저장완료!" : message;
        _closeKeyText.text = closeKey == KeyCode.None ? string.Empty : closeKey.ToString().ToUpperInvariant();

        TMP_FontAsset resolvedFont = ResolveFont(font);
        ApplyFont(_messageText, resolvedFont);
        ApplyFont(_closeKeyText, resolvedFont);

        _group.alpha = 1f;
        _group.interactable = true;
        _group.blocksRaycasts = true;
        _isVisible = true;
        _waitForCloseKeyRelease = closeKey != KeyCode.None && Input.GetKey(closeKey);

        if (lockPlayerInput && !_heldActionLock && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            _heldActionLock = true;
        }
    }

    private void Hide()
    {
        HideImmediate();
        ReleaseActionLockIfHeld();
    }

    private void HideImmediate()
    {
        if (_group != null)
        {
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        _isVisible = false;
        _waitForCloseKeyRelease = false;
    }

    private void ReleaseActionLockIfHeld()
    {
        if (!_heldActionLock)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.UnlockAction();

        _heldActionLock = false;
    }

    private void EnsureUi(TMP_FontAsset font)
    {
        if (_canvas != null && _group != null && _messageText != null && _closeKeyText != null)
            return;

        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 6000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        _group = GetComponent<CanvasGroup>();
        if (_group == null)
            _group = gameObject.AddComponent<CanvasGroup>();

        RectTransform rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        RectTransform frame = CreateImage("WindowFrame", transform, Color.white).rectTransform;
        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = new Vector2(0f, 50f);
        frame.sizeDelta = new Vector2(590f, 160f);

        RectTransform window = CreateImage("Window", frame, Color.black).rectTransform;
        window.anchorMin = Vector2.zero;
        window.anchorMax = Vector2.one;
        window.offsetMin = new Vector2(5f, 5f);
        window.offsetMax = new Vector2(-5f, -5f);

        _messageText = CreateText("Message", window, TextAlignmentOptions.Center, 26f, FontStyles.Bold);
        RectTransform messageRect = _messageText.rectTransform;
        messageRect.anchorMin = Vector2.zero;
        messageRect.anchorMax = Vector2.one;
        messageRect.offsetMin = new Vector2(36f, 44f);
        messageRect.offsetMax = new Vector2(-36f, -44f);

        _closeKeyText = CreateText("CloseKey", window, TextAlignmentOptions.BottomRight, 27f, FontStyles.Bold);
        RectTransform keyRect = _closeKeyText.rectTransform;
        keyRect.anchorMin = new Vector2(1f, 0f);
        keyRect.anchorMax = new Vector2(1f, 0f);
        keyRect.pivot = new Vector2(1f, 0f);
        keyRect.anchoredPosition = new Vector2(-24f, 22f);
        keyRect.sizeDelta = new Vector2(90f, 80f);

        TMP_FontAsset resolvedFont = ResolveFont(font);
        ApplyFont(_messageText, resolvedFont);
        ApplyFont(_closeKeyText, resolvedFont);
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        TextAlignmentOptions alignment,
        float fontSize,
        FontStyles fontStyle)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.color = Color.white;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static TMP_FontAsset ResolveFont(TMP_FontAsset preferred)
    {
        if (preferred != null)
            return preferred;

        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (s_instance != null && texts[i] != null && texts[i].transform.IsChildOf(s_instance.transform))
                continue;

            if (texts[i] != null && texts[i].font != null)
                return texts[i].font;
        }

        return TMP_Settings.defaultFontAsset;
    }

    private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text != null && font != null)
            text.font = font;
    }
}
