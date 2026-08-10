using UnityEngine;

public class EndController : MonoBehaviour
{
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private int endIndex = 3;

    void Reset()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
    }

    void Update()
    {
        if (!menu) return;

        if (menu.Index == ResolveEndIndex() && Input.GetKeyDown(KeyCode.E))
        {
            if (!BattleTutorialGate.Allows(BattleTutorialAction.EndPanelInteract)
                && !BattleTutorialGate.Allows(BattleTutorialAction.TurnEnd))
                return;

            if (TurnManager.Instance != null)
            {
                //  먼저 강제 버림 단계 진입 시도
                TurnManager.Instance.OnPlayerPressedEnd();
                BattleTutorialGate.Report(BattleTutorialAction.EndPanelInteract);
            }
        }
    }

    private int ResolveEndIndex()
    {
        return menu != null && menu.EntryCount >= 5 ? endIndex : 2;
    }
}
