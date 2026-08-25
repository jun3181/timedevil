using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(2200)]
public class BattleVictoryReturnApplier : MonoBehaviour
{
    private static BattleVictoryReturnApplier _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        BattleVictoryReturnApplier existing = FindObjectOfType<BattleVictoryReturnApplier>(true);
        if (existing != null)
        {
            _instance = existing;
            if (existing.transform.parent == null)
                DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new("BattleVictoryReturnApplier (Runtime)");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<BattleVictoryReturnApplier>();
    }

    [Header("Apply Timing")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool applyOnSceneLoaded = true;
    [SerializeField, Min(0)] private int waitFramesBeforeApply = 3;
    [SerializeField, Min(1)] private int maxWaitFrames = 120;
    [SerializeField] private bool waitUntilDialogueFree = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private Coroutine _applyCoroutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (applyOnSceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (applyOnSceneLoaded)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_applyCoroutine != null)
        {
            StopCoroutine(_applyCoroutine);
            _applyCoroutine = null;
        }
    }

    private void Start()
    {
        if (applyOnStart)
            ScheduleApply();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScheduleApply();
    }

    private void ScheduleApply()
    {
        if (!BattleVictoryReturnContext.HasPending)
            return;

        if (_applyCoroutine != null)
            StopCoroutine(_applyCoroutine);

        _applyCoroutine = StartCoroutine(CoApplyPendingVictoryRoute());
    }

    private IEnumerator CoApplyPendingVictoryRoute()
    {
        for (int i = 0; i < waitFramesBeforeApply; i++)
            yield return null;

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!BattleVictoryReturnContext.TryPeekForScene(activeSceneName, out BattleVictoryRouteRequest request))
        {
            _applyCoroutine = null;
            yield break;
        }

        TriggerRouter router = null;
        int waitedFrames = 0;
        while (router == null && waitedFrames < maxWaitFrames)
        {
            router = ResolveRouter(request);
            if (router != null)
                break;

            waitedFrames++;
            yield return null;
        }

        if (router == null)
        {
            if (debugLog)
                Debug.LogWarning($"[BattleVictoryReturnApplier] TriggerRouter was not found for victory route '{request.routeKey}'. Pending route was kept.", this);

            _applyCoroutine = null;
            yield break;
        }

        PlayerMove playerMove = null;
        waitedFrames = 0;
        while (playerMove == null && waitedFrames < maxWaitFrames)
        {
            playerMove = FindObjectOfType<PlayerMove>(true);
            if (playerMove != null)
                break;

            waitedFrames++;
            yield return null;
        }

        if (waitUntilDialogueFree)
        {
            while (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
                yield return null;
        }

        if (!BattleVictoryReturnContext.TryConsumeForScene(activeSceneName, out request))
        {
            _applyCoroutine = null;
            yield break;
        }

        Collider2D playerCollider = ResolvePlayerCollider(playerMove);
        var ctx = new TriggerContext(
            trigger: null,
            router: router,
            instigator: playerMove != null ? playerMove.gameObject : null,
            instigatorCollider: playerCollider,
            playerMove: playerMove
        );

        if (debugLog)
        {
            Debug.Log(
                $"[BattleVictoryReturnApplier] RequestRoute key='{request.routeKey}' " +
                $"router='{router.name}' enemy='{request.enemyId ?? "(none)"}'",
                this);
        }

        router.RequestRoute(request.routeKey, ctx);
        _applyCoroutine = null;
    }

    private TriggerRouter ResolveRouter(BattleVictoryRouteRequest request)
    {
        TriggerRouter[] routers = FindObjectsOfType<TriggerRouter>(true);
        if (routers == null || routers.Length == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(request.routerTransformPath))
        {
            for (int i = 0; i < routers.Length; i++)
            {
                TriggerRouter candidate = routers[i];
                if (candidate == null) continue;

                string path = BattleVictoryReturnContext.GetTransformPath(candidate.transform);
                if (string.Equals(path, request.routerTransformPath, System.StringComparison.Ordinal))
                    return candidate;
            }
        }

        return null;
    }

    private static Collider2D ResolvePlayerCollider(PlayerMove playerMove)
    {
        if (playerMove == null)
            return null;

        Collider2D collider = playerMove.GetComponent<Collider2D>();
        if (collider != null)
            return collider;

        collider = playerMove.GetComponentInChildren<Collider2D>(true);
        if (collider != null)
            return collider;

        return playerMove.GetComponentInParent<Collider2D>();
    }
}
