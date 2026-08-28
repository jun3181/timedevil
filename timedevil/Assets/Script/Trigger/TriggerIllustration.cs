using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TriggerStep_IllustrationPanel_New : TriggerStepBase
{
    [System.Serializable]
    private class IllustrationFrame
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private Dialogue dialogue;

        public Sprite Sprite => sprite;
        public Dialogue Dialogue => dialogue;
    }

    [Header("Illustration UI")]
    [FormerlySerializedAs("panel")]
    [SerializeField] private GameObject illustrationRoot;
    [FormerlySerializedAs("image")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private bool autoCreateImageIfMissing = true;

    [Header("Illustration Sequence")]
    [SerializeField] private List<IllustrationFrame> sequence = new();

    [Header("Illustration Fade")]
    [SerializeField] private bool useFadeTransition = true;
    [Min(0f)] [SerializeField] private float sequenceFadeOutDuration = 0.15f;
    [Min(0f)] [SerializeField] private float sequenceFadeInDuration = 0.15f;

    [Header("Dialogue UI")]
    [SerializeField] private bool showDialoguePanel = true;
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

    [Header("Dark Overlay")]
    [SerializeField] private bool useDarkOverlay = false;
    [Range(0f, 1f)]
    [SerializeField] private float darkOverlayAlpha = 0.65f;
    [Min(0f)] [SerializeField] private float darkOverlayInDuration = 0.15f;
    [SerializeField] private bool restoreDarkOverlayOnClose = true;
    [Min(0f)] [SerializeField] private float darkOverlayOutDuration = 0.15f;

    private bool _isOpen;
    private bool _locked;
    private Coroutine _autoClose;
    private DialogueManager _activeDialogueManager;
    private bool _startedDialogue;
    private bool _ownsDialogueBlockInput;
    private bool _previousDialogueBlockInput;
    private DarkOverlay _darkOverlay;
    private bool _storedDarkOverlayAlpha;
    private float _previousDarkOverlayAlpha;
    private readonly List<IllustrationFrame> _runtimeSequence = new();
    private int _sequenceIndex;
    private bool _currentFrameHadDialogue;
    private bool _isTransitioning;
    private float _visibleImageAlpha = 1f;
    private bool _hasStoredImageAlpha;
    private const string AutoImageName = "IllustrationImage";

    private void Reset()
    {
        if (illustrationRoot == null) illustrationRoot = gameObject;
    }

    private void OnDisable() => CloseImmediate();
    private void OnDestroy() => CloseImmediate();

    public override IEnumerator Execute(TriggerContext ctx)
    {
        PrepareRuntimeSequence();
        ResolveMissingReferences();

        if (illustrationRoot == null && illustrationImage == null)
        {
            Debug.LogWarning("[TriggerStep_IllustrationPanel_New] illustration UI is null.", this);
            yield break;
        }

        if (_isOpen)
            yield break;

        Open();

        if (ShouldFadeIllustration())
            yield return CoInitialFadeIn();

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
                yield return CoCloseWithOptionalFade();
                yield break;
            }

            if (closeWithKey && !_isTransitioning && Input.GetKeyDown(closeKey))
                yield return CoHandleAdvanceInput();

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

        ApplyDarkOverlay();

        _sequenceIndex = 0;
        _currentFrameHadDialogue = false;
        StoreImageVisibleAlpha();

        if (messageText != null)
            messageText.text = message;

        if (illustrationRoot != null)
            illustrationRoot.SetActive(true);
        else if (illustrationImage != null)
            illustrationImage.gameObject.SetActive(true);

        PrepareDialoguePanel();
        _isOpen = true;
        EnterCurrentIllustration();
        SetIllustrationAlpha(ShouldFadeIllustration() ? 0f : _visibleImageAlpha);

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
        _isTransitioning = false;

        if (_autoClose != null)
        {
            StopCoroutine(_autoClose);
            _autoClose = null;
        }

        CloseDialoguePanel();
        RestoreDarkOverlay();

        if (illustrationRoot != null)
            illustrationRoot.SetActive(false);
        else if (illustrationImage != null)
            illustrationImage.gameObject.SetActive(false);

        RestoreImageVisibleAlpha();
        _isOpen = false;

        if (_locked && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
            _locked = false;
        }
    }

    private void ApplyDarkOverlay()
    {
        if (!useDarkOverlay)
            return;

        _darkOverlay = DarkOverlay.Instance;
        if (_darkOverlay == null)
            return;

        _previousDarkOverlayAlpha = _darkOverlay.Alpha;
        _storedDarkOverlayAlpha = true;
        _darkOverlay.SetAlpha(darkOverlayAlpha, darkOverlayInDuration);
    }

    private void RestoreDarkOverlay()
    {
        if (!_storedDarkOverlayAlpha)
            return;

        if (restoreDarkOverlayOnClose && _darkOverlay != null)
            _darkOverlay.SetAlpha(_previousDarkOverlayAlpha, darkOverlayOutDuration);

        _darkOverlay = null;
        _storedDarkOverlayAlpha = false;
    }

    private void ResolveMissingReferences()
    {
        if (illustrationRoot == null && illustrationImage != null)
            illustrationRoot = illustrationImage.gameObject;

        if (illustrationImage == null && autoCreateImageIfMissing && HasIllustrationContent())
            illustrationImage = ResolveOrCreateIllustrationImage();

        if (showDialoguePanel && _activeDialogueManager == null && DialogueManager.instance != null)
            _activeDialogueManager = DialogueManager.instance;
    }

    private void PrepareRuntimeSequence()
    {
        _runtimeSequence.Clear();

        if (sequence != null)
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                IllustrationFrame frame = sequence[i];
                if (frame != null && frame.Sprite != null)
                    _runtimeSequence.Add(frame);
            }
        }
    }

    private bool HasIllustrationContent()
    {
        if (sequence == null)
            return false;

        for (int i = 0; i < sequence.Count; i++)
        {
            IllustrationFrame frame = sequence[i];
            if (frame != null && frame.Sprite != null)
                return true;
        }

        return false;
    }

    private bool HasNextIllustration()
    {
        return _sequenceIndex + 1 < _runtimeSequence.Count;
    }

    private IEnumerator CoHandleAdvanceInput()
    {
        if (TryAdvanceCurrentDialogue())
            yield break;

        if (HasNextIllustration())
            yield return CoAdvanceIllustration();
        else
            yield return CoCloseWithOptionalFade();
    }

    private IEnumerator CoAdvanceIllustration()
    {
        _isTransitioning = true;

        if (ShouldFadeIllustration())
            yield return CoFadeIllustrationAlpha(0f, sequenceFadeOutDuration);

        if (_isOpen && HasNextIllustration())
        {
            _sequenceIndex++;
            EnterCurrentIllustration();

            if (ShouldFadeIllustration())
                yield return CoFadeIllustrationAlpha(_visibleImageAlpha, sequenceFadeInDuration);
            else
                SetIllustrationAlpha(_visibleImageAlpha);
        }

        _isTransitioning = false;
    }

    private IEnumerator CoInitialFadeIn()
    {
        _isTransitioning = true;
        yield return CoFadeIllustrationAlpha(_visibleImageAlpha, sequenceFadeInDuration);
        _isTransitioning = false;
    }

    private IEnumerator CoCloseWithOptionalFade()
    {
        if (ShouldFadeIllustration())
        {
            _isTransitioning = true;
            yield return CoFadeIllustrationAlpha(0f, sequenceFadeOutDuration);
        }

        CloseImmediate();
    }

    private bool ShouldFadeIllustration()
    {
        return useFadeTransition && illustrationImage != null;
    }

    private void EnterCurrentIllustration()
    {
        _startedDialogue = false;
        _currentFrameHadDialogue = false;
        ApplyCurrentIllustration();
        StartCurrentFrameDialogue();
    }

    private void ApplyCurrentIllustration()
    {
        if (illustrationImage == null)
            return;

        illustrationImage.sprite = GetCurrentIllustrationSprite();
    }

    private Sprite GetCurrentIllustrationSprite()
    {
        if (_runtimeSequence.Count == 0)
            return null;

        int safeIndex = Mathf.Clamp(_sequenceIndex, 0, _runtimeSequence.Count - 1);
        return _runtimeSequence[safeIndex].Sprite;
    }

    private Dialogue GetCurrentDialogue()
    {
        if (_runtimeSequence.Count == 0)
            return null;

        int safeIndex = Mathf.Clamp(_sequenceIndex, 0, _runtimeSequence.Count - 1);
        IllustrationFrame frame = _runtimeSequence[safeIndex];
        return frame != null ? frame.Dialogue : null;
    }

    private bool TryAdvanceCurrentDialogue()
    {
        if (_activeDialogueManager == null || !_activeDialogueManager.isDialogueActive)
            return false;

        if (_activeDialogueManager.IsTyping)
            _activeDialogueManager.ForceCompleteTyping();
        else
            _activeDialogueManager.DisplayNextSentence(ignoreBlockInput: true);

        return true;
    }

    private void StartCurrentFrameDialogue()
    {
        if (!showDialoguePanel)
            return;

        Dialogue frameDialogue = GetCurrentDialogue();
        if (frameDialogue == null)
            return;

        DialogueManager manager = ResolveDialogueManager();
        if (manager == null)
            return;

        OwnDialogueBlockInput(manager);

        manager.blockInput = false;
        manager.StartDialogue(frameDialogue);
        manager.blockInput = true;
        _startedDialogue = true;
        _currentFrameHadDialogue = manager.isDialogueActive;
    }

    private DialogueManager ResolveDialogueManager()
    {
        if (_activeDialogueManager == null)
            _activeDialogueManager = DialogueManager.instance;

        return _activeDialogueManager;
    }

    private void OwnDialogueBlockInput(DialogueManager manager)
    {
        if (manager == null)
            return;

        if (_ownsDialogueBlockInput && _activeDialogueManager == manager)
            return;

        RestoreDialogueBlockInput();

        _activeDialogueManager = manager;
        _previousDialogueBlockInput = manager.blockInput;
        _ownsDialogueBlockInput = true;
    }

    private IEnumerator CoFadeIllustrationAlpha(float targetAlpha, float duration)
    {
        if (illustrationImage == null)
            yield break;

        if (duration <= 0f)
        {
            SetIllustrationAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = illustrationImage.color.a;
        float elapsed = 0f;

        while (_isOpen && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetIllustrationAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        if (_isOpen)
            SetIllustrationAlpha(targetAlpha);
    }

    private void StoreImageVisibleAlpha()
    {
        _visibleImageAlpha = 1f;
        _hasStoredImageAlpha = false;

        if (illustrationImage == null)
            return;

        _visibleImageAlpha = illustrationImage.color.a;
        _hasStoredImageAlpha = true;
    }

    private void RestoreImageVisibleAlpha()
    {
        if (!_hasStoredImageAlpha)
            return;

        SetIllustrationAlpha(_visibleImageAlpha);
        _hasStoredImageAlpha = false;
    }

    private void SetIllustrationAlpha(float alpha)
    {
        if (illustrationImage == null)
            return;

        Color color = illustrationImage.color;
        color.a = alpha;
        illustrationImage.color = color;
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

    private void PrepareDialoguePanel()
    {
        _activeDialogueManager = DialogueManager.instance;
        _startedDialogue = false;
        _ownsDialogueBlockInput = false;
        _previousDialogueBlockInput = false;
    }

    private void CloseDialoguePanel()
    {
        if (closeDialogueOnClose)
        {
            if (_startedDialogue && _activeDialogueManager != null && _activeDialogueManager.isDialogueActive)
                _activeDialogueManager.EndDialogueExternal();
        }

        _startedDialogue = false;
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
        return closeWhenDialogueEnds && _currentFrameHadDialogue && !IsDialogueActive() && !HasNextIllustration();
    }

    private bool IsDialogueActive()
    {
        return _activeDialogueManager != null && _activeDialogueManager.isDialogueActive;
    }
}
