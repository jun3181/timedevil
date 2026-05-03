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
    private bool noCardNoticeShown = false;
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
    }

    void Update()
    {
        if (!menu || !hand) return;

        //  강제 버림 단계 중에는 메뉴 인덱스와 무관하게 손패 선택 유지
        bool inDiscard = TurnManager.Instance && TurnManager.Instance.IsPlayerDiscardPhase;

        // 일반 진입(카드 탭) — 단, 버림 단계가 아닐 때만
        if (!inDiscard && !hand.IsInSelectMode && menu.Index == 0 && Input.GetKeyDown(KeyCode.E))
        {
            if (hand.CardCount <= 0)
            {
                desc?.ShowOneShotMessage("선택가능한 카드가 없습니다. Q를 눌러 취소하세요.", 1.25f);
                noCardNoticeShown = true;
                return;
            }
            panelViewBeforeSelect = panelController != null && panelController.IsGameplayView;
            hand.EnterSelectMode();
            menu.EnableInput(false);
            return;
        }

        if (noCardNoticeShown && Input.GetKeyDown(KeyCode.Q))
        {
            noCardNoticeShown = false;
            desc?.ClearTemporaryMessage();
            menu.EnableInput(true);
            if (panelController) panelController.SetGameplayView(panelViewBeforeSelect);
            return;
        }

        if (!hand.IsInSelectMode) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) hand.MoveSelect(+1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) hand.MoveSelect(-1);

        // Q: 버림 단계에서는 취소 불가, 평상시엔 취소 가능
        if (!inDiscard && Input.GetKeyDown(KeyCode.Q))
        {
            hand.ExitSelectMode();
            menu.EnableInput(true);
            if (panelController) panelController.SetGameplayView(panelViewBeforeSelect);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inDiscard)
            {
                //  버림 수행
                var bdr = BattleDeckRuntime.Instance;
                if (bdr != null && hand.CurrentSelectIndex >= 0)
                {
                    int idx = hand.CurrentSelectIndex;
                    bdr.DiscardToBottom(idx);       // 덱 밑으로 보냄
                    hand.RebuildFromHand();         // UI 갱신(인덱스 보정 포함)

                    int over = bdr.OverCapCount;
                    if (TurnManager.Instance) TurnManager.Instance.OnPlayerDiscardOne(over);
                }
            }
            else
            {
                // 평상시엔 카드 사용
                orchestrator?.UseCurrentSelected();
            }
        }
    }

    private void OnHandSelectModeChanged(bool on)
    {
        if (!externalSelector) return;
        externalSelector.enabled = on;
        if (on) SnapExternalSelector(hand.CurrentSelectIndex);
    }

    private void OnHandIndexChanged(int idx)
    {
        if (!externalSelector) return;
        SnapExternalSelector(idx);
    }

    private void SnapExternalSelector(int index)
    {
        if (!externalSelector) return;
        var rt = hand.GetCardRect(index);
        if (!rt) return;
        externalSelector.rectTransform.position = rt.position;
    }

}
