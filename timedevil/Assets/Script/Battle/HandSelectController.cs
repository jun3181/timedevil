using UnityEngine;
using UnityEngine.UI;

public class HandSelectController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private HandUI hand;
    [SerializeField] private Image externalSelector; // 옵션
    [SerializeField] private CardUseOrchestrator orchestrator;
    [SerializeField] private DescriptionPanelController desc;
    [SerializeField] private PanelController panelController;

    [Header("Behavior")]
    [SerializeField] private bool wrap = true;
    private bool panelViewBeforeSelect = false;

    void Reset()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        if (!hand) hand = FindObjectOfType<HandUI>(true);
        if (!orchestrator) orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (!desc) desc = FindObjectOfType<DescriptionPanelController>(true);
        if (!panelController) panelController = FindObjectOfType<PanelController>(true);
    }

    void Awake()
    {
        if (externalSelector) externalSelector.enabled = false;
    }

    void OnEnable()
    {
        if (hand != null)
        {
            hand.onSelectModeChanged += OnHandSelectModeChanged;
            hand.onSelectIndexChanged += OnHandIndexChanged;
        }
    }

    void OnDisable()
    {
        if (hand != null)
        {
            hand.onSelectModeChanged -= OnHandSelectModeChanged;
            hand.onSelectIndexChanged -= OnHandIndexChanged;
        }

        orchestrator?.ClearSelectedAttackWarning();
    }

    void Update()
    {
        if (!menu || !hand) return;

        //  강제 버림 단계 중에는 메뉴 인덱스와 무관하게 손패 선택 유지
        bool inDiscard = TurnManager.Instance && TurnManager.Instance.IsPlayerDiscardPhase;

        if (hand.IsReadOnlySelectMode)
            return;

        // 일반 진입(카드 탭) — 단, 버림 단계가 아닐 때만
        if (!inDiscard && !hand.IsInSelectMode && menu.Index == 0 && Input.GetKeyDown(KeyCode.E))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.CardSelect))
                return;

            if (hand.CardCount <= 0) return;
            panelViewBeforeSelect = panelController != null && panelController.IsGameplayView;
            hand.EnterSelectMode();
            BattleTutorialGate.Report(BattleTutorialAction.CardSelect);
            menu.EnableInput(false);
            return;
        }

        if (!hand.IsInSelectMode) return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.CardSelectionMove)) return;
            hand.MoveSelect(+1);
            BattleTutorialGate.Report(BattleTutorialAction.CardSelectionMove);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.CardSelectionMove)) return;
            hand.MoveSelect(-1);
            BattleTutorialGate.Report(BattleTutorialAction.CardSelectionMove);
        }

        // Q: 버림 단계에서는 취소 불가, 평상시엔 취소 가능
        if (!inDiscard && Input.GetKeyDown(KeyCode.Q))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.CardCancel))
                return;

            hand.ExitSelectMode();
            menu.EnableInput(true);
            if (panelController) panelController.SetGameplayView(panelViewBeforeSelect);
            BattleTutorialGate.Report(BattleTutorialAction.CardCancel);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inDiscard)
            {
                if (!BattleTutorialGate.Allows(BattleTutorialAction.CardDiscard))
                    return;

                //  버림 수행
                var bdr = BattleDeckRuntime.Instance;
                if (bdr != null && hand.CurrentSelectIndex >= 0)
                {
                    int idx = hand.CurrentSelectIndex;
                    bdr.DiscardToBottom(idx);       // 덱 밑으로 보냄
                    BattleTutorialGate.Report(BattleTutorialAction.CardDiscard);

                    int over = bdr.OverCapCount;
                    if (TurnManager.Instance) TurnManager.Instance.OnPlayerDiscardOne(over);
                }
            }
            else
            {
                if (!BattleTutorialGate.Allows(BattleTutorialAction.CardUse))
                    return;

                // 평상시엔 카드 사용
                orchestrator?.UseCurrentSelected();
            }
        }
    }

    private void OnHandSelectModeChanged(bool on)
    {
        if (externalSelector)
        {
            externalSelector.enabled = on;
            if (on) SnapExternalSelector(hand.CurrentSelectIndex);
        }

        RefreshAttackWarningForSelection();
    }

    private void OnHandIndexChanged(int idx)
    {
        if (externalSelector) SnapExternalSelector(idx);
        RefreshAttackWarningForSelection();
    }

    private void RefreshAttackWarningForSelection()
    {
        if (!orchestrator) return;

        bool inDiscard = TurnManager.Instance && TurnManager.Instance.IsPlayerDiscardPhase;
        if (hand != null && hand.IsInSelectMode && !hand.IsReadOnlySelectMode && !inDiscard)
            orchestrator.RefreshSelectedAttackWarning();
        else
            orchestrator.ClearSelectedAttackWarning();
    }

    private void SnapExternalSelector(int index)
    {
        if (!externalSelector) return;
        var rt = hand.GetCardRect(index);
        if (!rt) return;
        externalSelector.rectTransform.position = rt.position;
    }

}
