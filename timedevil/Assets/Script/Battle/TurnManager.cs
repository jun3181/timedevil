using UnityEngine;
using UnityEngine.SceneManagement;

public enum TurnState { PlayerTurn, EnemyTurn }

public class TurnManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Persisted flags (Intro / Gate)  **v2 keys**
    // ─────────────────────────────────────────
    private const string PREF_KEY_MOVE_TUTORIAL_SEEN_V2 = "Move_Tutorial_v2_IntroSeen";      // 인트로 1회
    private const string PREF_KEY_MOVE_TUTORIAL_GATE_SEEN_V2 = "Move_Tutorial_v2_GateSeen";  // 게이트 1회
    private const string PREF_KEY_MOVE_TUTORIAL_V2_MIGRATED = "Move_Tutorial_v2_Migrated";   // 빌드당 1회 초기화 마커

    private static bool s_MoveTutorialSeenThisSession = false;
    private static bool s_MoveTutorialGateSeenThisSession = false;

    [Header("Move_Tutorial Intro")]
    [SerializeField] private bool moveTutorialIntro = true;
    [SerializeField] private bool forceIntroThisRun = false; // 디버그용(이 실행에서만 강제 노출)
    [SerializeField, TextArea] private string introMsg1 = "넌 여기서 사라져야해...";
    [SerializeField, TextArea] private string introMsg2 = "일단.... 무서워..... 피해야해...!!";
    [SerializeField] private float introMsg1Seconds = 1.2f;
    [SerializeField] private float introMsg2Seconds = 1.2f;
    [SerializeField] private bool introRequireKey = false;
    [SerializeField] private KeyCode introKey = KeyCode.E;

    [Header("Intro SFX (optional)")]
    [SerializeField] private AudioClip introSfx1;
    [SerializeField, Range(0f, 1f)] private float introSfx1Volume = 1f;
    [SerializeField] private AudioClip introSfx2;
    [SerializeField, Range(0f, 1f)] private float introSfx2Volume = 1f;

    [Header("Debug / One-shot reset for this build")]
    [Tooltip("체크하면 이번 실행에서만 v2 키를 한 번 초기화하여 인트로/게이트가 다시 1회 노출됩니다.")]
    [SerializeField] private bool resetIntroGateOnceOnThisBuild = false;

    private bool tutorialIntroPlayed = false;
    private static bool IsMoveTutorial() => SceneManager.GetActiveScene().name == "Move_Tutorial";
    public static TurnManager Instance;

    // --- Move_Tutorial 전용 게이트 ---
    [Header("Move_Tutorial Gate")]
    [SerializeField] private bool moveTutorialGate = true;
    [SerializeField] private float postEnemyWait = 3f;
    [SerializeField] private KeyCode continueKey = KeyCode.E;
    [TextArea][SerializeField] private string gateMsg1 = "이 공격들을 피한다고....?(E키눌러서 계속)";
    [TextArea][SerializeField] private string gateMsg2 = "역시 너는 이 세상에 있으면 안돼...";

    [Header("Gate SFX (optional)")]
    [SerializeField] private AudioClip gateSfx1;
    [SerializeField, Range(0f, 1f)] private float gateSfx1Volume = 1f;
    [SerializeField] private AudioClip gateSfx2;
    [SerializeField, Range(0f, 1f)] private float gateSfx2Volume = 1f;

    [Header("Optional UI Controller")]
    [SerializeField] private BattleMenuController menu;

    [Header("Refs")]
    [SerializeField] private EnemyTurnController enemyTurnController;
    [SerializeField] private HandUI handUI;
    [SerializeField] private CostController cost;
    [SerializeField] private DescriptionPanelController desc;
    [SerializeField] private BattleDeckRuntime deck;

    [Header("Delays")]
    [SerializeField] private float enemyThinkDelay = 0.6f;
    [SerializeField] private EnemyHandUI enemyHandUI;
    [SerializeField] private EnemyDeckRuntime enemyDeck;
    [SerializeField] private ItemHandUI itemHand;
    [SerializeField] private float enemyDiscardRevealDelay = 3f;
    [SerializeField] private CardAnimeController cardAnime;

    private bool playerInitialRevealDone = false;
    private bool enemyInitialRevealDone = false;

    public bool IsPlayerDiscardPhase { get; private set; } = false;
    public TurnState currentTurn { get; private set; } = TurnState.PlayerTurn;

    private PlayerDataRuntime pdr;
    private EnemyRuntime enemyRt;
    private int playerSPD = 0;
    private int enemySPD = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!enemyTurnController) enemyTurnController = FindObjectOfType<EnemyTurnController>(true);
        if (!handUI) handUI = FindObjectOfType<HandUI>(true);
        if (!cost) cost = FindObjectOfType<CostController>(true);
        if (!desc) desc = FindObjectOfType<DescriptionPanelController>(true);
        if (!deck) deck = BattleDeckRuntime.Instance ?? FindObjectOfType<BattleDeckRuntime>(true);
        if (!enemyHandUI) enemyHandUI = FindObjectOfType<EnemyHandUI>(true);
        if (!enemyDeck) enemyDeck = EnemyDeckRuntime.Instance ?? FindObjectOfType<EnemyDeckRuntime>(true);
        if (!itemHand) itemHand = FindObjectOfType<ItemHandUI>(true);

        // (A) 이번 빌드에서 한 번만 초기화하고 싶다면 인스펙터 체크
        if (resetIntroGateOnceOnThisBuild && PlayerPrefs.GetInt(PREF_KEY_MOVE_TUTORIAL_V2_MIGRATED, 0) == 0)
        {
            PlayerPrefs.DeleteKey(PREF_KEY_MOVE_TUTORIAL_SEEN_V2);
            PlayerPrefs.DeleteKey(PREF_KEY_MOVE_TUTORIAL_GATE_SEEN_V2);
            PlayerPrefs.SetInt(PREF_KEY_MOVE_TUTORIAL_V2_MIGRATED, 1);
            PlayerPrefs.Save();
            Debug.LogWarning("[TurnManager] v2 intro/gate keys cleared once for this build.");
        }

        // (B) 저장된 플래그를 세션 캐시에 반영 (v2 키 기준)
        if (PlayerPrefs.GetInt(PREF_KEY_MOVE_TUTORIAL_SEEN_V2, 0) == 1)
            s_MoveTutorialSeenThisSession = true;
        if (PlayerPrefs.GetInt(PREF_KEY_MOVE_TUTORIAL_GATE_SEEN_V2, 0) == 1)
            s_MoveTutorialGateSeenThisSession = true;
    }

    void Start()
    {
        pdr = FindObjectOfType<PlayerDataRuntime>(true);
        enemyRt = EnemyRuntime.Instance ?? FindObjectOfType<EnemyRuntime>(true);

        ResolvePlayerData();
        ResolveEnemyData();

        // ▶ Move_Tutorial이면 인트로 우선 검사 (한 번만)
        if (IsMoveTutorial() && moveTutorialIntro && ShouldPlayIntroNow())
        {
            Debug.Log("[TurnManager] Move_Tutorial intro start");
            StartCoroutine(Co_MoveTutorialIntroBoot());
            return; // 인트로가 끝날 때까지 턴 진행 금지
        }

        // 그 외: 정상 시작
        DecideFirstTurn();
    }

    // ─────────────────────────────────────────
    // Intro / Gate 표시 판단 (v2 keys)
    // ─────────────────────────────────────────
    private bool ShouldPlayIntroNow()
    {
        if (forceIntroThisRun) return true;          // 테스트용
        if (tutorialIntroPlayed) return false;       // 이미 재생 시작
        bool seenGlobally = (PlayerPrefs.GetInt(PREF_KEY_MOVE_TUTORIAL_SEEN_V2, 0) == 1);
        bool seenSession = s_MoveTutorialSeenThisSession;
#if UNITY_EDITOR
        Debug.Log($"[TurnManager] Intro check: global={seenGlobally}, session={seenSession}, play={!(seenGlobally || seenSession)}");
#endif
        return !(seenGlobally || seenSession);
    }

    private bool ShouldPlayGateNow()
    {
        bool seenGlobally = (PlayerPrefs.GetInt(PREF_KEY_MOVE_TUTORIAL_GATE_SEEN_V2, 0) == 1);
        bool seenSession = s_MoveTutorialGateSeenThisSession;
#if UNITY_EDITOR
        Debug.Log($"[TurnManager] Gate  check: global={seenGlobally}, session={seenSession}, play={!(seenGlobally || seenSession)}");
#endif
        return !(seenGlobally || seenSession);
    }

    void ResolvePlayerData()
    {
        if (pdr && pdr.Data != null) playerSPD = Mathf.Max(0, pdr.Data.speed);
        else { playerSPD = 0; Debug.LogWarning("[TurnManager] PlayerDataRuntime/Data 없음 → SPD=0"); }
    }

    void ResolveEnemyData()
    {
        if (enemyRt != null) enemySPD = Mathf.Max(0, enemyRt.speed);
        else { enemySPD = 0; Debug.LogWarning("[TurnManager] EnemyRuntime 없음 → SPD=0"); }
    }

    void DecideFirstTurn()
    {
        Debug.Log($"[TurnManager] SPD Compare => Player:{playerSPD} vs Enemy:{enemySPD}");
        if (enemySPD > playerSPD) BeginEnemyTurn();
        else BeginPlayerTurn();
    }

    public void BeginPlayerTurn()
    {
        currentTurn = TurnState.PlayerTurn;
        IsPlayerDiscardPhase = false;

        if (cost) cost.ResetTurn();
        if (deck) deck.DrawOneIfNeeded();

        if (handUI) handUI.ShowCards();
        if (menu) menu.EnableInput(true);
        if (desc) { desc.SetEnemyTurn(false); desc.SetPlayerDiscardMode(false); }

        if (enemyHandUI) enemyHandUI.HideAll();
        if (itemHand) itemHand.SetEnemyTurn(false);

        if (!playerInitialRevealDone && cardAnime != null)
        {
            playerInitialRevealDone = true;
            StartCoroutine(Co_RevealPlayerInitialAfterFrame());
        }

        Debug.Log("🔷 플레이어 턴 시작");
    }

    public void BeginEnemyTurn()
    {
        if (itemHand) itemHand.SetEnemyTurn(true);

        currentTurn = TurnState.EnemyTurn;
        IsPlayerDiscardPhase = false;

        if (cost) cost.ResetTurn();

        if (menu) menu.EnableInput(false);
        if (handUI) handUI.HideCards();
        if (desc) { desc.SetEnemyTurn(true); desc.SetPlayerDiscardMode(false); }

        if (enemyHandUI) { enemyHandUI.gameObject.SetActive(true); enemyHandUI.RebuildFromHand(); }

        if (!enemyInitialRevealDone && cardAnime != null)
        {
            enemyInitialRevealDone = true;
            StartCoroutine(Co_RevealEnemyInitialAfterFrame());
        }

        Debug.Log("🔶 적 턴 시작");
        StartCoroutine(Co_RunEnemyTurnThenBack());
    }

    System.Collections.IEnumerator Co_RunEnemyTurnThenBack()
    {
        if (enemyTurnController)
            yield return enemyTurnController.RunTurn();

        // 적 손패 초과 자동 버림
        if (enemyDeck != null && cardAnime != null)
        {
            int over = enemyDeck.OverCapCount;
            if (over > 0)
            {
                yield return cardAnime.DiscardLastNCards(
                    Faction.Enemy,
                    n: over,
                    fromRight: true,
                    afterAnimDataOp: () => enemyDeck.DiscardExcessToBottom(fromRight: true)
                );

                if (enemyDiscardRevealDelay > 0f)
                    yield return new WaitForSeconds(enemyDiscardRevealDelay);
            }
        }
        else
        {
            int dumped = 0;
            if (enemyDeck != null)
            {
                dumped = enemyDeck.DiscardExcessToBottom(fromRight: true);
                if (dumped > 0 && enemyHandUI) enemyHandUI.RebuildFromHand();
                if (dumped > 0 && enemyDiscardRevealDelay > 0f)
                    yield return new WaitForSeconds(enemyDiscardRevealDelay);
            }
        }

        Debug.Log("🔶 적 턴 종료");

        // ★ 게이트도 "첫 방문 1회만"
        if (moveTutorialGate && IsMoveTutorial() && ShouldPlayGateNow())
        {
            if (menu) menu.EnableInput(false);
            yield return StartCoroutine(Co_MoveTutorialGate());
            yield break; // 게이트 코루틴 안에서 BeginPlayerTurn 호출
        }

        BeginPlayerTurn();
    }

    public void OnPlayerPressedEnd()
    {
        if (currentTurn != TurnState.PlayerTurn) return;

        if (deck == null || deck.OverCapCount <= 0)
        {
            OnPlayerActionCommitted();
            return;
        }

        IsPlayerDiscardPhase = true;

        if (menu) menu.EnableInput(false);
        if (handUI)
        {
            handUI.ShowCards();
            handUI.EnterSelectMode();
        }

        if (desc)
        {
            desc.SetPlayerDiscardMode(true);
            desc.ShowTemporaryExplanation($"손패가 {deck.MaxHandSize}장을 초과했습니다. 버릴 카드를 선택하세요. (남은 초과: {deck.OverCapCount})");
        }
        Debug.Log($"[TurnManager] DiscardPhase 시작 — 초과 {deck.OverCapCount}");
    }

    public void OnPlayerDiscardOne(int remainingOver)
    {
        if (!IsPlayerDiscardPhase) return;

        if (remainingOver > 0)
        {
            if (desc)
                desc.ShowTemporaryExplanation($"버릴 카드를 계속 선택하세요. (남은 초과: {remainingOver})");
            return;
        }

        IsPlayerDiscardPhase = false;
        if (desc)
        {
            desc.ClearTemporaryMessage();
            desc.SetPlayerDiscardMode(false);
        }

        if (handUI) handUI.ExitSelectMode();
        OnPlayerActionCommitted();
    }

    public void OnPlayerActionCommitted()
    {
        if (currentTurn != TurnState.PlayerTurn) return;
        Debug.Log("[TurnManager] Player action committed → EnemyTurn");
        BeginEnemyTurn();
    }

    private System.Collections.IEnumerator Co_RevealPlayerInitialAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        if (cardAnime != null) cardAnime.RevealInitialPlayerHand();
    }

    private System.Collections.IEnumerator Co_RevealEnemyInitialAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        if (cardAnime != null) cardAnime.RevealInitialEnemyHand();
    }

    private System.Collections.IEnumerator Co_MoveTutorialGate()
    {
        if (postEnemyWait > 0f)
            yield return new WaitForSeconds(postEnemyWait);

        // 게이트 1: 문구 + 사운드
        if (desc) desc.ShowTemporaryExplanation(gateMsg1);
        PlaySfx(gateSfx1, gateSfx1Volume);

        while (!Input.GetKeyDown(continueKey)) yield return null;
        yield return null; while (Input.GetKey(continueKey)) yield return null;

        // 게이트 2: 문구 + 사운드
        if (desc) desc.ShowTemporaryExplanation(gateMsg2);
        PlaySfx(gateSfx2, gateSfx2Volume);

        while (!Input.GetKeyDown(continueKey)) yield return null;
        yield return null; while (Input.GetKey(continueKey)) yield return null;

        if (desc) desc.ClearTemporaryMessage();

        // ▶ 게이트 1회 완료 플래그 저장 (v2)
        s_MoveTutorialGateSeenThisSession = true;
        PlayerPrefs.SetInt(PREF_KEY_MOVE_TUTORIAL_GATE_SEEN_V2, 1);
        PlayerPrefs.Save();

        BeginPlayerTurn();
    }

    private System.Collections.IEnumerator Co_MoveTutorialIntroBoot()
    {
        // 인트로 동안 입력 잠금/적 턴 차단
        if (menu) menu.EnableInput(false);
        if (handUI) handUI.HideCards();
        if (desc) { desc.SetEnemyTurn(true); desc.SetPlayerDiscardMode(false); }

        tutorialIntroPlayed = true;

        // 1) 첫 문장 + 사운드
        if (desc) desc.ShowTemporaryExplanation(introMsg1);
        float w1 = introMsg1Seconds;
        if (!introRequireKey) // 자동 진행 모드에서만 클립 길이 고려
            w1 = Mathf.Max(w1, introSfx1 ? introSfx1.length : 0f);
        PlaySfx(introSfx1, introSfx1Volume);

        if (introRequireKey)
        {
            while (!Input.GetKeyDown(introKey)) yield return null;
            yield return null; while (Input.GetKey(introKey)) yield return null;
        }
        else if (w1 > 0f) yield return new WaitForSeconds(w1);

        // 2) 둘째 문장 + 사운드
        if (desc) desc.ShowTemporaryExplanation(introMsg2);
        float w2 = introMsg2Seconds;
        if (!introRequireKey)
            w2 = Mathf.Max(w2, introSfx2 ? introSfx2.length : 0f);
        PlaySfx(introSfx2, introSfx2Volume);

        if (introRequireKey)
        {
            while (!Input.GetKeyDown(introKey)) yield return null;
            yield return null; while (Input.GetKey(introKey)) yield return null;
        }
        else if (w2 > 0f) yield return new WaitForSeconds(w2);

        if (desc) desc.ClearTemporaryMessage();

        // ▶ 인트로 완료 플래그 저장 (v2)
        s_MoveTutorialSeenThisSession = true;
        PlayerPrefs.SetInt(PREF_KEY_MOVE_TUTORIAL_SEEN_V2, 1);
        PlayerPrefs.Save();

        // 인트로 후 적 턴 시작
        BeginEnemyTurn();
    }

#if UNITY_EDITOR
    // F12: 인트로 플래그 초기화 / F11: 게이트 플래그 초기화  (v2 키 기준)
    void Update()
    {
        if (IsMoveTutorial() && Input.GetKeyDown(KeyCode.F12))
        {
            PlayerPrefs.DeleteKey(PREF_KEY_MOVE_TUTORIAL_SEEN_V2);
            s_MoveTutorialSeenThisSession = false;
            Debug.LogWarning("[TurnManager] Intro v2 flag cleared (F12)");
        }
        if (IsMoveTutorial() && Input.GetKeyDown(KeyCode.F11))
        {
            PlayerPrefs.DeleteKey(PREF_KEY_MOVE_TUTORIAL_GATE_SEEN_V2);
            s_MoveTutorialGateSeenThisSession = false;
            Debug.LogWarning("[TurnManager] Gate v2 flag cleared (F11)");
        }
    }
#endif

    // ─────────────────────────────────────────
    // SFX helper
    // ─────────────────────────────────────────
    private void PlaySfx(AudioClip clip, float volume)
    {
        if (!clip) return;
        var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, pos, Mathf.Clamp01(volume));
    }
}
