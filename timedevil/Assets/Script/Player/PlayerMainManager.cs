using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMainManager : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField] private PlayerMove move;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private MenuController menu;
    [SerializeField] private GameManager gameManager;

    [Header("Keys (Q/W/E는 '이동'에 사용하지 않음)")]
    [SerializeField] private KeyCode keyMenu = KeyCode.Q;
    [SerializeField] private KeyCode keyInteractOrSubmit = KeyCode.E;
    [SerializeField] private KeyCode keyCloseAlso = KeyCode.W; // ✅ 메뉴 닫기에도 사용

    [Header("Debug")]
    [SerializeField] private bool debugInput = true;

    private void Reset()
    {
        move ??= GetComponent<PlayerMove>();
        interactor ??= GetComponent<PlayerInteractor>();
        gameManager ??= GameManager.Instance;
        menu ??= FindObjectOfType<MenuController>(true);
    }

    private void Awake()
    {
        if (!move) move = GetComponent<PlayerMove>();
        if (!interactor) interactor = GetComponent<PlayerInteractor>();
        if (!gameManager) gameManager = GameManager.Instance;
        if (!menu) menu = FindObjectOfType<MenuController>(true);

        if (!move) Debug.LogError("[PlayerMainManager] PlayerMove가 필요합니다.", this);
        if (!interactor) Debug.LogError("[PlayerMainManager] PlayerInteractor가 필요합니다.", this);

        if (debugInput)
        {
            var menus = FindObjectsOfType<MenuController>(true);
            Debug.Log($"[PlayerMainManager] Awake. menus found = {menus.Length}", this);
        }
    }

    private void Update()
    {
        // ===== Debug: 키 입력이 여기로 들어오는지 확인 =====
        if (debugInput)
        {
            if (Input.GetKeyDown(keyMenu))
                Debug.Log($"[PlayerMainManager] KeyDown {keyMenu} (menu). menuOpen={menu?.IsOpen} gmAction={gameManager?.isAction}", this);

            if (Input.GetKeyDown(keyCloseAlso))
                Debug.Log($"[PlayerMainManager] KeyDown {keyCloseAlso} (closeAlso). menuOpen={menu?.IsOpen} gmAction={gameManager?.isAction}", this);

            if (Input.GetKeyDown(keyInteractOrSubmit))
                Debug.Log($"[PlayerMainManager] KeyDown {keyInteractOrSubmit} (interact/submit). menuOpen={menu?.IsOpen} gmAction={gameManager?.isAction}", this);
        }

        // =========================================================
        // 1) ✅ 메뉴가 열려있으면 최우선 처리 (Q/W로 닫히게)
        //    (여기서 처리 안 하면 gmAction=true 때문에 Update가 막혀버림)
        // =========================================================
        if (menu && menu.IsOpen)
        {
            move?.SetMoveInput(0, 0, false, false, false, false);

            if (Input.GetKeyDown(KeyCode.UpArrow)) menu.Navigate(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow)) menu.Navigate(+1);

            if (Input.GetKeyDown(keyInteractOrSubmit)) menu.SubmitCurrent();

            // ✅ Q 또는 W를 다시 누르면 닫기
            if (Input.GetKeyDown(keyMenu) || Input.GetKeyDown(keyCloseAlso))
            {
                if (debugInput) Debug.Log("[PlayerMainManager] Close menu by Q/W", this);
                menu.Close();
            }
            return;
        }

        // =========================================================
        // 2) 메뉴가 닫혀있을 때만 입력 차단 체크
        // =========================================================
        if (IsInputBlocked())
        {
            move?.SetMoveInput(0, 0, false, false, false, false);
            return;
        }

        // =========================================================
        // 3) 월드 모드
        // =========================================================
        // Q: 메뉴 열기
        if (menu && Input.GetKeyDown(keyMenu))
        {
            if (debugInput) Debug.Log("[PlayerMainManager] Open menu by Q", this);
            menu.Open();
            move?.SetMoveInput(0, 0, false, false, false, false);
            return;
        }

        // ✅ 이동은 화살표만 인정 (WASD 미사용)
        int h = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) + (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0);
        int v = (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) + (Input.GetKey(KeyCode.DownArrow) ? -1 : 0);

        bool hDown = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
        bool vDown = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);
        bool hUp = Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow);
        bool vUp = Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow);

        move?.SetMoveInput(h, v, hDown, vDown, hUp, vUp);

        // E: 상호작용
        if (Input.GetKeyDown(keyInteractOrSubmit))
        {
            interactor?.TryInteract();
        }

        // W: 월드에서는 지금은 아무것도 안 함(예약키)
    }

    private bool IsInputBlocked()
    {
        bool gmLock = (gameManager != null && gameManager.isAction);

        bool dialogueLock = false;
        if (DialogueManager.instance != null)
        {
            dialogueLock = DialogueManager.instance.isDialogueActive || DialogueManager.instance.blockInput;
        }

        return gmLock || dialogueLock;
    }
}
