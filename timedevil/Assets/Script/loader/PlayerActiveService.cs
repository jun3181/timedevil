// Assets/Script/Services/PlayerActiveService.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerActiveService : MonoBehaviour
{
    public static PlayerActiveService Instance { get; private set; }

    [Header("Policy")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoFindPlayerOnAwake = true;
    [SerializeField] private bool autoRefindOnSceneLoaded = true;

    [Header("Player Ref (optional)")]
    [Tooltip("비워두면 자동 탐색( PlayerMainManager -> PlayerMove -> tag 'Player' )")]
    [SerializeField] private GameObject playerRoot;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Rigidbody2D _rb;
    private PlayerMove _pm;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        if (autoFindPlayerOnAwake)
            ResolvePlayer(force: true);

        if (autoRefindOnSceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬마다 Player가 새로 생기는 프로젝트면 재탐색 필요
        ResolvePlayer(force: true);
    }

    public GameObject CurrentPlayer => ResolvePlayer(force: false);

    public void SetPlayer(GameObject go)
    {
        playerRoot = go;
        CacheComponents();
    }

    public bool SetActive(bool active, bool syncPhysics = true, bool resetVelocity = true, bool clearMoveInput = true)
    {
        var go = ResolvePlayer(force: false);
        if (!go)
        {
            if (debugLog) Debug.LogWarning("[PlayerActiveService] playerRoot not found");
            return false;
        }

        // disable 직전 안전처리
        if (!active)
        {
            if (clearMoveInput && _pm != null)
                _pm.SetMoveInput(0, 0, false, false, false, false);

            if (resetVelocity && _rb != null)
                _rb.velocity = Vector2.zero;

            if (debugLog) Debug.Log("[PlayerActiveService] SetActive(false) -> " + go.name, go);
            go.SetActive(false);
            return true;
        }

        // enable
        if (debugLog) Debug.Log("[PlayerActiveService] SetActive(true) -> " + go.name, go);
        go.SetActive(true);

        // enable 직후엔 컴포넌트 캐시가 바뀔 수 있어서 재캐시
        CacheComponents();

        if (resetVelocity && _rb != null)
            _rb.velocity = Vector2.zero;

        if (clearMoveInput && _pm != null)
            _pm.SetMoveInput(0, 0, false, false, false, false);

        if (syncPhysics)
            Physics2D.SyncTransforms();

        return true;
    }

    // -----------------------
    // internal
    // -----------------------
    private GameObject ResolvePlayer(bool force)
    {
        if (!force && playerRoot != null) return playerRoot;

        // 1) PlayerMainManager
        PlayerMainManager pmm = null;
#if UNITY_2020_1_OR_NEWER
        pmm = Object.FindObjectOfType<PlayerMainManager>(true);
#else
        pmm = Object.FindObjectOfType<PlayerMainManager>();
#endif
        if (pmm != null)
        {
            playerRoot = pmm.gameObject;
            CacheComponents();
            if (debugLog) Debug.Log("[PlayerActiveService] found PlayerMainManager -> " + playerRoot.name, playerRoot);
            return playerRoot;
        }

        // 2) PlayerMove
        PlayerMove pm = null;
#if UNITY_2020_1_OR_NEWER
        pm = Object.FindObjectOfType<PlayerMove>(true);
#else
        pm = Object.FindObjectOfType<PlayerMove>();
#endif
        if (pm != null)
        {
            playerRoot = pm.gameObject;
            CacheComponents();
            if (debugLog) Debug.Log("[PlayerActiveService] found PlayerMove -> " + playerRoot.name, playerRoot);
            return playerRoot;
        }

        // 3) tag "Player"
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
        {
            playerRoot = tagged;
            CacheComponents();
            if (debugLog) Debug.Log("[PlayerActiveService] found tag Player -> " + playerRoot.name, playerRoot);
            return playerRoot;
        }

        playerRoot = null;
        _rb = null;
        _pm = null;
        return null;
    }

    private void CacheComponents()
    {
        if (!playerRoot) { _rb = null; _pm = null; return; }

        // inactive 포함해서 찾기 위해 GetComponentInChildren(true) 사용 가능하지만
        // 너 구조상 PlayerRoot에 달려있을 확률이 커서 GetComponent로 충분
        _rb = playerRoot.GetComponent<Rigidbody2D>();
#if UNITY_2020_1_OR_NEWER
        _pm = playerRoot.GetComponent<PlayerMove>() ?? playerRoot.GetComponentInChildren<PlayerMove>(true);
#else
        _pm = playerRoot.GetComponent<PlayerMove>();
#endif
    }
}
