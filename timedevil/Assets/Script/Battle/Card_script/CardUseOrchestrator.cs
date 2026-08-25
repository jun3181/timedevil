using System.Collections;
using UnityEngine;

public enum Faction { Player, Enemy }

public class CardUseOrchestrator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HandUI hand;
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private CardDatabaseSO database;
    [SerializeField] private CostController costController;

    [Header("Preview")]
    [SerializeField] private bool showCardPreviewEnabled = false;
    [SerializeField] private ShowCardController showCard;
    [SerializeField] private float totalSeconds = 3f;   // 페이드 포함 총 시간

    [Header("UI Hooks")]
    [SerializeField] private DescriptionPanelController desc; //  관전 모드 대사 표시용
    [SerializeField] private bool logDebug = false; // ← 옵션 로그
    [SerializeField] private float postResolvePanelDelay = 0.2f;

    // (효과 실행은 타이밍 안정화 후 다시 연결)
    [Header("Optional Effect Controllers (disabled for timing)")]
    [SerializeField] private AttackController attackController;
    [SerializeField] private SupportController supportController;
    [SerializeField] private DrawController drawController;
    [SerializeField] private MoveController moveController;
    [SerializeField] private PanelController PanelController;

    public CardDatabaseSO CardDatabase => database;

    private bool busy;
    public bool IsBusy { get { return busy; } }
    public bool GetIsBusy() { return busy; }

    void Awake()
    {
        ResolveRefs();

        if (logDebug && !desc)
            Debug.LogWarning("[Orchestrator] DescriptionPanelController not found. Explanation won't show.");
    }

    void OnEnable()
    {
        ResolveRefs();
    }

    private void ResolveRefs()
    {
        //  런타임에서도 안전하게 참조 보강
        if (!hand) hand = FindObjectOfType<HandUI>(true);
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!database) database = Resources.Load<CardDatabaseSO>("CardDatabase");
        if (!costController) costController = FindObjectOfType<CostController>(true);
        if (!PanelController) PanelController = FindObjectOfType<PanelController>(true);
        if (!showCard) showCard = FindObjectOfType<ShowCardController>(true);
        if (!desc) desc = FindObjectOfType<DescriptionPanelController>(true);
        if (!attackController) attackController = FindObjectOfType<AttackController>(true);
        if (!supportController) supportController = FindObjectOfType<SupportController>(true);
        if (!drawController) drawController = FindObjectOfType<DrawController>(true);
        if (!moveController) moveController = FindObjectOfType<MoveController>(true);
    }

    private void EnsureActiveForUse()
    {
        if (gameObject.activeInHierarchy) return;

        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    void OnDisable()
    {
        ClearSelectedAttackWarning();
    }

    public void UseCurrentSelected()
    {
        if (!BattleTutorialGate.Allows(BattleTutorialAction.CardUse)) return;
        EnsureActiveForUse();
        ResolveRefs();
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[Orchestrator] Card use was requested while the orchestrator is inactive.");
            return;
        }
        if (busy || hand == null || !hand.IsInSelectMode) return;
        int idx = hand.CurrentSelectIndex;
        if (idx < 0 || idx >= hand.CardCount) return;
        ClearSelectedAttackWarning();
        StartCoroutine(Co_UseWithExactTiming(idx));
    }

    public void RefreshSelectedAttackWarning()
    {
        ResolveRefs();
        if (hand == null || !hand.IsInSelectMode || attackController == null)
        {
            ClearSelectedAttackWarning();
            return;
        }

        int idx = hand.CurrentSelectIndex;
        if (idx < 0 || idx >= hand.CardCount)
        {
            ClearSelectedAttackWarning();
            return;
        }

        string id = hand.GetVisibleIdAt(idx);
        var so = database ? database.GetById(id) : null;
        if (so is AttackCardSO attackCard)
            attackController.ShowPreviewWarning(attackCard, Faction.Enemy);
        else
            ClearSelectedAttackWarning();
    }

    public void ClearSelectedAttackWarning()
    {
        attackController?.ClearPreviewWarning();
    }

    /// <summary>
    /// 정확한 타이밍:
    /// 1) 카드 선택 → 코스트 즉시 지불(가능 여부 확인 포함)
    /// 2) 카드 즉시 사라짐(덱 아래로 이동)
    /// 3) 관전모드(선택 해제 + 메뉴 입력 OFF) + 설명판에 explanation 고정
    /// 4) ShowCard 프리뷰(페이드 인/유지/아웃)
    /// 5) 설명판 임시문구 해제 → 카드 선택 모드 복귀
    /// </summary>
    // CardUseOrchestrator.cs 내부
    private IEnumerator Co_UseWithExactTiming(int handIndex)
    {
        busy = true;

        // A. 카드 SO 확보
        string id = hand.GetVisibleIdAt(handIndex);
        if (string.IsNullOrEmpty(id)) { busy = false; yield break; }
        var so = database ? database.GetById(id) : null;
        if (!so) { busy = false; yield break; }

        // B. Draw 계열은 코스트 지불/카드 제거 전에 손패 버리기 가능 여부를 먼저 검사
        if (drawController != null && so is DrawCardSO precheckDraw &&
            !drawController.CanExecute(precheckDraw, Faction.Player, selfCardsAlreadyCommitted: 1, out string drawFailMessage))
        {
            desc?.ShowOneShotMessage(drawFailMessage);
            busy = false;
            yield break;
        }

        if (supportController != null && so is SupportCardSO precheckSupport &&
            !supportController.CanExecute(precheckSupport, Faction.Player, selfCardsAlreadyCommitted: 1, out string supportFailMessage))
        {
            desc?.ShowOneShotMessage(supportFailMessage);
            busy = false;
            yield break;
        }

        // C. 코스트 즉시 지불
        int need = Mathf.Max(0, so.cost);
        bool freeCost = supportController != null && supportController.TryConsumeNextCardFree(Faction.Player);
        if (!freeCost && costController && (costController.Current < need || !costController.TryPay(need)))
        { busy = false; yield break; }

        // D. 카드 사용 직전 선택 레이아웃을 즉시 정리해, 제거 후 남은 손패 y가 튀지 않게 한다.
        hand.ExitSelectMode(true);
        hand.HideCards();

        // E. 카드 즉시 제거(덱 아래)
        var bdr = BattleDeckRuntime.Instance;
        if (bdr != null) bdr.UseCardToBottom(handIndex);
        yield return null;               // 데이터 반영
        hand.RebuildFromHand();
        BattleTutorialGate.Report(BattleTutorialAction.CardUse);

        // F. 관전 모드: 입력 OFF + 설명 고정
        if (menu) menu.EnableInput(false);
        if (desc)
        {
            desc.EnterEffectLock();
            string line =
                !string.IsNullOrEmpty(so.explanation) ? so.explanation :
                (!string.IsNullOrEmpty(so.display) ? so.display :
                (!string.IsNullOrEmpty(so.displayName) ? so.displayName : so.id));
            desc.ShowTemporaryExplanation(line);
        }

        // ====  여기부터 효과 실행 분기(프리뷰/효과 타이밍) ====
        if (attackController != null && so is AttackCardSO aso)
        {
            // --- 동시에 실행하고, 둘 다 끝날 때까지 대기 ---
            bool previewDone = false;
            bool attackDone = false;

            // 동시에 시작
            StartCoroutine(CoRunPreview(aso.id, totalSeconds, () => previewDone = true));
            StartCoroutine(CoRunAttack(aso, true, () => attackDone = true));

            // 둘 중 누가 먼저 끝나든, 둘 다 true 될 때까지 대기
            while (!(previewDone && attackDone))
                yield return null;
        }
        else if (supportController != null && so is SupportCardSO sso)
        {
            bool previewDone = false;
            bool supportDone = false;

            StartCoroutine(CoRunPreview(sso.id, totalSeconds, () => previewDone = true));
            StartCoroutine(CoRunSupport(sso, () => supportDone = true));

            while (!(previewDone && supportDone))
                yield return null;
        }
        else if (drawController != null && so is DrawCardSO dso)
        {
            bool previewDone = false;
            bool drawDone = false;

            StartCoroutine(CoRunPreview(dso.id, totalSeconds, () => previewDone = true));
            StartCoroutine(CoRunDraw(dso, () => drawDone = true));

            while (!(previewDone && drawDone))
                yield return null;
        }
        else if (moveController != null && so is MoveCardSO mso)
        {
            bool previewDone = false;
            bool moveDone = false;

            StartCoroutine(CoRunPreview(mso.id, totalSeconds, () => previewDone = true));
            StartCoroutine(CoRunMove(mso, () => moveDone = true));

            while (!(previewDone && moveDone))
                yield return null;
        }
        else
        {
            // 기타 카드: 프리뷰만
            if (CanShowCardPreview()) yield return showCard.PreviewById(so.id, totalSeconds);
            else yield return null;
        }
        // ====  분기 끝 ====

        // F. 설명 해제 및 선택 모드 복귀
        if (desc) desc.ClearTemporaryMessage();
        if (desc) desc.ExitEffectLock();

        if (TurnManager.Instance != null && TurnManager.Instance.IsBattleResultPendingOrRunning)
        {
            if (menu) menu.EnableInput(false);
            hand.ExitSelectMode(true);
            hand.HideCards();
            busy = false;
            TurnManager.Instance.TryStartPendingBattleResultFlow();
            yield break;
        }

        if (hand.CardCount > 0)
        {
            hand.EnterSelectMode();
            int nextIdx = Mathf.Clamp(handIndex, 0, hand.CardCount - 1);
            hand.SetSelectIndexPublic(nextIdx);
            if (menu) menu.EnableInput(false); // 규칙 유지
        }
        else
        {
            if (menu) menu.EnableInput(true);
            if (PanelController && PanelController.IsGameplayView)
                PanelController.SetGameplayViewDelayed(false, postResolvePanelDelay);
        }

        busy = false;
    }
    // attack + showCard 동시 실행을 위한 내부 코루틴 래퍼
    private IEnumerator CoRunPreview(string id, float seconds, System.Action onDone)
    {
        if (CanShowCardPreview())
            yield return showCard.PreviewById(id, seconds);
        // showCard가 없으면 즉시 완료 처리
        onDone?.Invoke();
    }

    private bool CanShowCardPreview()
    {
        return showCardPreviewEnabled && showCard != null;
    }

    private IEnumerator CoRunAttack(AttackCardSO aso, bool skipWarningTimeline, System.Action onDone)
    {
        yield return attackController.Execute(aso, Faction.Player, Faction.Enemy, skipWarningTimeline);
        onDone?.Invoke();
    }

    private IEnumerator CoRunSupport(SupportCardSO sso, System.Action onDone)
    {
        yield return supportController.Execute(sso, Faction.Player, Faction.Enemy);
        onDone?.Invoke();
    }

    private IEnumerator CoRunDraw(DrawCardSO dso, System.Action onDone)
    {
        yield return drawController.Execute(dso, Faction.Player);
        onDone?.Invoke();
    }

    private IEnumerator CoRunMove(MoveCardSO mso, System.Action onDone)
    {
        yield return moveController.Execute(mso, Faction.Player, Faction.Enemy);
        onDone?.Invoke();
    }


}
