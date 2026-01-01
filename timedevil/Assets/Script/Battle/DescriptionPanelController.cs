using TMPro;
using UnityEngine;

public class DescriptionPanelController : MonoBehaviour
{
    [Header("Target UI")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("Sources")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private HandUI hand;
    [SerializeField] private CardDatabaseSO database;

    [Header("Enemy Hand (for End focus view)")]
    [SerializeField] private EnemyHandUI enemyHand;           // 👈 추가
    [SerializeField] private CanvasGroup enemyHandCanvasGroup; // (선택) 적 손패용 CG

    [Header("Messages")]
    [TextArea] public string msgCard = "Card를 선택합니다.";
    [TextArea] public string msgItem = "Item을 선택합니다.";
    [TextArea] public string msgEnd = "턴엔드합니다.";
    [TextArea] public string msgRun = "도망칩니다.";
    [TextArea] public string msgEnemyTurn = "상대턴입니다."; // 적턴 고정 안내

    [Header("Optional Refs")]
    [SerializeField] private CanvasGroup handCanvasGroup;
    [SerializeField] private bool clearOnAwake = true;
    [SerializeField] private bool logDebug = false;

    private int _lastIndex = -1;
    private bool _forceEnemyTurn = false;   // TurnManager에서 on/off
    private string _forcedMessage = null;   // 👈 발동 중(explanation) 임시 고정 문구
    private bool _forcePlayerDiscard = false; // 🔸 강제 버림 모드

    // ⬇️ 클래스 필드에 추가
    private bool _spectate = false;                    // 관전 플래그
    private Faction _spectateSide = Faction.Enemy;     // 관전 시 보여줄 손패 쪽


    void Reset()
    {
        if (!descriptionText) descriptionText = GetComponentInChildren<TMP_Text>(true);
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!hand) hand = FindObjectOfType<HandUI>(true);
        if (!enemyHand) enemyHand = FindObjectOfType<EnemyHandUI>(true);                 // 👈 추가

    }

    void Awake()
    {
        if (!descriptionText) descriptionText = GetComponentInChildren<TMP_Text>(true);
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!hand) hand = FindObjectOfType<HandUI>(true);
        if (!enemyHand) enemyHand = FindObjectOfType<EnemyHandUI>(true);                 // 👈 추가

    }

    void OnEnable()
    {
        if (menu) menu.onFocusChanged.AddListener(OnMenuFocusChanged);
        if (hand != null)
        {
            hand.onSelectModeChanged += _ => RefreshNow();
            hand.onSelectIndexChanged += _ => RefreshNow();
        }
        _lastIndex = menu ? menu.Index : 0;
        RefreshNow();
    }

    void OnDisable()
    {
        if (menu) menu.onFocusChanged.RemoveListener(OnMenuFocusChanged);
        if (hand != null)
        {
            hand.onSelectModeChanged -= _ => RefreshNow();
            hand.onSelectIndexChanged -= _ => RefreshNow();
        }
    }

    void Start()
    {
        if (clearOnAwake && descriptionText) descriptionText.text = string.Empty;
    }

    void Update()
    {
        if (menu && menu.Index != _lastIndex)
        {
            _lastIndex = menu.Index;
            RefreshNow();
        }
    }
    // ⬇️ 공개 API 추가
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

    // TurnManager가 EnemyTurn 시작/종료 때 호출
    public void SetEnemyTurn(bool on)
    {
        _forceEnemyTurn = on;

        // 적턴이면 손패 UI 숨김, 아니라면 현재 메뉴 인덱스 기준으로 토글
        if (hand != null)
        {
            if (on) hand.HideCards();
            else
            {
                int idx = menu ? menu.Index : 0;
                if (idx == 0) hand.ShowCards(); else hand.HideCards();
            }
        }
        // ✅ 적 턴엔 EnemyHand 표시, 플레이어 턴엔 나머지 로직(RefreshNow)에서 결정
        if (enemyHand != null)
        {
            if (on) enemyHand.ShowAll();
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

    // 👇 카드 발동(관전 모드) 동안 임시 문구를 고정 표시
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

    private void RefreshNow()
    {
        if (!descriptionText) return;

        int index = menu ? menu.Index : 0;


        // ✅ 0) 관전 모드가 최우선
        if (_spectate)
        {
            // 보여줄 쪽만 ON, 나머지는 OFF (클릭/레이캐스트 모두 차단)
            if (_spectateSide == Faction.Player)
            {
                if (handCanvasGroup) { handCanvasGroup.alpha = 1f; handCanvasGroup.interactable = false; handCanvasGroup.blocksRaycasts = false; }
                if (hand) hand.ShowCards();

                if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 0f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }
                if (enemyHand) enemyHand.HideAll();
            }
            else // Enemy
            {
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
            if (handCanvasGroup) { handCanvasGroup.alpha = 0f; handCanvasGroup.interactable = false; handCanvasGroup.blocksRaycasts = false; }
            if (hand) hand.HideCards();

            if (enemyHand) enemyHand.ShowAll();
            if (enemyHandCanvasGroup) { enemyHandCanvasGroup.alpha = 1f; enemyHandCanvasGroup.interactable = false; enemyHandCanvasGroup.blocksRaycasts = false; }

            descriptionText.text = !string.IsNullOrEmpty(_forcedMessage) ? _forcedMessage : msgEnemyTurn;
            return;
        }

        // 2) 강제 버림 페이즈: PlayerHand 항상 ON, EnemyHand OFF
        if (_forcePlayerDiscard)
        {
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

        // 3) 평상시(플레이어 턴, 버림 페이즈 아님): 메뉴 인덱스 기반
        // PlayerHand: Card(0)에서만 표시
        if (handCanvasGroup)
        {
            bool showHand = (index == 0);
            handCanvasGroup.alpha = showHand ? 1f : 0f;
            handCanvasGroup.interactable = showHand;
            handCanvasGroup.blocksRaycasts = showHand;
        }
        if (hand != null)
        {
            if (index == 0) hand.ShowCards();
            else hand.HideCards();
        }

        // EnemyHand: End(2)에서만 표시
        if (enemyHand != null)
        {
            bool showEnemy = (index == 2);
            if (showEnemy) enemyHand.ShowAll(); else enemyHand.HideAll();
            if (enemyHandCanvasGroup)
            {
                enemyHandCanvasGroup.alpha = showEnemy ? 1f : 0f;
                enemyHandCanvasGroup.interactable = false;
                enemyHandCanvasGroup.blocksRaycasts = false;
            }
        }

        // ★★★ 텍스트 결정부: 강제 문구가 있으면 항상 최우선으로 사용 ★★★
        string text;
        if (!string.IsNullOrEmpty(_forcedMessage))
        {
            text = _forcedMessage;                         // 관전모드/연출 중 설명 고정
        }
        else if (index == 0 && hand != null && hand.IsInSelectMode)
        {
            text = GetCurrentCardDisplay() ?? msgCard;     // 선택 모드 설명
        }
        else
        {
            text = index switch
            {
                0 => msgCard,
                1 => msgItem,
                2 => msgEnd,
                3 => msgRun,
                _ => string.Empty
            };
        }
        descriptionText.text = text;
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
