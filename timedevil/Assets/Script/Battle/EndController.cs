using UnityEngine;

public class EndController : MonoBehaviour
{
    [SerializeField] private BattleMenuController menu;

    void Reset()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
    }

    void Update()
    {
        if (!menu) return;

        if (menu.Index == 2 && Input.GetKeyDown(KeyCode.E))
        {
            if (TurnManager.Instance != null)
            {
                // ✅ 먼저 강제 버림 단계 진입 시도
                TurnManager.Instance.OnPlayerPressedEnd();
            }
        }
    }
}
