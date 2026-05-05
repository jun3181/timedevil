using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Battle UI 메뉴 상호작용에 따라
/// - enemyTargets (연출용 적)
/// - gameplayTargets (그리드/캐릭터/전투 요소)
/// 를 서로 반대 방향으로 이동시켜 전투 시점을 전환한다.
/// </summary>
public class PanelController : MonoBehaviour
{
    [Header("Trigger Refs")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private HandUI handUI;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private CardUseOrchestrator orchestrator;

    [Header("Menu Submit Index Rules")]
    [Tooltip("켜면 panel 인덱스 제출 시 게임플레이 뷰로 전환")]
    [SerializeField] private bool usePanelSubmit = true;
    [SerializeField] private int panelSubmitIndex = 2;

    [Tooltip("켜면 card 인덱스 제출 시 게임플레이 뷰로 전환")]
    [SerializeField] private bool useCardSubmit = true;
    [SerializeField] private int cardSubmitIndex = 0;

    [Tooltip("켜면 end 인덱스 제출 시 게임플레이 뷰로 전환")]
    [SerializeField] private bool useEndSubmit = true;
    [SerializeField] private int endSubmitIndex = 3;

    [Header("Return Rules")]
    [Tooltip("카드 선택모드에서 Q 취소 입력 시에만 원상태로 복귀")]
    [SerializeField] private bool returnOnCardCancelKey = true;

    [Tooltip("EnemyTurn -> PlayerTurn 전환 시 원상태로 복귀")]
    [SerializeField] private bool returnOnPlayerTurnStart = true;
    [SerializeField] private float submitViewDelay = 0.12f;
    [SerializeField] private float turnTransitionDelay = 0.16f;

    [Header("Target Groups")]
    [Tooltip("적(연출용) 대상들. 게임플레이 뷰로 전환하면 아래로 내려감")]
    [SerializeField] private List<Transform> enemyTargets = new List<Transform>();

    [Tooltip("그리드/캐릭터/전투 오브젝트 등 게임플레이 대상들. 기본은 아래 대기, 전환 시 올라옴")]
    [SerializeField] private List<Transform> gameplayTargets = new List<Transform>();

    [SerializeField] private bool useLocalPosition = true;
    [SerializeField] private bool lockZDepth = true;

    [Header("Offsets")]
    [Tooltip("게임플레이 뷰 ON일 때 enemyTargets에 적용할 오프셋(보통 아래 음수 Y)")]
    [SerializeField] private Vector3 enemyHiddenOffset = new Vector3(0f, -650f, 0f);

    [Tooltip("게임플레이 뷰 OFF일 때 gameplayTargets에 적용할 오프셋(보통 아래 음수 Y)")]
    [SerializeField] private Vector3 gameplayHiddenOffset = new Vector3(0f, -650f, 0f);

    [Header("Animation - Enemy")]
    [SerializeField, Min(0.01f)] private float enemyDuration = 0.35f;
    [SerializeField] private AnimationCurve enemyEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    [Header("Animation - Gameplay")]
    [SerializeField, Min(0.01f)] private float gameplayDuration = 0.35f;
    [SerializeField] private AnimationCurve gameplayEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Battle Menu Panel Auto-Hide")]
    [Tooltip("상대 턴이거나 카드 선택 중일 때 화면 아래로 내릴 UI 패널들(Card/Item/End/Run)")]
    [SerializeField] private List<Transform> battleMenuPanelTargets = new List<Transform>();

    [Tooltip("자동 숨김 시 적용할 패널 오프셋(화면 밖으로 내려가도록 충분히 큰 음수 Y 권장)")]
    [SerializeField] private Vector3 battleMenuHiddenOffset = new Vector3(0f, -900f, 0f);

    [SerializeField, Min(0.01f)] private float battleMenuDuration = 0.3f;
    [SerializeField] private AnimationCurve battleMenuEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Initial State")]
    [Tooltip("체크 시 시작부터 gameplayTargets가 보이는 상태")]
    [SerializeField] private bool startInGameplayView = false;
    [Tooltip("시작 직후 턴 상태(Player/Enemy)에 맞춰 최초 뷰를 즉시 동기화(애니메이션 없음)")]
    [SerializeField] private bool syncInitialViewWithTurnState = true;

    private readonly List<Vector3> enemyShownBase = new List<Vector3>();
    private readonly List<Vector3> gameplayShownBase = new List<Vector3>();
    private readonly List<Vector3> battleMenuShownBase = new List<Vector3>();

    private bool isGameplayView;
    private bool isAnimating;
    private bool pendingReturnByEnd;
    private Coroutine running;
    private Coroutine menuPanelRunning;
    private Coroutine delayedViewRoutine;
    private bool menuPanelsHidden;
    private bool initialSyncRequested;

    private TurnState lastTurnState = TurnState.PlayerTurn;
    private bool lastHandSelectMode;

    void Reset()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!handUI) handUI = FindObjectOfType<HandUI>(true);
        if (!turnManager) turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (!orchestrator) orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
    }

    void Awake()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!handUI) handUI = FindObjectOfType<HandUI>(true);
        if (!turnManager) turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (!orchestrator) orchestrator = FindObjectOfType<CardUseOrchestrator>(true);

        CacheShownBasePositions();

        isGameplayView = startInGameplayView;
        ApplyImmediate(isGameplayView);
        ApplyBattleMenuImmediate(false);

        if (turnManager) lastTurnState = turnManager.currentTurn;
        if (handUI) lastHandSelectMode = handUI.IsInSelectMode;
    }

    void OnEnable()
    {
        if (menu) menu.onSubmit.AddListener(OnMenuSubmit);
        if (!turnManager) turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (syncInitialViewWithTurnState && !initialSyncRequested)
        {
            initialSyncRequested = true;
            StartCoroutine(Co_SyncInitialViewWithTurnState());
        }
    }

    void OnDisable()
    {
        if (menu) menu.onSubmit.RemoveListener(OnMenuSubmit);
        if (turnManager) turnManager.OnTurnChanged -= HandleTurnChanged;
    }

    void Update()
    {
        bool qDown = Input.GetKeyDown(KeyCode.Q);
        bool handSelecting = handUI && handUI.IsInSelectMode;

        bool enemyTurn = turnManager && turnManager.currentTurn == TurnState.EnemyTurn;
        bool cardResolving = orchestrator && orchestrator.GetIsBusy();
        bool shouldHideMenuPanels = enemyTurn || handSelecting || cardResolving;
        if (shouldHideMenuPanels != menuPanelsHidden)
            SetBattleMenuPanelsHidden(shouldHideMenuPanels);

        if (returnOnCardCancelKey && qDown)
        {
            bool inDiscard = turnManager && turnManager.IsPlayerDiscardPhase;
            bool wasSelectingThisFrame = handSelecting || lastHandSelectMode;

            if (!inDiscard && wasSelectingThisFrame)
                SetGameplayView(false);
        }

        lastHandSelectMode = handSelecting;

        if (!returnOnPlayerTurnStart) return;

        if (!turnManager) turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (!orchestrator) orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (!turnManager) return;

        var cur = turnManager.currentTurn;
        if (cur != lastTurnState)
        {
            bool enemyToPlayer = (lastTurnState == TurnState.EnemyTurn && cur == TurnState.PlayerTurn);
            bool playerToEnemy = (lastTurnState == TurnState.PlayerTurn && cur == TurnState.EnemyTurn);
            if (enemyToPlayer && (pendingReturnByEnd || isGameplayView))
            {
                SetGameplayViewDelayed(false, turnTransitionDelay);
                pendingReturnByEnd = false;
            }
            else if (playerToEnemy && isGameplayView)
            {
                SetGameplayViewDelayed(false, turnTransitionDelay);
            }
            lastTurnState = cur;
        }
    }

    private void OnMenuSubmit(int index)
    {
        if (usePanelSubmit && index == panelSubmitIndex)
        {
            SetGameplayViewDelayed(true, submitViewDelay);
            return;
        }

        if (useCardSubmit && index == cardSubmitIndex)
        {
            if (handUI != null && handUI.CardCount <= 0)
                return;

            SetGameplayViewDelayed(true, submitViewDelay);
            return;
        }

        if (useEndSubmit && index == endSubmitIndex)
        {
            SetGameplayViewDelayed(true, submitViewDelay);
            pendingReturnByEnd = true;
        }
    }



    public bool IsGameplayView => isGameplayView;

    public void ToggleView()
    {
        SetGameplayView(!isGameplayView);
    }

    public void SetGameplayView(bool on, bool immediate = false)
    {
        if (delayedViewRoutine != null)
        {
            StopCoroutine(delayedViewRoutine);
            delayedViewRoutine = null;
        }

        if (isAnimating && running != null)
        {
            StopCoroutine(running);
            running = null;
            isAnimating = false;
        }

        isGameplayView = on;

        if (immediate) ApplyImmediate(on);
        else running = StartCoroutine(Co_Animate(on));
    }

    public void SetGameplayViewDelayed(bool on, float delaySeconds)
    {
        if (delayedViewRoutine != null)
        {
            StopCoroutine(delayedViewRoutine);
            delayedViewRoutine = null;
        }
        delayedViewRoutine = StartCoroutine(Co_SetGameplayViewDelayed(on, delaySeconds));
    }

    private IEnumerator Co_SetGameplayViewDelayed(bool on, float delaySeconds)
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);
        SetGameplayView(on);
        delayedViewRoutine = null;
    }

    private IEnumerator Co_SyncInitialViewWithTurnState()
    {
        if (!turnManager) turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (!turnManager) yield break;

        // 첫 턴이 실제로 확정될 때까지 기다렸다가 즉시 반영 (프레임 타임아웃 없음)
        while (!turnManager.HasFirstTurnDecided)
            yield return null;

        bool enemyTurn = turnManager && turnManager.currentTurn == TurnState.EnemyTurn;
        bool isMoveTutorial = SceneManager.GetActiveScene().name == "Move_Tutorial";
        bool shouldAnimateAfterIntro = isMoveTutorial && DialogueManager.instance != null;
        if (shouldAnimateAfterIntro)
        {
            while (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
                yield return null;
        }

        SetGameplayView(enemyTurn, !shouldAnimateAfterIntro);

        bool handSelecting = handUI && handUI.IsInSelectMode;
        bool cardResolving = orchestrator && orchestrator.GetIsBusy();
        bool shouldHideMenuPanels = enemyTurn || handSelecting || cardResolving;
        SetBattleMenuPanelsHidden(shouldHideMenuPanels, true);

        if (turnManager) lastTurnState = turnManager.currentTurn;
    }

    // 일부 브랜치/프리팹 상태에서 OnTurnChanged 구독 코드가 남아 있을 수 있어
    // 컴파일 오류(CS0103) 방지를 위해 안전한 핸들러를 유지한다.
    private void HandleTurnChanged(TurnState _)
    {
    }

    [ContextMenu("Re-cache Shown Base From Current")]
    public void CacheShownBasePositions()
    {
        enemyShownBase.Clear();
        gameplayShownBase.Clear();
        battleMenuShownBase.Clear();

        for (int i = 0; i < enemyTargets.Count; i++)
            enemyShownBase.Add(GetPos(enemyTargets[i]));

        for (int i = 0; i < gameplayTargets.Count; i++)
            gameplayShownBase.Add(GetPos(gameplayTargets[i]));

        for (int i = 0; i < battleMenuPanelTargets.Count; i++)
            battleMenuShownBase.Add(GetPos(battleMenuPanelTargets[i]));
    }

    private IEnumerator Co_Animate(bool gameplayView)
    {
        isAnimating = true;

        var enemyFrom = SnapshotCurrent(enemyTargets);
        var gameplayFrom = SnapshotCurrent(gameplayTargets);

        var enemyTo = BuildEnemyTargetPositions(gameplayView);
        var gameplayTo = BuildGameplayTargetPositions(gameplayView);

        float maxDuration = Mathf.Max(enemyDuration, gameplayDuration);
        float t = 0f;

        while (t < maxDuration)
        {
            t += Time.deltaTime;

            float enemyT = enemyDuration <= 0f ? 1f : Mathf.Clamp01(t / enemyDuration);
            float gameplayT = gameplayDuration <= 0f ? 1f : Mathf.Clamp01(t / gameplayDuration);

            float enemyK = EvaluateCurve(enemyEase, enemyT);
            float gameplayK = EvaluateCurve(gameplayEase, gameplayT);

            ApplyLerp(enemyTargets, enemyFrom, enemyTo, enemyK);
            ApplyLerp(gameplayTargets, gameplayFrom, gameplayTo, gameplayK);

            yield return null;
        }

        ApplyAbsolute(enemyTargets, enemyTo);
        ApplyAbsolute(gameplayTargets, gameplayTo);

        running = null;
        isAnimating = false;
    }

    private void ApplyImmediate(bool gameplayView)
    {
        ApplyAbsolute(enemyTargets, BuildEnemyTargetPositions(gameplayView));
        ApplyAbsolute(gameplayTargets, BuildGameplayTargetPositions(gameplayView));
    }

    private List<Vector3> BuildEnemyTargetPositions(bool gameplayView)
    {
        var list = new List<Vector3>(enemyTargets.Count);
        for (int i = 0; i < enemyTargets.Count; i++)
        {
            Vector3 shown = i < enemyShownBase.Count ? enemyShownBase[i] : GetPos(enemyTargets[i]);
            list.Add(gameplayView ? shown + enemyHiddenOffset : shown);
        }
        return list;
    }

    private List<Vector3> BuildGameplayTargetPositions(bool gameplayView)
    {
        var list = new List<Vector3>(gameplayTargets.Count);
        for (int i = 0; i < gameplayTargets.Count; i++)
        {
            Vector3 shown = i < gameplayShownBase.Count ? gameplayShownBase[i] : GetPos(gameplayTargets[i]);
            list.Add(gameplayView ? shown : shown + gameplayHiddenOffset);
        }
        return list;
    }


    public void SetBattleMenuPanelsHidden(bool hidden, bool immediate = false)
    {
        if (menuPanelRunning != null)
        {
            StopCoroutine(menuPanelRunning);
            menuPanelRunning = null;
        }

        menuPanelsHidden = hidden;

        if (immediate) ApplyBattleMenuImmediate(hidden);
        else menuPanelRunning = StartCoroutine(Co_AnimateBattleMenuPanels(hidden));
    }

    private IEnumerator Co_AnimateBattleMenuPanels(bool hidden)
    {
        var from = SnapshotCurrent(battleMenuPanelTargets);
        var to = BuildBattleMenuTargetPositions(hidden);

        float t = 0f;
        while (t < battleMenuDuration)
        {
            t += Time.deltaTime;
            float k = battleMenuDuration <= 0f ? 1f : Mathf.Clamp01(t / battleMenuDuration);
            ApplyLerp(battleMenuPanelTargets, from, to, EvaluateCurve(battleMenuEase, k));
            yield return null;
        }

        ApplyAbsolute(battleMenuPanelTargets, to);
        menuPanelRunning = null;
    }

    private void ApplyBattleMenuImmediate(bool hidden)
    {
        ApplyAbsolute(battleMenuPanelTargets, BuildBattleMenuTargetPositions(hidden));
    }

    private List<Vector3> BuildBattleMenuTargetPositions(bool hidden)
    {
        var list = new List<Vector3>(battleMenuPanelTargets.Count);
        for (int i = 0; i < battleMenuPanelTargets.Count; i++)
        {
            Vector3 shown = i < battleMenuShownBase.Count ? battleMenuShownBase[i] : GetPos(battleMenuPanelTargets[i]);
            list.Add(hidden ? shown + battleMenuHiddenOffset : shown);
        }
        return list;
    }

    private List<Vector3> SnapshotCurrent(List<Transform> targets)
    {
        var list = new List<Vector3>(targets.Count);
        for (int i = 0; i < targets.Count; i++)
            list.Add(GetPos(targets[i]));
        return list;
    }

    private void ApplyLerp(List<Transform> targets, List<Vector3> from, List<Vector3> to, float t)
    {
        int n = Mathf.Min(targets.Count, Mathf.Min(from.Count, to.Count));
        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;
            SetPos(tr, Vector3.LerpUnclamped(from[i], to[i], t));
        }
    }

    private void ApplyAbsolute(List<Transform> targets, List<Vector3> to)
    {
        int n = Mathf.Min(targets.Count, to.Count);
        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;
            SetPos(tr, to[i]);
        }
    }

    private Vector3 GetPos(Transform t)
    {
        if (!t) return Vector3.zero;
        return useLocalPosition ? t.localPosition : t.position;
    }

    private void SetPos(Transform t, Vector3 value)
    {
        if (!t) return;

        if (lockZDepth)
        {
            float currentZ = useLocalPosition ? t.localPosition.z : t.position.z;
            value.z = currentZ;
        }

        if (useLocalPosition) t.localPosition = value;
        else t.position = value;
    }

    private static float EvaluateCurve(AnimationCurve curve, float t)
    {
        if (curve == null || curve.length == 0) return t;
        return curve.Evaluate(t);
    }
}
