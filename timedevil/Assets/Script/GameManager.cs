// Assets/Script/GameManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dialogue UI (Simple Talk Panel)")]
    public TextMeshProUGUI talkText;
    public GameObject talkPanel;

    private GameObject scanObject;

    // =========================
    // Action Lock (Cutscene/Input)
    // =========================
    [Header("Action Lock (Cutscene/Input)")]
    [Tooltip("외부에서 읽는 용도. 내부 LockCount로 관리됩니다.")]
    public bool isAction;

    [SerializeField] private bool debugActionLockLog = false;
    private int _actionLockCount = 0;

    // =========================
    // Interaction UI State (A안 핵심)
    // =========================
    [Header("Interaction UI State")]
    [SerializeField] private bool _isInteractionUIOpen = false;

    // 상호작용 UI가 열릴 때도 이동/행동을 막고 싶으면,
    // LockAction/UnlockAction을 “카운트 방식”으로 같이 사용한다.
    private bool _interactionLockHeld = false;

    public bool IsInteractionUIOpen => _isInteractionUIOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_actionLockCount == 0 && isAction)
        {
            if (debugActionLockLog)
                Debug.Log($"[GameManager] Recovered stale isAction=true after scene load: {scene.name}");

            isAction = false;
        }
    }

    // ✅ Timeline SignalReceiver에서 바로 걸기 좋은 이름
    public void DisableControls() => LockAction();
    public void EnableControls() => UnlockAction();

    // ✅ 직접 잠금/해제 API (isAction은 여기서만 변하게)
    public void LockAction()
    {
        _actionLockCount++;
        isAction = true;

        if (debugActionLockLog)
            Debug.Log($"[GameManager] LockAction -> {_actionLockCount}");
    }

    public void UnlockAction()
    {
        _actionLockCount = Mathf.Max(0, _actionLockCount - 1);
        isAction = (_actionLockCount > 0);

        if (debugActionLockLog)
            Debug.Log($"[GameManager] UnlockAction -> {_actionLockCount}");
    }

    public void ForceClearActionLocks()
    {
        _actionLockCount = 0;
        isAction = false;

        // 인터랙션 잠금 플래그도 리셋
        _interactionLockHeld = false;

        if (debugActionLockLog)
            Debug.Log("[GameManager] ForceClearActionLocks");
    }

    // =========================
    // Interaction API (기존 Action 역할 대체)
    // - isAction을 직접 true/false로 만지지 않는다 (A안 핵심)
    // =========================
    public void Action(GameObject scanObj)
    {
        if (talkText == null || talkPanel == null)
        {
            Debug.LogWarning("[GameManager] talk UI missing");
            return;
        }

        // 이미 열려 있으면 닫기
        if (_isInteractionUIOpen)
        {
            CloseInteractionUI();
            return;
        }

        // 열기
        if (scanObj == null)
        {
            Debug.LogWarning("[GameManager] scanObj is null");
            return;
        }

        OpenInteractionUI(scanObj);
    }

    private void OpenInteractionUI(GameObject scanObj)
    {
        _isInteractionUIOpen = true;
        scanObject = scanObj;

        talkText.text = $"{scanObj.name} 과(와) 상호작용!";
        talkPanel.SetActive(true);

        // ✅ 상호작용 중에도 이동/행동 불가를 원하면 LockAction 사용(직접 isAction=true 금지)
        if (!_interactionLockHeld)
        {
            LockAction();
            _interactionLockHeld = true;
        }
    }

    private void CloseInteractionUI()
    {
        _isInteractionUIOpen = false;
        scanObject = null;

        talkPanel.SetActive(false);

        // ✅ 열 때 잡았던 잠금만 정확히 반환
        if (_interactionLockHeld)
        {
            UnlockAction();
            _interactionLockHeld = false;
        }
    }
}
