// Assets/Script/Battle/Item_script/ItemHandUI.cs  (폴더는 편한 곳에)
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ItemHandUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleMenuController menu;   // 0=Card, 1=Item, 2=End, 3=Run
    private CanvasGroup cg;

    // 적턴이면 무조건 숨김
    private bool enemyTurn = false;

    void Reset()
    {
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
    }

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (!menu) menu = FindObjectOfType<BattleMenuController>(true);
        Hide();
    }

    void OnEnable()
    {
        if (menu)
        {
            menu.onFocusChanged.AddListener(OnMenuFocusChanged);
            // 현재 인덱스로 한 번 동기화
            OnMenuFocusChanged(menu.Index);
        }
    }

    void OnDisable()
    {
        if (menu)
            menu.onFocusChanged.RemoveListener(OnMenuFocusChanged);
    }

    private void OnMenuFocusChanged(int idx)
    {
        if (enemyTurn)
        {
            Hide();
            return;
        }

        // 1 = Item 포커스일 때만 표시
        if (idx == 1) Show();
        else Hide();
    }

    public void SetEnemyTurn(bool on)
    {
        enemyTurn = on;
        if (on) Hide();
        else
        {
            // 플레이어 턴 복귀 시 현재 포커스 기준으로 다시 동기화
            if (menu) OnMenuFocusChanged(menu.Index);
            else Hide();
        }
    }

    private void Show()
    {
        if (!cg) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        if (!cg) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        gameObject.SetActive(true); // CanvasGroup만 끄고 오브젝트는 유지(레이아웃 안정)
    }
}
