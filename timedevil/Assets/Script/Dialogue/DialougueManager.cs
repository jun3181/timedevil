using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    public delegate void DialogueManagerEventHandler();
    public static event DialogueManagerEventHandler OnDialogueEnd;
    public static event DialogueManagerEventHandler OnDialogueStart;

    [Header("UI Root")]
    public GameObject uiRoot;            // 예: UI_Dialogue/Panel
    public TMP_Text nameText;            // Name Text
    public TMP_Text dialogueText;        // Dialogue Text (TMP)

    [Header("Portraits (2개)")]
    public Image leftPortraitImage;
    public Image rightPortraitImage;

    [Header("Panel Style")]
    [SerializeField] private bool useMainMenuPanelStyle = true;
    [SerializeField] private Image dialoguePanelImage;
    [SerializeField] private Color dialoguePanelColor = new Color(0f, 0f, 0f, 0.78f);

    [Header("Dialogue Text Layout")]
    [SerializeField] private bool fitDialogueTextToPanel = true;
    [SerializeField] private Vector2 dialogueTextInsetMin = new Vector2(90f, 42f);
    [SerializeField] private Vector2 dialogueTextInsetMax = new Vector2(-170f, -70f);
    [SerializeField, Min(1f)] private float dialogueTextFontSize = 100f;

    [Header("Advance Prompt")]
    [SerializeField] private bool showAdvancePrompt = true;
    [SerializeField] private Image advancePromptImage;
    [SerializeField] private Sprite advancePromptSprite;
    [SerializeField] private string advancePromptObjectName = "Keyboard_UI_01_22";
    [SerializeField] private Vector2 advancePromptSize = new Vector2(52f, 52f);
    [SerializeField, Min(0.1f)] private float advancePromptScale = 3f;
    [SerializeField] private Vector2 advancePromptOffset = new Vector2(-26f, 18f);
    [Range(0f, 1f)][SerializeField] private float advancePromptMinAlpha = 0.25f;
    [Range(0f, 1f)][SerializeField] private float advancePromptMaxAlpha = 1f;
    [Min(0.05f)][SerializeField] private float advancePromptBlinkPeriod = 0.9f;

    [Header("Dimming")]
    public bool dimInactive = true;
    [Range(0f, 1f)] public float activeAlpha = 1f;
    [Range(0f, 1f)] public float inactiveAlpha = 0.35f;

    [Header("Typewriter")]
    public bool useTypewriter = true;
    [Tooltip("초당 표시 글자 수")]
    public float charactersPerSecond = 35f;
    [Tooltip("문장부호에서 추가 대기(리듬)")]
    public float punctuationExtraDelay = 0.10f;
    [Tooltip("새 문장이 시작된 직후 입력 무시 시간(초). 같은 프레임 입력으로 타이핑이 즉시 완료되는 문제를 방지")]
    public float lineAdvanceLockSeconds = 0.08f;

    [Header("State")]
    public bool isDialogueActive = false;

    [Tooltip("컷씬에서 사용: true면 '월드 입력(일반 E 호출)'은 대사 넘김이 막힘. 컷씬 컨트롤러는 ignore로 우회 가능.")]
    public bool blockInput = false;

    private readonly Queue<DialogueLine> _queue = new();
    private string _defaultName;
    private Sprite _currentLeft;
    private Sprite _currentRight;

    // typing state
    private Coroutine _typingCo;
    private bool _isTyping = false;
    private float _nextAdvanceAllowedUnscaledTime = 0f;
    private Coroutine _advancePromptBlinkCo;

    // =========================
    // Cutscene API (추가)
    // =========================
    public bool IsTyping => _isTyping;

    /// <summary>컷씬 컨트롤러가 타이핑 즉시 완성할 때 사용</summary>
    public void ForceCompleteTyping() => CompleteTyping();

    /// <summary>컷씬 컨트롤러 전용: blockInput 무시하고 다음 줄 표시</summary>
    public void Cutscene_DisplayNextSentence()
    {
        DisplayNextSentence(ignoreBlockInput: true);
    }

    /// <summary>컷씬 종료 시 강제로 UI 닫기</summary>
    public void EndDialogueExternal() => EndDialogue();

    // =========================

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyDialoguePanelStyle();
        ApplyDialogueTextLayout();
        EnsureAdvancePrompt();
        HideAdvancePrompt();

        if (uiRoot) uiRoot.SetActive(false);
    }

    private void OnDisable()
    {
        StopAdvancePromptBlink();
    }

    public void StartDialogue(Dialogue dialogue)
    {
        if (dialogue == null) return;

        ApplyDialoguePanelStyle();
        ApplyDialogueTextLayout();
        EnsureAdvancePrompt();

        // UI ON
        if (uiRoot) uiRoot.SetActive(true);
        ShowAdvancePrompt();

        isDialogueActive = true;

        // blockInput은 컷씬/외부가 소유(여기서 강제로 false로 바꾸지 않음)

        _queue.Clear();

        _defaultName = dialogue.name;
        _currentLeft = dialogue.leftPortrait;
        _currentRight = dialogue.rightPortrait;

        // lines 우선
        if (dialogue.lines != null && dialogue.lines.Length > 0)
        {
            foreach (var line in dialogue.lines)
            {
                if (string.IsNullOrWhiteSpace(line.text)) continue;
                _queue.Enqueue(line);
            }
        }
        else
        {
            // legacy sentences
            if (dialogue.sentences != null)
            {
                foreach (var s in dialogue.sentences)
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    _queue.Enqueue(new DialogueLine
                    {
                        text = s,
                        speakerName = _defaultName,
                        leftPortrait = null,
                        rightPortrait = null,
                        focus = PortraitFocus.None
                    });
                }
            }
        }
        OnDialogueStart?.Invoke();

        //  기본 동작: 첫 줄 출력 시도
        // 컷씬에서는 blockInput=true로 해두면 여기서 막혀서 "대기(큐만 채움)" 상태가 됨.
        DisplayNextSentence(ignoreBlockInput: false);
    }

    /// <summary>
    /// E 입력은 PlayerMainManager가 여기만 호출.
    /// - 타이핑 중: 즉시 완성
    /// - 타이핑 끝: 다음 문장
    /// - 더 없음: 종료
    ///
    ///  컷씬 중에는 blockInput=true로 막아두고,
    /// 컷씬 컨트롤러가 ignoreBlockInput=true로 우회 호출한다.
    /// </summary>
    public void DisplayNextSentence(bool ignoreBlockInput = false)
    {
        if (!isDialogueActive) return;

        //  컷씬 중, 월드 입력(일반 호출) 차단
        if (blockInput && !ignoreBlockInput) return;

        // 새 문장 시작 직후에는 같은 키 입력/같은 프레임 재호출을 잠깐 무시
        if (!ignoreBlockInput && Time.unscaledTime < _nextAdvanceAllowedUnscaledTime)
            return;

        // 1) 타이핑 중이면: "다음"이 아니라 "즉시 완성"
        if (_isTyping)
        {
            CompleteTyping();
            return;
        }

        // 2) 다음 줄이 없으면 종료
        if (_queue.Count == 0)
        {
            EndDialogue();
            return;
        }

        // 3) 다음 줄 적용
        var line = _queue.Dequeue();

        string speaker = string.IsNullOrEmpty(line.speakerName) ? _defaultName : line.speakerName;
        if (nameText) nameText.text = speaker;

        // 초상 업데이트(줄에서 override가 있으면 갱신)
        if (line.leftPortrait != null) _currentLeft = line.leftPortrait;
        if (line.rightPortrait != null) _currentRight = line.rightPortrait;

        ApplyPortraits(_currentLeft, _currentRight, line.focus);

        // 텍스트 출력(타이핑)
        _nextAdvanceAllowedUnscaledTime = Time.unscaledTime + Mathf.Max(0f, lineAdvanceLockSeconds);

        if (dialogueText)
        {
            if (!useTypewriter)
            {
                dialogueText.text = line.text;
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
            else
            {
                StartTyping(line.text);
            }
        }
    }

    private void StartTyping(string fullText)
    {
        if (!dialogueText) return;

        if (_typingCo != null) StopCoroutine(_typingCo);
        _typingCo = StartCoroutine(CoTypeTMP(fullText));
    }

    private IEnumerator CoTypeTMP(string fullText)
    {
        _isTyping = true;

        // TMP는 전체 텍스트를 넣고 maxVisibleCharacters로 조절(리치텍스트 안전)
        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = 0;

        // 글자 수 확정
        dialogueText.ForceMeshUpdate();
        int totalChars = dialogueText.textInfo.characterCount;

        if (totalChars <= 0)
        {
            dialogueText.maxVisibleCharacters = int.MaxValue;
            _isTyping = false;
            _typingCo = null;
            yield break;
        }

        float interval = (charactersPerSecond <= 0f) ? 0f : (1f / charactersPerSecond);

        for (int i = 0; i < totalChars; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;

            // 기본 속도
            if (interval > 0f)
                yield return new WaitForSecondsRealtime(interval);

            // 문장부호 리듬(선택)
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

        _isTyping = false;
        _typingCo = null;
    }

    private void CompleteTyping()
    {
        if (!dialogueText) return;

        if (_typingCo != null)
        {
            StopCoroutine(_typingCo);
            _typingCo = null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        _isTyping = false;
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

        float lA = activeAlpha, rA = activeAlpha;

        if (dimInactive)
        {
            if (focus == PortraitFocus.Left) { lA = activeAlpha; rA = inactiveAlpha; }
            else if (focus == PortraitFocus.Right) { lA = inactiveAlpha; rA = activeAlpha; }
        }

        if (leftPortraitImage) SetAlpha(leftPortraitImage, lA);
        if (rightPortraitImage) SetAlpha(rightPortraitImage, rA);
    }

    private void ApplyDialoguePanelStyle()
    {
        if (!useMainMenuPanelStyle) return;

        Image panelImage = ResolveDialoguePanelImage();
        if (!panelImage) return;

        panelImage.sprite = null;
        panelImage.overrideSprite = null;
        panelImage.type = Image.Type.Simple;
        panelImage.preserveAspect = false;
        panelImage.color = dialoguePanelColor;
    }

    private Image ResolveDialoguePanelImage()
    {
        if (dialoguePanelImage) return dialoguePanelImage;
        if (!uiRoot) return null;

        Transform panelTransform = uiRoot.transform.Find("Panel");
        if (panelTransform && panelTransform.TryGetComponent(out dialoguePanelImage))
            return dialoguePanelImage;

        foreach (Image image in uiRoot.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject.name != "Panel") continue;
            dialoguePanelImage = image;
            return dialoguePanelImage;
        }

        if (uiRoot.TryGetComponent(out dialoguePanelImage))
            return dialoguePanelImage;

        return null;
    }

    private void ApplyDialogueTextLayout()
    {
        if (!fitDialogueTextToPanel || !dialogueText)
            return;

        Image panelImage = ResolveDialoguePanelImage();
        RectTransform textRect = dialogueText.rectTransform;

        if (panelImage != null && textRect.parent != panelImage.transform)
            textRect.SetParent(panelImage.transform, false);

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = dialogueTextInsetMin;
        textRect.offsetMax = dialogueTextInsetMax;
        textRect.localScale = Vector3.one;

        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.enableWordWrapping = true;
        dialogueText.raycastTarget = false;
        dialogueText.margin = Vector4.zero;
        dialogueText.fontSize = Mathf.Max(1f, dialogueTextFontSize);
    }

    private void EnsureAdvancePrompt()
    {
        if (!showAdvancePrompt || !uiRoot) return;

        if (!advancePromptImage)
            advancePromptImage = FindAdvancePromptImage();

        if (!advancePromptSprite)
            advancePromptSprite = LoadDefaultAdvancePromptSprite();

        if (!advancePromptImage && advancePromptSprite)
            CreateAdvancePromptImage();

        if (!advancePromptImage) return;

        if (!advancePromptImage.sprite && advancePromptSprite)
            advancePromptImage.sprite = advancePromptSprite;

        ApplyAdvancePromptLayout();

        advancePromptImage.preserveAspect = true;
        advancePromptImage.raycastTarget = false;
    }

    private Image FindAdvancePromptImage()
    {
        if (!uiRoot) return null;

        foreach (Image image in uiRoot.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject.name == advancePromptObjectName)
                return image;
        }

        return null;
    }

    private void CreateAdvancePromptImage()
    {
        Transform parent = null;
        Image panelImage = ResolveDialoguePanelImage();
        if (panelImage)
            parent = panelImage.transform;

        if (!parent && uiRoot)
            parent = uiRoot.transform;

        if (!parent) return;

        var promptObject = new GameObject(advancePromptObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        promptObject.transform.SetParent(parent, false);
        promptObject.transform.SetAsLastSibling();

        advancePromptImage = promptObject.GetComponent<Image>();
        advancePromptImage.sprite = advancePromptSprite;
    }

    private void ApplyAdvancePromptLayout()
    {
        RectTransform rect = advancePromptImage ? advancePromptImage.rectTransform : null;
        if (!rect) return;

        Image panelImage = ResolveDialoguePanelImage();
        if (panelImage && rect.parent != panelImage.transform)
            rect.SetParent(panelImage.transform, false);

        rect.SetAsLastSibling();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = advancePromptOffset;
        rect.sizeDelta = advancePromptSize;
        rect.localScale = Vector3.one * Mathf.Max(0.1f, advancePromptScale);
    }

    private Sprite LoadDefaultAdvancePromptSprite()
    {
        if (advancePromptSprite)
            return advancePromptSprite;

#if UNITY_EDITOR
        const string keyboardSpriteSheetPath = "Assets/ElvGames/Fantasy Dreamland/UI/Keyboard Keys/Keyboard_UI_01.png";
        UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(keyboardSpriteSheetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == "Keyboard_UI_01_22")
                return sprite;
        }
#endif

        return null;
    }

    private void ShowAdvancePrompt()
    {
        if (!showAdvancePrompt) return;

        EnsureAdvancePrompt();
        if (!advancePromptImage) return;

        advancePromptImage.gameObject.SetActive(true);
        SetAlpha(advancePromptImage, advancePromptMaxAlpha);
        StartAdvancePromptBlink();
    }

    private void HideAdvancePrompt()
    {
        StopAdvancePromptBlink();

        if (!advancePromptImage) return;

        SetAlpha(advancePromptImage, advancePromptMaxAlpha);
        advancePromptImage.gameObject.SetActive(false);
    }

    private void StartAdvancePromptBlink()
    {
        StopAdvancePromptBlink();

        if (!advancePromptImage || !isActiveAndEnabled) return;

        _advancePromptBlinkCo = StartCoroutine(CoBlinkAdvancePrompt());
    }

    private void StopAdvancePromptBlink()
    {
        if (_advancePromptBlinkCo == null) return;

        StopCoroutine(_advancePromptBlinkCo);
        _advancePromptBlinkCo = null;
    }

    private IEnumerator CoBlinkAdvancePrompt()
    {
        float minAlpha = Mathf.Clamp01(advancePromptMinAlpha);
        float maxAlpha = Mathf.Clamp01(Mathf.Max(minAlpha, advancePromptMaxAlpha));
        float period = Mathf.Max(0.05f, advancePromptBlinkPeriod);

        while (advancePromptImage && advancePromptImage.gameObject.activeInHierarchy)
        {
            float t = Mathf.PingPong(Time.unscaledTime / period, 1f);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(advancePromptImage, Mathf.Lerp(maxAlpha, minAlpha, eased));
            yield return null;
        }

        _advancePromptBlinkCo = null;
    }

    private void SetAlpha(Image img, float a)
    {
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    private void EndDialogue()
    {
        // 타이핑 중이면 정리
        CompleteTyping();
        HideAdvancePrompt();

        isDialogueActive = false;
        _queue.Clear();

        if (uiRoot) uiRoot.SetActive(false);
        if (nameText) nameText.text = "";

        if (dialogueText)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        OnDialogueEnd?.Invoke();
    }
}
