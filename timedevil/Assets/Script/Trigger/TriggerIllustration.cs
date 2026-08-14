using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TriggerStep_IllustrationPanel_New : TriggerStepBase
{
    [Header("Illustration UI")]
    [FormerlySerializedAs("panel")]
    [SerializeField] private GameObject illustrationRoot;
    [FormerlySerializedAs("image")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private bool autoCreateImageIfMissing = true;

    [Header("Illustration Content")]
    [FormerlySerializedAs("sprite")]
    [SerializeField] private Sprite illustrationSprite;

    [Header("Dialogue UI")]
    [SerializeField] private bool showDialoguePanel = true;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Dialogue dialogue;
    [SerializeField] private bool closeWhenDialogueEnds = true;
    [SerializeField] private bool closeDialogueOnClose = true;

    [Header("Legacy Text")]
    [FormerlySerializedAs("text")]
    [SerializeField] private TMP_Text messageText;
    [TextArea]
    [SerializeField] private string message;

    [Header("Flow")]
    [SerializeField] private bool closeWithKey = true;
    [SerializeField] private KeyCode closeKey = KeyCode.E;
    [SerializeField] private bool waitUntilClosed = true;
    [Min(0f)] [SerializeField] private float autoCloseDelay = 0f;

    [Header("Player Input")]
    [SerializeField] private bool lockPlayerInput = true;

    private bool _isOpen;
    private bool _locked;
    private Coroutine _autoClose;
    private DialogueManager _activeDialogueManager;
    private bool _startedDialogue;
    private bool _openedDialoguePanelDirectly;
    private bool _ownsDialogueBlockInput;
    private bool _previousDialogueBlockInput;
    private const string AutoImageName = "IllustrationImage";

    private void Reset()
    {
        if (illustrationRoot == null) illustrationRoot = gameObject;
    }

    private void OnDisable() => CloseImmediate();
    private void OnDestroy() => CloseImmediate();

    public override IEnumerator Execute(TriggerContext ctx)
    {
        ResolveMissingReferences();

        if (illustrationRoot == null && illustrationImage == null)
        {
            Debug.LogWarning("[TriggerStep_IllustrationPanel_New] illustration UI is null.", this);
            yield break;
        }

        if (_isOpen)
            yield break;

        Open();

        if (!waitUntilClosed)
            yield break;

        if (!closeWithKey && autoCloseDelay <= 0f && !CanCloseWhenDialogueEnds())
        {
            Debug.LogWarning("[TriggerStep_IllustrationPanel_New] no close condition configured. Closing immediately.", this);
            CloseImmediate();
            yield break;
        }

        while (_isOpen)
        {
            if (CanCloseWhenDialogueEnds())
            {
                CloseImmediate();
                yield break;
            }

            if (closeWithKey && !IsDialogueActive() && Input.GetKeyDown(closeKey))
                CloseImmediate();

            yield return null;
        }
    }

    private void Open()
    {
        if (lockPlayerInput && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            _locked = true;
        }

        if (illustrationImage != null)
            illustrationImage.sprite = illustrationSprite;

        if (messageText != null)
            messageText.text = message;

        if (illustrationRoot != null)
            illustrationRoot.SetActive(true);
        else if (illustrationImage != null)
            illustrationImage.gameObject.SetActive(true);

        OpenDialoguePanel();
        _isOpen = true;

        if (autoCloseDelay > 0f)
            _autoClose = StartCoroutine(CoAutoClose());
    }

    private IEnumerator CoAutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (_isOpen)
            CloseImmediate();
    }

    private void CloseImmediate()
    {
        if (_autoClose != null)
        {
            StopCoroutine(_autoClose);
            _autoClose = null;
        }

        CloseDialoguePanel();

        if (illustrationRoot != null)
            illustrationRoot.SetActive(false);
        else if (illustrationImage != null)
            illustrationImage.gameObject.SetActive(false);

        _isOpen = false;

        if (_locked && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
            _locked = false;
        }
    }

    private void ResolveMissingReferences()
    {
        if (illustrationRoot == null && illustrationImage != null)
            illustrationRoot = illustrationImage.gameObject;

        if (illustrationImage == null && autoCreateImageIfMissing && illustrationSprite != null)
            illustrationImage = ResolveOrCreateIllustrationImage();

        if (showDialoguePanel && dialoguePanel == null && DialogueManager.instance != null)
            dialoguePanel = DialogueManager.instance.uiRoot;
    }

    private Image ResolveOrCreateIllustrationImage()
    {
        if (illustrationRoot != null)
        {
            Transform existing = FindChildRecursive(illustrationRoot.transform, AutoImageName);
            if (existing != null && existing.TryGetComponent(out Image existingImage))
                return existingImage;
        }

        GameObject parentRoot = ResolveIllustrationRoot();
        if (parentRoot == null)
            return null;

        GameObject imageObject = new GameObject(AutoImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parentRoot.transform, false);
        imageObject.transform.SetAsLastSibling();

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image image = imageObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        return image;
    }

    private GameObject ResolveIllustrationRoot()
    {
        if (IsUsableUiRoot(illustrationRoot))
            return illustrationRoot;

        GameObject root = new GameObject("Runtime Illustration Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        root.SetActive(false);
        illustrationRoot = root;
        return illustrationRoot;
    }

    private static bool IsUsableUiRoot(GameObject root)
    {
        if (root == null)
            return false;

        return root.GetComponent<RectTransform>() != null || root.GetComponent<Canvas>() != null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void OpenDialoguePanel()
    {
        if (!showDialoguePanel)
            return;

        _activeDialogueManager = DialogueManager.instance;
        _startedDialogue = false;
        _openedDialoguePanelDirectly = false;
        _ownsDialogueBlockInput = false;
        _previousDialogueBlockInput = false;

        if (_activeDialogueManager != null && dialogue != null)
        {
            _previousDialogueBlockInput = _activeDialogueManager.blockInput;
            _activeDialogueManager.blockInput = false;
            _ownsDialogueBlockInput = true;

            _activeDialogueManager.StartDialogue(dialogue);
            _startedDialogue = true;
            return;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            _openedDialoguePanelDirectly = true;
        }
    }

    private void CloseDialoguePanel()
    {
        if (closeDialogueOnClose)
        {
            if (_startedDialogue && _activeDialogueManager != null && _activeDialogueManager.isDialogueActive)
                _activeDialogueManager.EndDialogueExternal();
            else if (_openedDialoguePanelDirectly && dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        _startedDialogue = false;
        _openedDialoguePanelDirectly = false;
        RestoreDialogueBlockInput();
        _activeDialogueManager = null;
    }

    private void RestoreDialogueBlockInput()
    {
        if (_ownsDialogueBlockInput && _activeDialogueManager != null)
            _activeDialogueManager.blockInput = _previousDialogueBlockInput;

        _ownsDialogueBlockInput = false;
        _previousDialogueBlockInput = false;
    }

    private bool CanCloseWhenDialogueEnds()
    {
        return closeWhenDialogueEnds && _startedDialogue && !IsDialogueActive();
    }

    private bool IsDialogueActive()
    {
        return _activeDialogueManager != null && _activeDialogueManager.isDialogueActive;
    }
}
