// Assets/Script/Battle/Item_script/ItemHandUI.cs  (������ ���� ����)
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ItemHandUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleMenuController menu;   // 0=Card, 1=Item,2=Panel, 3=End, 4=Run
    private CanvasGroup cg;

    // �����̸� ������ ����
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
            // ���� �ε����� �� �� ����ȭ
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

        // 1 = Item ��Ŀ���� ���� ǥ��
        if (idx == 1) Show();
        else Hide();
    }

    public void SetEnemyTurn(bool on)
    {
        enemyTurn = on;
        if (on) Hide();
        else
        {
            // �÷��̾� �� ���� �� ���� ��Ŀ�� �������� �ٽ� ����ȭ
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
        gameObject.SetActive(true); // CanvasGroup�� ���� ������Ʈ�� ����(���̾ƿ� ����)
    }
}
