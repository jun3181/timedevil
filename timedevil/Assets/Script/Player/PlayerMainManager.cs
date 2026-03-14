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
    [SerializeField] private KeyCode keyBackOrReserved = KeyCode.W; // 메뉴에서는 Back(닫기), 월드에서는 예약

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool verboseInputDebug = false;
    [SerializeField] private float verboseInputDebugInterval = 0.5f;

    private bool _lastBlocked = false;
    private string _lastBlockReason = "";
    private float _nextVerboseDebugAt = 0f;

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

        if (!move) Debug.LogError("[PlayerMainManager] PlayerMove가 필요합니다.");
        if (!interactor) Debug.LogError("[PlayerMainManager] PlayerInteractor가 필요합니다.");
        if (!menu) Debug.LogError("[PlayerMainManager] MenuController를 찾지 못했습니다. (씬에 1개만 두고 연결 권장)");
    }

    private void Update()
    {
        // =========================
        // DIALOGUE MODE (E는 대사 넘기기 전용)
        // =========================
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
        {
            move?.SetMoveInput(0, 0, false, false, false, false);

            // 컷씬이면 스킵 금지
            if (!DialogueManager.instance.blockInput && Input.GetKeyDown(keyInteractOrSubmit))
            {
                if (debugLog) Debug.Log("[PlayerMainManager] Dialogue Advance by E");
                DialogueManager.instance.DisplayNextSentence();
            }

            // 대화 중엔 메뉴/상호작용으로 절대 안 내려보냄
            return;
        }

        // =========================
        // MENU MODE (메뉴가 열려 있으면 ActionLock보다 우선 처리)
        // =========================
        if (menu != null && menu.IsOpen)
        {
            LogBlockState(false, "");
            move?.SetMoveInput(0, 0, false, false, false, false);

            // 메뉴 닫기: Q 또는 W
            if (Input.GetKeyDown(keyMenu) || Input.GetKeyDown(keyBackOrReserved))
            {
                if (debugLog) Debug.Log("[PlayerMainManager] MENU CLOSE by Q/W");
                menu.Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow)) menu.Navigate(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow)) menu.Navigate(+1);

            if (Input.GetKeyDown(keyInteractOrSubmit)) menu.SubmitCurrent();
            return;
        }

        // =========================
        // CUTSCENE / ACTION LOCK (대화/메뉴는 위에서 처리)
        // =========================
        if (IsInputBlockedByCutsceneOnly(out string blockReason))
        {
            LogBlockState(true, blockReason);
            move?.SetMoveInput(0, 0, false, false, false, false);
            return;
        }

        LogBlockState(false, "");

        // =========================
        // WORLD MODE
        // =========================

        // 메뉴 열기: Q
        if (menu != null && Input.GetKeyDown(keyMenu))
        {
            if (debugLog) Debug.Log("[PlayerMainManager] MENU OPEN by Q");
            menu.Open();
            move?.SetMoveInput(0, 0, false, false, false, false);
            return;
        }

        // 이동: Arrow만
        int h = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) + (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0);
        int v = (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) + (Input.GetKey(KeyCode.DownArrow) ? -1 : 0);

        bool hDown = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
        bool vDown = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);
        bool hUp = Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow);
        bool vUp = Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow);

        if (verboseInputDebug && Time.unscaledTime >= _nextVerboseDebugAt)
        {
            bool anyGameplayKey =
                Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) ||
                Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.E);

            if (anyGameplayKey)
            {
                _nextVerboseDebugAt = Time.unscaledTime + Mathf.Max(0.1f, verboseInputDebugInterval);
                DebugDumpInputState("WORLD_INPUT");
            }
        }

        move?.SetMoveInput(h, v, hDown, vDown, hUp, vUp);

        // 상호작용: E
        if (Input.GetKeyDown(keyInteractOrSubmit))
        {
            if (debugLog) Debug.Log("[PlayerMainManager] INTERACT by E");
            interactor?.TryInteract();
        }

        // W: 월드에서는 예약키
        if (Input.GetKeyDown(keyBackOrReserved))
        {
            if (debugLog) Debug.Log("[PlayerMainManager] W pressed (reserved in world)");
        }
    }

    //  여기서는 "대화 활성"은 빼야 함. (대화는 Update 상단에서 처리)
    private bool IsInputBlockedByCutsceneOnly(out string reason)
    {
        reason = "";

        bool menuOpen = (menu != null && menu.IsOpen);

        bool gmLock = (gameManager != null && gameManager.isAction);
        if (gmLock && !menuOpen)
        {
            reason = "GameManager.isAction=true";
            return true;
        }

        if (DialogueManager.instance != null)
        {
            // 대화가 없는 상태에서 blockInput만 true로 남아도 입력 전체가 영구 차단되지 않게 방어
            if (DialogueManager.instance.isDialogueActive && DialogueManager.instance.blockInput)
            {
                reason = "Dialogue(blockInput=true, isDialogueActive=true)";
                return true;
            }
        }

        return false;
    }

    private void LogBlockState(bool blocked, string reason)
    {
        if (!debugLog) return;

        if (_lastBlocked == blocked && _lastBlockReason == reason)
            return;

        _lastBlocked = blocked;
        _lastBlockReason = reason;

        if (blocked)
            Debug.Log($"[PlayerMainManager] INPUT BLOCKED: {reason}");
        else
            Debug.Log("[PlayerMainManager] INPUT UNBLOCKED");
    }

    [ContextMenu("Debug/Dump Input State")]
    public void DebugDumpInputState()
    {
        DebugDumpInputState("MANUAL");
    }

    private void DebugDumpInputState(string context)
    {
        if (!debugLog && !verboseInputDebug) return;

        bool moveEnabled = (move != null && move.enabled);
        bool interactorEnabled = (interactor != null && interactor.enabled);
        bool menuOpen = (menu != null && menu.IsOpen);
        bool gmAction = (gameManager != null && gameManager.isAction);

        bool dmExists = DialogueManager.instance != null;
        bool dmActive = dmExists && DialogueManager.instance.isDialogueActive;
        bool dmBlock = dmExists && DialogueManager.instance.blockInput;

        Debug.Log(
            "[PlayerMainManager] INPUT STATE " +
            $"ctx={context}, move.enabled={moveEnabled}, interactor.enabled={interactorEnabled}, " +
            $"menuOpen={menuOpen}, gm.isAction={gmAction}, dm.active={dmActive}, dm.blockInput={dmBlock}, " +
            $"keys(←↑→↓/QWE)=({Input.GetKey(KeyCode.LeftArrow)},{Input.GetKey(KeyCode.UpArrow)},{Input.GetKey(KeyCode.RightArrow)},{Input.GetKey(KeyCode.DownArrow)}/{Input.GetKey(KeyCode.Q)},{Input.GetKey(KeyCode.W)},{Input.GetKey(KeyCode.E)})"
        );
    }
}
