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
    [SerializeField] private bool debugQEInput = true;
    [SerializeField] private bool verboseInputDebug = false;
    [SerializeField] private float verboseInputDebugInterval = 0.5f;

    private bool _lastBlocked = false;
    private string _lastBlockReason = "";
    private float _nextVerboseDebugAt = 0f;
    private bool _wasMenuKeyHeld;
    private bool _wasInteractKeyHeld;
    private bool _wasBackKeyHeld;

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

    private void OnEnable()
    {
        ResetKeyLatchState();
    }

    private void Update()
    {
        ResolveRuntimeRefs();

        bool rawMenuDown = keyMenu != KeyCode.None && Input.GetKeyDown(keyMenu);
        bool rawInteractDown = keyInteractOrSubmit != KeyCode.None && Input.GetKeyDown(keyInteractOrSubmit);
        bool rawMenuHeld = keyMenu != KeyCode.None && Input.GetKey(keyMenu);
        bool rawInteractHeld = keyInteractOrSubmit != KeyCode.None && Input.GetKey(keyInteractOrSubmit);

        bool menuPressed = ConsumeKeyPress(keyMenu, ref _wasMenuKeyHeld);
        bool interactPressed = ConsumeKeyPress(keyInteractOrSubmit, ref _wasInteractKeyHeld);
        bool backPressed = ConsumeKeyPress(keyBackOrReserved, ref _wasBackKeyHeld);

        DebugLogQEInput(rawMenuDown, rawMenuHeld, menuPressed, rawInteractDown, rawInteractHeld, interactPressed);

        // =========================
        // DIALOGUE MODE (E는 대사 넘기기 전용)
        // =========================
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
        {
            move?.SetMoveInput(0, 0, false, false, false, false);

            // 컷씬이면 스킵 금지
            if (!DialogueManager.instance.blockInput && interactPressed)
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
            if (menuPressed || backPressed)
            {
                if (debugLog) Debug.Log("[PlayerMainManager] MENU BACK/CLOSE by Q/W");
                menu.BackOrClose();
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow)) menu.NavigateVertical(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow)) menu.NavigateVertical(+1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) menu.NavigateHorizontal(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) menu.NavigateHorizontal(+1);

            if (interactPressed) menu.SubmitCurrent();
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
        if (menu != null && menuPressed)
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

        // 상호작용: E (Action Lock 중에는 무시)
        if (interactPressed)
        {
            if (gameManager != null && gameManager.isAction)
            {
                if (debugLog) Debug.Log("[PlayerMainManager] INTERACT ignored (GameManager.isAction=true)");
                return;
            }

            if (debugLog) Debug.Log("[PlayerMainManager] INTERACT by E");
            interactor?.TryInteract();
        }

        // W: 월드에서는 예약키
        if (backPressed)
        {
            if (debugLog) Debug.Log("[PlayerMainManager] W pressed (reserved in world)");
        }
    }

    private static bool ConsumeKeyPress(KeyCode key, ref bool wasHeld)
    {
        if (key == KeyCode.None)
        {
            wasHeld = false;
            return false;
        }

        bool held = Input.GetKey(key);
        bool pressed = Input.GetKeyDown(key) || (held && !wasHeld);
        wasHeld = held;
        return pressed;
    }

    private void ResetKeyLatchState()
    {
        _wasMenuKeyHeld = keyMenu != KeyCode.None && Input.GetKey(keyMenu);
        _wasInteractKeyHeld = keyInteractOrSubmit != KeyCode.None && Input.GetKey(keyInteractOrSubmit);
        _wasBackKeyHeld = keyBackOrReserved != KeyCode.None && Input.GetKey(keyBackOrReserved);
    }

    private void ResolveRuntimeRefs()
    {
        if (!move) move = GetComponent<PlayerMove>();
        if (!interactor) interactor = GetComponent<PlayerInteractor>();
        if (!gameManager) gameManager = GameManager.Instance;
        if (!menu) menu = FindObjectOfType<MenuController>(true);
    }

    private void DebugLogQEInput(
        bool rawMenuDown,
        bool rawMenuHeld,
        bool menuPressed,
        bool rawInteractDown,
        bool rawInteractHeld,
        bool interactPressed
    )
    {
        if (!debugQEInput) return;
        if (!rawMenuDown && !rawMenuHeld && !menuPressed && !rawInteractDown && !rawInteractHeld && !interactPressed)
            return;

        bool menuOpen = menu != null && menu.IsOpen;
        bool gmAction = gameManager != null && gameManager.isAction;
        bool dmExists = DialogueManager.instance != null;
        bool dmActive = dmExists && DialogueManager.instance.isDialogueActive;
        bool dmBlock = dmExists && DialogueManager.instance.blockInput;

        Debug.Log(
            "[PlayerMainManager][Q/E] " +
            $"Q(rawDown={rawMenuDown}, held={rawMenuHeld}, pressed={menuPressed}) " +
            $"E(rawDown={rawInteractDown}, held={rawInteractHeld}, pressed={interactPressed}) " +
            $"refs(move={move != null}, interactor={interactor != null}, menu={menu != null}, gm={gameManager != null}) " +
            $"state(menuOpen={menuOpen}, gm.isAction={gmAction}, dm.active={dmActive}, dm.blockInput={dmBlock})",
            this
        );
    }

    //  여기서는 "대화 활성"은 빼야 함. (대화는 Update 상단에서 처리)
    private bool IsInputBlockedByCutsceneOnly(out string reason)
    {
        reason = "";

        bool menuOpen = (menu != null && menu.IsOpen);

        // TriggerRouter 기반 입력 차단은 GameManager 잠금값과 별도로 한번 더 방어
        // (중간 Step에서 잠금 상태가 변해도 라우트 실행 중이면 이동 차단 유지)
        var routers = FindObjectsOfType<TriggerRouter>(true);
        for (int i = 0; i < routers.Length; i++)
        {
            var router = routers[i];
            if (router == null) continue;
            if (router.IsBlockingInputRouteRunning() && !menuOpen)
            {
                reason = "TriggerRouter(blockPlayerInputWhileRunning=true)";
                return true;
            }
        }

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
