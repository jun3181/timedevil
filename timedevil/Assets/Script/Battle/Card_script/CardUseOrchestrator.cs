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
    [SerializeField] private ShowCardController showCard;
    [SerializeField] private float totalSeconds = 3f;   // 페이드 포함 총 시간

    [Header("UI Hooks")]
    [SerializeField] private DescriptionPanelController desc; // 👈 관전 모드 대사 표시용
    [SerializeField] private bool logDebug = false; // ← 옵션 로그

    // (효과 실행은 타이밍 안정화 후 다시 연결)
    [Header("Optional Effect Controllers (disabled for timing)")]
    [SerializeField] private AttackController attackController;
    [SerializeField] private SupportController supportController;
    [SerializeField] private DrawController drawController;
    [SerializeField] private MoveController moveController;


    private bool busy;

    void Awake()
    {
        // 💡 런타임에서도 안전하게 참조 보강
        if (!hand) hand = FindObjectOfType<HandUI>(true);
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!database) database = Resources.Load<CardDatabaseSO>("CardDatabase");
        if (!costController) costController = FindObjectOfType<CostController>(true);
        if (!showCard) showCard = FindObjectOfType<ShowCardController>(true);
        if (!desc) desc = FindObjectOfType<DescriptionPanelController>(true);

        if (logDebug && !desc)
            Debug.LogWarning("[Orchestrator] DescriptionPanelController not found. Explanation won't show.");
    }

    public void UseCurrentSelected()
    {
        if (busy || hand == null || !hand.IsInSelectMode) return;
        int idx = hand.CurrentSelectIndex;
        if (idx < 0 || idx >= hand.CardCount) return;
        StartCoroutine(Co_UseWithExactTiming(idx));
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

        // B. 코스트 즉시 지불
        int need = Mathf.Max(0, so.cost);
        if (costController && (costController.Current < need || !costController.TryPay(need)))
        { busy = false; yield break; }

        // C. 카드 즉시 제거(덱 아래)
        var bdr = BattleDeckRuntime.Instance;
        if (bdr != null) bdr.UseCardToBottom(handIndex);
        yield return null;               // 데이터 반영
        hand.RebuildFromHand();

        // D. 관전 모드: 선택 해제 + 입력 OFF + 설명 고정
        hand.ExitSelectMode();
        if (menu) menu.EnableInput(false);
        if (desc)
        {
            string line =
                !string.IsNullOrEmpty(so.explanation) ? so.explanation :
                (!string.IsNullOrEmpty(so.display) ? so.display :
                (!string.IsNullOrEmpty(so.displayName) ? so.displayName : so.id));
            desc.ShowTemporaryExplanation(line);
        }

        // ==== ★ 여기부터 효과 실행 분기(프리뷰/효과 타이밍) ====
        if (attackController != null && so is AttackCardSO aso)
        {
            // --- 동시에 실행하고, 둘 다 끝날 때까지 대기 ---
            bool previewDone = false;
            bool attackDone = false;

            // 동시에 시작
            StartCoroutine(CoRunPreview(aso.id, totalSeconds, () => previewDone = true));
            StartCoroutine(CoRunAttack(aso, () => attackDone = true));

            // 둘 중 누가 먼저 끝나든, 둘 다 true 될 때까지 대기
            while (!(previewDone && attackDone))
                yield return null;
        }
        else if (drawController != null && so is DrawCardSO dso)
        {
            // Draw는 실행 코루틴을 흘려보내고, 프리뷰만 기다림
            StartCoroutine(drawController.Execute(dso, Faction.Player));
            if (showCard != null) yield return showCard.PreviewById(so.id, totalSeconds);
            else yield return null;
        }
        else if (moveController != null && so is MoveCardSO mso)
        {
            // Move도 실행 코루틴을 흘려보내고, 프리뷰만 기다림
            StartCoroutine(moveController.Execute(mso, Faction.Player, Faction.Enemy));
            if (showCard != null) yield return showCard.PreviewById(so.id, totalSeconds);
            else yield return null;
        }
        else
        {
            // 기타 카드: 프리뷰만
            if (showCard != null) yield return showCard.PreviewById(so.id, totalSeconds);
            else yield return null;
        }
        // ==== ★ 분기 끝 ====

        // E. 설명 해제 및 선택 모드 복귀
        if (desc) desc.ClearTemporaryMessage();

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
        }

        busy = false;
    }
    // attack + showCard 동시 실행을 위한 내부 코루틴 래퍼
    private IEnumerator CoRunPreview(string id, float seconds, System.Action onDone)
    {
        if (showCard != null)
            yield return showCard.PreviewById(id, seconds);
        // showCard가 없으면 즉시 완료 처리
        onDone?.Invoke();
    }

    private IEnumerator CoRunAttack(AttackCardSO aso, System.Action onDone)
    {
        yield return attackController.Execute(aso, Faction.Player, Faction.Enemy);
        onDone?.Invoke();
    }


}
