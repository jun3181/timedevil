using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneArrivalApplier : MonoBehaviour
{
    private static SceneArrivalApplier _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        var go = new GameObject("SceneArrivalApplier (Runtime)");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SceneArrivalApplier>();
    }

    [Header("Player Find")]
    [SerializeField] private int maxFindPlayerFrames = 60;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
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

    private IEnumerator Start()
    {
        yield return CoApply(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(CoApply(scene));
    }

    private IEnumerator CoApply(Scene scene)
    {
        // Let scene-local Awake/Start bootstrap components settle first, then apply the final arrival.
        yield return null;

        bool hasRequest = SceneArrivalContext.TryConsumeForScene(scene.name, out SceneArrivalRequest request);
        if (!hasRequest)
        {
            if (!TryBuildDefaultRequest(scene.name, out request))
                yield break;
        }

        yield return Apply(request, scene.name);
    }

    private IEnumerator Apply(SceneArrivalRequest request, string activeSceneName)
    {
        if (request == null) yield break;

        Transform player = null;
        int waitFrames = Mathf.Max(1, maxFindPlayerFrames);
        for (int i = 0; i < waitFrames; i++)
        {
            player = SaveSystem.ResolvePlayerTransform();
            if (player != null) break;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning($"[SceneArrivalApplier] Player not found for scene='{activeSceneName}' kind={request.kind}.");
            ClearLegacyReturnIfNeeded(request);
            yield break;
        }

        bool moved = TryResolvePosition(request, activeSceneName, out Vector3 targetPosition, out bool keepPlayerZ);
        Vector3 from = player.position;

        if (moved)
        {
            if (keepPlayerZ)
                targetPosition.z = player.position.z;

            player.position = targetPosition;
            Physics2D.SyncTransforms();
        }

        ApplyCamera(request, player, moved ? targetPosition : player.position, from);

        if (request.kind == SceneArrivalKind.BattleReturn)
        {
            RestoreEnemySnapshot(request);
            ApplyReturnGrace(request);
            ApplyOverlapSuppression(request, moved ? targetPosition : player.position);
            ClearLegacyReturnIfNeeded(request);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ForceClearActionLocks();

        if (debugLog)
        {
            Debug.Log(
                $"[SceneArrivalApplier] applied scene='{activeSceneName}' kind={request.kind} " +
                $"moved={moved} pos={(moved ? targetPosition.ToString() : "(keep)")} camera={request.camera.hasCamera}"
            );
        }
    }

    private bool TryBuildDefaultRequest(string sceneName, out SceneArrivalRequest request)
    {
        request = null;

        var profile = FindObjectOfType<SceneEntryProfile>(true);
        if (profile == null || !profile.TryGetDefault(out SceneEntryDefinition entry))
            return false;

        request = SceneArrivalRequest.Default(sceneName);
        ApplyEntryToRequest(request, entry);
        return true;
    }

    private bool TryResolvePosition(
        SceneArrivalRequest request,
        string activeSceneName,
        out Vector3 position,
        out bool keepPlayerZ)
    {
        position = Vector3.zero;
        keepPlayerZ = request.keepPlayerZ;

        if (request.kind == SceneArrivalKind.MyroomEntry)
        {
            var myroomApplier = FindObjectOfType<MyroomEntryApplier>(true);
            if (myroomApplier != null && myroomApplier.TryGetSpawn(request.myroomEntryPoint, out Transform spawn))
            {
                position = spawn.position;
                keepPlayerZ = true;
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.spawnKey))
        {
            if (TryResolveProfileEntry(request.spawnKey, out SceneEntryDefinition profileEntry))
            {
                ApplyEntryToRequest(request, profileEntry);
                if (profileEntry.HasSpawn)
                {
                    position = profileEntry.spawnPoint.position;
                    keepPlayerZ = profileEntry.keepPlayerZ;
                    return true;
                }
            }

            if (TryResolveLegacySpawnPoint(activeSceneName, request.spawnKey, out Transform legacySpawn))
            {
                position = legacySpawn.position;
                keepPlayerZ = true;
                return true;
            }
        }

        if (request.hasWorldPosition)
        {
            position = request.worldPosition;
            return true;
        }

        return false;
    }

    private bool TryResolveProfileEntry(string key, out SceneEntryDefinition entry)
    {
        entry = null;

        var profiles = FindObjectsOfType<SceneEntryProfile>(true);
        for (int i = 0; i < profiles.Length; i++)
        {
            var profile = profiles[i];
            if (profile != null && profile.TryGetEntry(key, out entry))
                return true;
        }

        return false;
    }

    private bool TryResolveLegacySpawnPoint(string sceneName, string key, out Transform spawn)
    {
        spawn = null;

        var points = FindObjectsOfType<SceneEntrySpawnPoint>(true);
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            if (point != null && point.MatchesEntry(sceneName, key))
            {
                spawn = point.transform;
                SceneEntrySpawnContext.TryConsume(sceneName, key);
                return true;
            }
        }

        return false;
    }

    private static void ApplyEntryToRequest(SceneArrivalRequest request, SceneEntryDefinition entry)
    {
        if (request == null || entry == null) return;

        if (entry.HasSpawn)
        {
            request.hasWorldPosition = true;
            request.worldPosition = entry.spawnPoint.position;
            request.keepPlayerZ = entry.keepPlayerZ;
        }

        if (!entry.applyCamera) return;

        Vector3 fallbackPosition = entry.HasSpawn ? entry.spawnPoint.position : request.worldPosition;
        request.camera = entry.ToCameraRequest(fallbackPosition);
    }

    private void ApplyCamera(SceneArrivalRequest request, Transform player, Vector3 toPosition, Vector3 fromPosition)
    {
        if (request == null || !request.camera.hasCamera || CameraManager.Instance == null)
            return;

        var camera = request.camera;
        CameraManager.Instance.ReacquireVcam(camera.preferredVcamName, logWhenMissing: false);

        float? size = camera.orthoSize > 0f ? camera.orthoSize : (float?)null;
        Collider2D bounds = ResolveBounds(camera.boundsName);
        Vector3 delta = toPosition - fromPosition;
        delta.z = 0f;

        switch (camera.mode)
        {
            case CameraModeId.Fixed:
                CameraManager.Instance.SetFixed(camera.fixedPosition, size);
                CameraManager.Instance.SnapCameraTo(camera.fixedPosition);
                break;

            case CameraModeId.Cutscene:
                CameraManager.Instance.SetCutscene(camera.fixedPosition, size);
                CameraManager.Instance.SnapCameraTo(camera.fixedPosition);
                break;

            case CameraModeId.FollowConfined:
                if (bounds != null) CameraManager.Instance.SetFollowConfined(player, bounds, size);
                else CameraManager.Instance.SetFollowFree(player, size);
                CameraManager.Instance.NotifyTargetWarp(player, delta);
                break;

            case CameraModeId.FollowFree:
            default:
                CameraManager.Instance.SetFollowFree(player, size);
                CameraManager.Instance.NotifyTargetWarp(player, delta);
                break;
        }
    }

    private Collider2D ResolveBounds(string boundsName)
    {
        if (string.IsNullOrWhiteSpace(boundsName))
            return null;

        var all = FindObjectsOfType<Collider2D>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var candidate = all[i];
            if (candidate != null && candidate.name == boundsName)
                return candidate;
        }

        return null;
    }

    private void RestoreEnemySnapshot(SceneArrivalRequest request)
    {
        if (request == null || !request.restoreEnemySnapshot) return;
        if (string.IsNullOrWhiteSpace(request.enemyInstanceId)) return;
        if (WorldNPCStateService.Instance == null) return;

        if (!WorldNPCStateService.Instance.TryGetSnapshot(request.enemyInstanceId, out EnemySnapshot snap))
            return;

        GameObject enemy = FindEnemy(request.enemyInstanceId, snap.transformPath, request.enemyNameInScene);
        if (enemy == null)
        {
            if (debugLog)
                Debug.LogWarning($"[SceneArrivalApplier] enemy snapshot exists but target not found id='{request.enemyInstanceId}'");
            return;
        }

        snap.ApplyTo(enemy);
        Physics2D.SyncTransforms();
    }

    private GameObject FindEnemy(string instanceId, string transformPath, string fallbackName)
    {
        var ids = FindObjectsOfType<EnemyInstanceId>(true);
        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (id != null && id.Id == instanceId)
                return id.gameObject;
        }

        if (!string.IsNullOrWhiteSpace(transformPath))
        {
            var all = FindObjectsOfType<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t != null && BuildTransformPath(t) == transformPath)
                    return t.gameObject;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
            return GameObject.Find(fallbackName);

        return null;
    }

    private static string BuildTransformPath(Transform t)
    {
        if (t == null) return string.Empty;
        var stack = new System.Collections.Generic.Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack.ToArray());
    }

    private void ApplyReturnGrace(SceneArrivalRequest request)
    {
        float seconds = Mathf.Max(0f, request.graceSeconds);
        if (seconds <= 0f)
        {
            PlayerReturnContext.IsInGracePeriod = false;
            PlayerReturnContext.GraceSecondsPending = 0f;
            return;
        }

        StartCoroutine(CoGrace(seconds));
    }

    private IEnumerator CoGrace(float seconds)
    {
        PlayerReturnContext.IsInGracePeriod = true;
        PlayerReturnContext.GraceSecondsPending = 0f;

        yield return new WaitForSecondsRealtime(seconds);

        PlayerReturnContext.IsInGracePeriod = false;
    }

    private void ApplyOverlapSuppression(SceneArrivalRequest request, Vector3 center)
    {
        if (request == null || !request.useOverlapSuppression) return;
        if (request.overlapRadius <= 0f || request.overlapSeconds <= 0f) return;

        var cols = Physics2D.OverlapCircleAll(center, request.overlapRadius);
        if (cols == null || cols.Length == 0) return;

        for (int i = 0; i < cols.Length; i++)
        {
            var col = cols[i];
            if (col == null) continue;

            var trigger = col.GetComponent<TriggerGet>();
            if (trigger == null) trigger = col.GetComponentInParent<TriggerGet>();
            if (trigger == null) continue;

            StartCoroutine(CoSuppressTrigger(trigger, col, request.overlapSeconds));
        }
    }

    private IEnumerator CoSuppressTrigger(TriggerGet trigger, Collider2D collider, float seconds)
    {
        if (trigger != null) trigger.enabled = false;
        if (collider != null) collider.enabled = false;

        yield return new WaitForSecondsRealtime(seconds);

        if (trigger != null) trigger.enabled = true;
        if (collider != null) collider.enabled = true;
    }

    private static void ClearLegacyReturnIfNeeded(SceneArrivalRequest request)
    {
        if (request == null || request.kind != SceneArrivalKind.BattleReturn) return;
        bool isInGracePeriod = PlayerReturnContext.IsInGracePeriod;
        float graceSecondsPending = PlayerReturnContext.GraceSecondsPending;

        PlayerReturnContext.ClearReturnCore();

        PlayerReturnContext.IsInGracePeriod = isInGracePeriod;
        PlayerReturnContext.GraceSecondsPending = graceSecondsPending;
    }
}
