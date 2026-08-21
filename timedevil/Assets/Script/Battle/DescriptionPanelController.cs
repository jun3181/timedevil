using TMPro;
using UnityEngine;
using System.Collections;

public class DescriptionPanelController : MonoBehaviour
{
    [Header("Target UI")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("Panel Visibility")]
    [SerializeField] private GameObject descriptionPanelRoot;
    [SerializeField] private bool autoResolveDescriptionPanelRoot = true;
    [SerializeField] private bool hideDescriptionPanelRootWhenItemViewExits = true;
    [SerializeField] private bool animateDescriptionPanelRoot = true;
    [SerializeField] private Vector2 descriptionPanelShownAnchoredPosition = new Vector2(9f, -260f);
    [SerializeField] private float descriptionPanelHiddenY = -680f;
    [SerializeField, Min(0.01f)] private float descriptionPanelRiseDuration = 0.28f;
    [SerializeField] private AnimationCurve descriptionPanelRiseEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool showDescriptionPanelRootInStateView = false;

    [Header("Sources")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private HandUI hand;
    [SerializeField] private CardDatabaseSO database;

    [Header("Enemy Hand (for End focus view)")]
    [SerializeField] private EnemyHandUI enemyHand;           //  추가
    [SerializeField] private CanvasGroup enemyHandCanvasGroup; // (선택) 적 손패용 CG

    [Header("Item Hand")]
    [SerializeField] private ItemHandUI itemHand;
    [SerializeField] private RectTransform itemHandRect;
    [SerializeField] private bool alignItemHandToDescriptionPanelCenterY = true;
    [SerializeField] private bool animateItemHandWithDescriptionPanel = true;

    [Header("Hand Preview Layout")]
    [SerializeField] private RectTransform playerHandRect;
    [SerializeField] private RectTransform enemyHandRect;
    [SerializeField] private bool convertHandPositionsFromSeparatedRoot = true;
    [SerializeField] private string separatedHandRootName = "hand01";
    [SerializeField] private bool panelControllerOwnsPlayerActivePosition = true;
    [SerializeField] private bool enemyTurnControllerOwnsEnemyTurnHand = true;
    [SerializeField] private bool showPlayerHandPreviewOnCardFocus = false;
    [SerializeField] private bool showEnemyHandPreviewOnEndFocus = false;
    [SerializeField] private Vector2 playerPreviewAnchoredPosition = new Vector2(-280f, -250f);
    [SerializeField] private Vector2 enemyPreviewAnchoredPosition = new Vector2(280f, -250f);
    [SerializeField] private Vector2 playerActiveAnchoredPosition = new Vector2(-280f, -385f);
    [SerializeField] private Vector2 enemyActiveAnchoredPosition = new Vector2(280f, -385f);

    [Header("Messages")]
    [TextArea] public string msgCard = "Card를 선택합니다.";
    [TextArea] public string msgItem = "Item을 선택합니다.";
    [TextArea] public string msgState = "상태를 확인합니다.";

    [TextArea] public string msgEnd = "턴엔드합니다.";
    [TextArea] public string msgRun = "도망칩니다.";
    [TextArea] public string msgEnemyTurn = "상대턴입니다."; // 적턴 고정 안내

    [Header("Optional Refs")]
    [SerializeField] private CanvasGroup handCanvasGroup;
    [SerializeField] private bool clearOnAwake = true;
    [SerializeField] private bool logDebug = false;

    private int _lastIndex = -1;
    private bool _forceEnemyTurn = false;   // TurnManager에서 on/off
    private string _forcedMessage = null;   //  발동 중(explanation) 임시 고정 문구
    private bool _forcePlayerDiscard = false; //  강제 버림 모드
    private int _effectLockCount = 0;         // 카드 효과 실행 중 기본 문구 억제
    private bool _stateView = false;
    private bool _stateViewShowPlayerHand = false;
    private bool _stateViewShowEnemyHand = false;
    private string _stateViewMessage = null;
    private bool _itemView = false;
    private string _itemViewMessage = null;
    private RectTransform _descriptionPanelRootRect;
    private Coroutine _descriptionPanelMotionRoutine;
    private bool _descriptionPanelRootShown;

    //  클래스 필드에 추가
    private bool _spectate = false;                    // 관전 플래그
    private Faction _spectateSide = Faction.Enemy;     // 관전 시 보여줄 손패 쪽


    void Reset()
    {
        if (!descriptionText) descriptionText = GetComponentInChildren<TMP_Text>(true);
        ResolveDescriptionPanelRoot();
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!hand) hand = FindObjectOfType<HandUI>(true);
        if (!enemyHand) enemyHand = FindObjectOfType<EnemyHandUI>(true);                 //  추가
        ResolveHandRects();
        ResolveItemHandRefs();

    }

    void Awake()
    {
        if (!descriptionText) descriptionText = GetComponentInChildren<TMP_Text>(true);
        ResolveDescriptionPanelRoot();
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!hand) hand = FindObjectOfType<HandUI>(true);
        if (!enemyHand) enemyHand = FindObjectOfType<EnemyHandUI>(true);                 //  추가
        ResolveHandRects();
        ResolveItemHandRefs();

    }

    void OnEnable()
    {
        if (menu) menu.onFocusChanged.AddListener(OnMenuFocusChanged);
        if (hand != null)
        {
            hand.onSelectModeChanged += OnHandSelectModeChanged;
            hand.onSelectIndexChanged += OnHandSelectIndexChanged;
        }
        _lastIndex = menu ? menu.Index : 0;
        RefreshNow();
    }

    void OnDisable()
    {
        if (menu) menu.onFocusChanged.RemoveListener(OnMenuFocusChanged);
        if (hand != null)
        {
            hand.onSelectModeChanged -= OnHandSelectModeChanged;
            hand.onSelectIndexChanged -= OnHandSelectIndexChanged;
        }

        StopDescriptionPanelMotion();
    }

    void Start()
    {
        if (clearOnAwake && descriptionText) descriptionText.text = string.Empty;
        StartCoroutine(Co_RefreshAfterStartup());
    }

    void Update()
    {
        if (menu && menu.Index != _lastIndex)
        {
            _lastIndex = menu.Index;
            RefreshNow();
        }
    }
    //  공개 API 추가
    public void EnterSpectate(Faction showSide, string message = null)
    {
        _spectate = true;
        _spectateSide = showSide;
        _forcedMessage = message;
        RefreshNow();
    }

    public void ExitSpectate()
    {
        _spectate = false;
        _forcedMessage = null;
        RefreshNow();
    }

    private void OnMenuFocusChanged(int idx)
    {
        _lastIndex = idx;
        RefreshNow();
    }

    private void OnHandSelectModeChanged(bool _)
    {
        RefreshNow();
    }

    private void OnHandSelectIndexChanged(int _)
    {
        RefreshNow();
    }

    // TurnManager가 EnemyTurn 시작/종료 때 호출
    public void SetEnemyTurn(bool on)
    {
        _forceEnemyTurn = on;
        ResolveHandRects();

        // 적턴이면 손패 UI 숨김, 아니라면 현재 메뉴 인덱스 기준으로 토글
        if (hand != null)
        {
            if (on)
            {
                hand.HideCards();
            }
            else
            {
                int idx = menu ? menu.Index : 0;
                if (ShouldShowPlayerHandForMenuFocus(idx)) hand.ShowCards(); else hand.HideCards();
            }
        }
        //  적 턴엔 EnemyHand 표시, 플레이어 턴엔 나머지 로직(RefreshNow)에서 결정
        if (enemyHand != null)
        {
            if (on)
            {
                ShowEnemyHandForEnemyTurn();
            }
            else enemyHand.HideAll();  // 플레이어 턴은 RefreshNow가 End(2)일 때 다시 켜줌
        }
        if (enemyHandCanvasGroup)
        {
            bool showEnemy = on;
            enemyHandCanvasGroup.alpha = showEnemy ? 1f : 0f;
            enemyHandCanvasGroup.interactable = false;
            enemyHandCanvasGroup.blocksRaycasts = false;
        }

        RefreshNow();
    }

    //  카드 발동(관전 모드) 동안 임시 문구를 고정 표시
    public void ShowTemporaryExplanation(string text)
    {
        _forcedMessage = text;
        if (logDebug) Debug.Log($"[DescPanel] forcedMessage ON: {text}");
        RefreshNow();
    }

    public void ClearTemporaryMessage()
    {
        if (logDebug) Debug.Log("[DescPanel] forcedMessage OFF");
        _forcedMessage = null;
        RefreshNow();
    }

    public void ShowOneShotMessage(string text, float seconds = 1.2f)
    {
        StartCoroutine(Co_ShowOneShotMessage(text, seconds));
    }

    public void EnterEffectLock()
    {
        _effectLockCount++;
        RefreshNow();
    }

    public void ExitEffectLock()
    {
        _effectLockCount = Mathf.Max(0, _effectLockCount - 1);
        RefreshNow();
    }

    public bool HasForcedMessage => !string.IsNullOrEmpty(_forcedMessage);

    public void EnterStateView(string message)
    {
        EnterStateView(message, false, Faction.Player);
    }

    public void EnterStateView(string message, bool showHand, Faction handSide)
    {
        bool showPlayerHand = showHand && handSide == Faction.Player;
        bool showEnemyHand = showHand && handSide == Faction.Enemy;
        EnterStateView(message, showPlayerHand, showEnemyHand, handSide);
    }

    public void EnterStateView(string message, bool showPlayerHand, bool showEnemyHand, Faction primaryHandSide)
    {
        _stateView = true;
        _stateViewShowPlayerHand = showPlayerHand;
        _stateViewShowEnemyHand = showEnemyHand;
        _stateViewMessage = message;
        if (showDescriptionPanelRootInStateView)
        {
            SetDescriptionPanelRootVisible(true, true);
            BringDescriptionPanelRootToFront();
        }
        else
        {
            SetDescriptionPanelRootVisible(false);
        }
        RefreshNow();
    }

    public void ExitStateView()
    {
        _stateView = false;
        _stateViewShowPlayerHand = false;
        _stateViewShowEnemyHand = false;
        _stateViewMessage = null;
        RefreshNow();
    }

    public void EnterItemInteractionView(string message = null)
    {
        _itemView = true;
        _itemViewMessage = message;
        SetDescriptionPanelRootVisible(true, true);
        ShowItemHandForItemInteraction();
        RefreshNow();
    }

    public void ExitItemInteractionView()
    {
        _itemView = false;
        _itemViewMessage = null;

        if (hideDescriptionPanelRootWhenItemViewExits)
            SetDescriptionPanelRootVisible(false);

        itemHand?.ExitItemInteractionMode(descriptionPanelRiseDuration, descriptionPanelRiseEase);
        RefreshNow();
    }

    private System.Collections.IEnumerator Co_ShowOneShotMessage(string text, float seconds)
    {
        _forcedMessage = text;
        RefreshNow();
        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));
        _forcedMessage = null;
        RefreshNow();
    }

    private System.Collections.IEnumerator Co_RefreshAfterStartup()
    {
        yield return null;
        RefreshNow();
    }

    private void RefreshNow()
    {
        if (!descriptionText) return;

        int index = menu ? menu.Index : 0;
        int stateIndex = ResolveStateIndex();
        int endIndex = ResolveEndIndex();
        int runIndex = ResolveRunIndex();
        int itemIndex = ResolveItemIndex();


        if (_stateView)
        {
            if (showDescriptionPanelRootInStateView)
            {
                SetDescriptionPanelRootVisible(true);
                BringDescriptionPanelRootToFront();
            }
            else
            {
                SetDescriptionPanelRootVisible(false);
            }

            if (_stateViewShowPlayerHand)
            {
                SetPlayerActivePositionIfOwnedHere();
                if (handCanvasGroup) { handCanvasGroup.alpha = 1f; handCanvasGroup.interactable = false; handCanvasGroup.blocksRaycasts = false; }
                if (hand) hand.ShowCards();
            }
            else
            {
                if (handCanvasGroup) { handCanvasGroup.alpha = 0f; handCanvasGroup.interactable = false; handCanvasGroup.blocksRaycasts = false; }
                if (hand) hand.HideCards();
            }

            if (_stateViewShowEnemyHand)
            {
                SetEnemyHandPosition(enemyActiveAnchoredPosition);
                if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 1f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }
                if (enemyHand) enemyHand.ShowAll();
            }
            else
            {
                if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 0f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }
                if (enemyHand) enemyHand.HideAll();
            }

            descriptionText.text = string.IsNullOrEmpty(_stateViewMessage) ? msgState : _stateViewMessage;
            return;
        }

        //  0) 관전 모드가 최우선
        if (_spectate)
        {
            // 보여줄 쪽만 ON, 나머지는 OFF (클릭/레이캐스트 모두 차단)
            if (_spectateSide == Faction.Player)
            {
                SetPlayerHandPosition(playerActiveAnchoredPosition);
                if (handCanvasGroup) { handCanvasGroup.alpha = 1f; handCanvasGroup.interactable = false; handCanvasGroup.blocksRaycasts = false; }
                if (hand) hand.ShowCards();

                if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 0f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }
                if (enemyHand) enemyHand.HideAll();
            }
            else // Enemy
            {
                SetEnemyHandPosition(enemyActiveAnchoredPosition);
                if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 1f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }
                if (enemyHand) enemyHand.ShowAll();

                if (handCanvasGroup) { handCanvasGroup.alpha = 0f; handCanvasGroup.interactable = false; handCanvasGroup.blocksRaycasts = false; }
                if (hand) hand.HideCards();
            }

            descriptionText.text = !string.IsNullOrEmpty(_forcedMessage) ? _forcedMessage : "";
            return;
        }

        // 1) 적 턴: EnemyHand 항상 ON, PlayerHand OFF
        if (_forceEnemyTurn)
        {
            SetPlayerActivePositionIfOwnedHere();

            if (handCanvasGroup) { handCanvasGroup.alpha = 0f; handCanvasGroup.interactable = false; handCanvasGroup.blocksRaycasts = false; }
            if (hand) hand.HideCards();

            ShowEnemyHandForEnemyTurn();
            if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 1f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }

            descriptionText.text = !string.IsNullOrEmpty(_forcedMessage) ? _forcedMessage : msgEnemyTurn;
            return;
        }

        // 2) 강제 버림 페이즈: PlayerHand 항상 ON, EnemyHand OFF
        if (_forcePlayerDiscard)
        {
            SetPlayerActivePositionIfOwnedHere();

            // EnemyHand 강제 OFF
            if (enemyHand) enemyHand.HideAll();
            if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 0f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }

            // PlayerHand 강제 ON
            if (handCanvasGroup) { handCanvasGroup.alpha = 1f; handCanvasGroup.interactable = true; handCanvasGroup.blocksRaycasts = true; }
            if (hand) hand.ShowCards();

            // 문구 고정(있으면 임시문구, 없으면 기본 안내)
            descriptionText.text = !string.IsNullOrEmpty(_forcedMessage)
                ? _forcedMessage
                : $"손패가 초과되었습니다. 버릴 카드를 선택하세요.";
            return;
        }

        if (_itemView)
        {
            if (handCanvasGroup) { handCanvasGroup.alpha = 0f; handCanvasGroup.interactable = false; handCanvasGroup.blocksRaycasts = false; }
            if (hand) hand.HideCards();

            if (enemyHand) enemyHand.HideAll();
            if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 0f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }

            SetDescriptionPanelRootVisible(true);
            descriptionText.text = !string.IsNullOrEmpty(_itemViewMessage) ? _itemViewMessage : msgItem;
            return;
        }

        // 3) 평상시(플레이어 턴, 버림 페이즈 아님): 메뉴 인덱스 기반
        // PlayerHand: Card(0)에서만 표시
        bool showPlayerHand = ShouldShowPlayerHandForMenuFocus(index);
        if (handCanvasGroup)
        {
            handCanvasGroup.alpha = showPlayerHand ? 1f : 0f;
            handCanvasGroup.interactable = showPlayerHand;
            handCanvasGroup.blocksRaycasts = showPlayerHand;
        }
        if (hand != null)
        {
            if (showPlayerHand)
            {
                if (hand.IsInSelectMode)
                    SetPlayerActivePositionIfOwnedHere();
                else
                    SetPlayerHandPosition(playerPreviewAnchoredPosition);

                hand.ShowCards();
            }
            else hand.HideCards();
        }

        // EnemyHand: End(2)에서만 표시
        if (enemyHand != null)
        {
            bool showEnemy = ShouldShowEnemyHandForMenuFocus(index, endIndex);
            if (showEnemy)
            {
                SetEnemyHandPosition(enemyPreviewAnchoredPosition);
                enemyHand.ShowAll();
            }
            else enemyHand.HideAll();
            if (enemyHandCanvasGroup)
            {
                enemyHandCanvasGroup.alpha = showEnemy ? 1f : 0f;
                enemyHandCanvasGroup.interactable = false;
                enemyHandCanvasGroup.blocksRaycasts = false;
            }
        }

        //  텍스트 결정부: 강제 문구가 있으면 항상 최우선으로 사용 
        string text;
        if (!string.IsNullOrEmpty(_forcedMessage))
        {
            text = _forcedMessage;                         // 관전모드/연출 중 설명 고정
        }
        else if (_effectLockCount > 0)
        {
            text = string.Empty;
        }
        else if (index == 0 && hand != null && hand.IsInSelectMode)
        {
            text = GetCurrentCardDisplay() ?? msgCard;     // 선택 모드 설명
        }
        else
        {
            text = index switch
            {
                0 when hand != null && hand.CardCount <= 0 => "선택가능한 카드가 없습니다.",
                0 => msgCard,
                _ when index == stateIndex => msgState,
                _ when index == itemIndex => msgItem,
                _ when index == endIndex => msgEnd,
                _ when index == runIndex => msgRun,
                _ => string.Empty
            };
        }
        descriptionText.text = text;
    }

    private int ResolveStateIndex()
    {
        int named = FindEntryIndexByName("state");
        if (named >= 0) return named;
        return menu != null && menu.EntryCount >= 5 ? 2 : -1;
    }

    private int ResolveEndIndex()
    {
        int named = FindEntryIndexByName("end");
        if (named >= 0) return named;
        return menu != null && menu.EntryCount >= 5 ? 3 : 2;
    }

    private int ResolveRunIndex()
    {
        int named = FindEntryIndexByName("run");
        if (named >= 0) return named;
        return menu != null && menu.EntryCount >= 5 ? 4 : 3;
    }

    private int ResolveItemIndex()
    {
        int named = FindEntryIndexByName("item");
        if (named >= 0) return named;
        return menu != null && menu.EntryCount >= 5 ? 1 : -1;
    }

    private int FindEntryIndexByName(string token)
    {
        if (menu == null || string.IsNullOrEmpty(token)) return -1;

        for (int i = 0; i < menu.EntryCount; i++)
        {
            GameObject entry = menu.GetEntryObject(i);
            if (entry && entry.name.ToLowerInvariant().Contains(token))
                return i;
        }

        return -1;
    }

    private bool ShouldShowPlayerHandForMenuFocus(int index)
    {
        return hand != null
            && index == 0
            && (hand.IsInSelectMode || showPlayerHandPreviewOnCardFocus);
    }

    private bool ShouldShowEnemyHandForMenuFocus(int index, int endIndex)
    {
        return enemyHand != null
            && endIndex >= 0
            && index == endIndex
            && showEnemyHandPreviewOnEndFocus;
    }

    private void ResolveHandRects()
    {
        if (!hand) hand = FindObjectOfType<HandUI>(true);
        if (!enemyHand) enemyHand = FindObjectOfType<EnemyHandUI>(true);

        if (!playerHandRect && hand) playerHandRect = hand.GetComponent<RectTransform>();
        if (!enemyHandRect && enemyHand) enemyHandRect = enemyHand.GetComponent<RectTransform>();
    }

    private void ResolveItemHandRefs()
    {
        if (!itemHand) itemHand = FindObjectOfType<ItemHandUI>(true);
        if (!itemHandRect && itemHand) itemHandRect = itemHand.GetComponent<RectTransform>();
    }

    private void ShowItemHandForItemInteraction()
    {
        ResolveItemHandRefs();
        RectTransform descriptionRect = GetDescriptionPanelRootRect();
        if (!itemHand || !itemHandRect || !descriptionRect)
            return;

        Vector2 shown = GetDescriptionPanelShownPosition(descriptionRect);
        float targetY = alignItemHandToDescriptionPanelCenterY
            ? ConvertRootYToItemHandLocalY(shown.y)
            : itemHandRect.anchoredPosition.y;
        float hiddenY = ConvertRootYToItemHandLocalY(descriptionPanelHiddenY);
        float duration = animateItemHandWithDescriptionPanel ? descriptionPanelRiseDuration : 0f;
        itemHand.ShowForItemInteraction(targetY, hiddenY, duration, descriptionPanelRiseEase);
    }

    private float ConvertRootYToItemHandLocalY(float rootY)
    {
        if (!itemHandRect)
            return rootY;

        Vector2 current = itemHandRect.anchoredPosition;
        Vector2 converted = HandPositionUtility.ToSeparatedRootLocal(
            itemHandRect,
            new Vector2(current.x, rootY),
            convertHandPositionsFromSeparatedRoot,
            separatedHandRootName);
        return converted.y;
    }

    private void ResolveDescriptionPanelRoot()
    {
        if (descriptionPanelRoot || !autoResolveDescriptionPanelRoot || !descriptionText)
            return;

        Transform parent = descriptionText.transform.parent;
        descriptionPanelRoot = parent ? parent.gameObject : descriptionText.gameObject;
        _descriptionPanelRootRect = descriptionPanelRoot ? descriptionPanelRoot.GetComponent<RectTransform>() : null;
    }

    private void SetDescriptionPanelRootVisible(bool visible, bool animate = false)
    {
        ResolveDescriptionPanelRoot();
        if (!descriptionPanelRoot)
            return;

        RectTransform rect = GetDescriptionPanelRootRect();

        if (visible)
        {
            if (_descriptionPanelRootShown && descriptionPanelRoot.activeSelf)
                return;

            _descriptionPanelRootShown = true;
            descriptionPanelRoot.SetActive(true);

            Vector2 shown = GetDescriptionPanelShownPosition(rect);
            if (!rect || !Application.isPlaying || !animate || !animateDescriptionPanelRoot)
            {
                if (rect) rect.anchoredPosition = shown;
                return;
            }

            StopDescriptionPanelMotion();
            Vector2 hidden = new Vector2(shown.x, descriptionPanelHiddenY);
            rect.anchoredPosition = hidden;
            _descriptionPanelMotionRoutine = StartCoroutine(Co_MoveDescriptionPanelRoot(rect, hidden, shown));
            return;
        }

        _descriptionPanelRootShown = false;
        StopDescriptionPanelMotion();
        if (rect)
        {
            Vector2 shown = GetDescriptionPanelShownPosition(rect);
            rect.anchoredPosition = new Vector2(shown.x, descriptionPanelHiddenY);
        }

        descriptionPanelRoot.SetActive(false);
    }

    private RectTransform GetDescriptionPanelRootRect()
    {
        if (!_descriptionPanelRootRect && descriptionPanelRoot)
            _descriptionPanelRootRect = descriptionPanelRoot.GetComponent<RectTransform>();
        return _descriptionPanelRootRect;
    }

    private void BringDescriptionPanelRootToFront()
    {
        RectTransform rect = GetDescriptionPanelRootRect();
        if (rect)
            rect.SetAsLastSibling();
    }

    private Vector2 GetDescriptionPanelShownPosition(RectTransform rect)
    {
        return descriptionPanelShownAnchoredPosition;
    }

    private IEnumerator Co_MoveDescriptionPanelRoot(RectTransform rect, Vector2 from, Vector2 to)
    {
        float duration = Mathf.Max(0.01f, descriptionPanelRiseDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float eased = descriptionPanelRiseEase != null ? descriptionPanelRiseEase.Evaluate(u) : u;
            if (rect) rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        if (rect) rect.anchoredPosition = to;
        _descriptionPanelMotionRoutine = null;
    }

    private void StopDescriptionPanelMotion()
    {
        if (_descriptionPanelMotionRoutine == null)
            return;

        StopCoroutine(_descriptionPanelMotionRoutine);
        _descriptionPanelMotionRoutine = null;
    }

    private void SetPlayerHandPosition(Vector2 anchoredPosition)
    {
        ResolveHandRects();
        if (playerHandRect)
        {
            playerHandRect.anchoredPosition = HandPositionUtility.ToSeparatedRootLocal(
                playerHandRect,
                anchoredPosition,
                convertHandPositionsFromSeparatedRoot,
                separatedHandRootName);
        }
    }

    private void SetPlayerActivePositionIfOwnedHere()
    {
        if (!panelControllerOwnsPlayerActivePosition)
            SetPlayerHandPosition(playerActiveAnchoredPosition);
    }

    private void SetEnemyHandPosition(Vector2 anchoredPosition)
    {
        ResolveHandRects();
        if (enemyHandRect)
        {
            enemyHandRect.anchoredPosition = HandPositionUtility.ToSeparatedRootLocal(
                enemyHandRect,
                anchoredPosition,
                convertHandPositionsFromSeparatedRoot,
                separatedHandRootName);
        }
    }

    private void ShowEnemyHandForEnemyTurn()
    {
        if (!enemyHand || enemyTurnControllerOwnsEnemyTurnHand)
            return;

        SetEnemyHandPosition(enemyActiveAnchoredPosition);
        enemyHand.ShowAll();
    }


    private string GetCurrentCardDisplay()
    {
        if (database == null || hand == null) return null;

        var ids = hand.VisibleHandIds; // HandUI 스냅샷
        if (ids == null || ids.Count == 0) return null;

        int i = Mathf.Clamp(hand.CurrentSelectIndex, 0, ids.Count - 1);
        string id = ids[i];

        var so = database.GetById(id);
        if (!so)
        {
            if (logDebug) Debug.LogWarning($"[DescPanel] DB miss for id={id}");
            return $"(등록되지 않은 카드: {id})";
        }
        // 선택 모드에서는 display(설명문) 사용
        return string.IsNullOrEmpty(so.display) ? "(설명이 없습니다)" : so.display;
    }

    public void SetPlayerDiscardMode(bool on)
    {
        _forcePlayerDiscard = on;
        RefreshNow();
    }


}
