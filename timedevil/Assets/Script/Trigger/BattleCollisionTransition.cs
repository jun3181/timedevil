using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public class BattleCollisionTransition : MonoBehaviour
{
    [Header("Battle")]
    [SerializeField] private string battleSceneName = "battle";
    [SerializeField] private EnemyDatabaseSO enemyDatabase;
    [SerializeField] private EnemySO encounterEnemy;
    [SerializeField] private string enemyId = "Enemy1";
    [SerializeField] private Transform enemySnapshotTarget;
    [SerializeField] private Transform chasingObject;
    [SerializeField] private bool forceActivateChasingObject = true;

    [Header("Encounter Completion")]
    [SerializeField] private bool consumeOnEnemyDefeat = false;
    [SerializeField] private string encounterKey = "";
    [SerializeField] private bool disableWhenEncounterConsumed = true;

    [Header("Return")]
    [SerializeField] private Transform returnPointOverride;
    [SerializeField] private float graceSeconds = 0.5f;
    [SerializeField] private bool requestCameraRebind = false;
    [SerializeField] private string worldVcamName = "CM vcam1";

    [Header("Return Camera")]
    [SerializeField] private bool useReturnCameraOverride = false;
    [SerializeField] private CameraModeId returnCameraModeOverride = CameraModeId.FollowFree;
    [SerializeField] private Transform returnFixedCameraAnchorOverride;
    [SerializeField] private Collider2D returnConfinerBoundsOverride;
    [SerializeField] private float returnOrthoSizeOverride = 0f;
    [SerializeField] private bool captureCameraSnapshot = true;
    [SerializeField] private Transform forceFixedReturnAnchor;

    [Header("Filter")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool allowTagFallback = false;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool disableAfterEnter = true;
    [SerializeField] private float reenterBlockSeconds = 0.5f;

    [Header("Pause On Enter")]
    [SerializeField] private Transform pauseTargetObject;
    [SerializeField] private Rigidbody2D pauseTargetRigidbody2D;
    [SerializeField] private MonoBehaviour pauseTargetController;
    [SerializeField] private float pauseSecondsBeforeBattle = 0.12f;

    [Header("Enemy Reactivation Delay On Return")]
    [SerializeField] private bool delayEnemySnapshotTargetOnReturn = true;
    [SerializeField] private float enemySnapshotReactivateSeconds = 1.0f;

    [Header("Follow After Reenter Block")]
    [SerializeField] private bool followAfterReenterBlock = true;
    [SerializeField] private Transform followTargetObject;
    [SerializeField] private float followMoveSpeed = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool _entered;
    private bool _followArmedAfterReturn;
    private static readonly System.Collections.Generic.Dictionary<string, float> _reenterBlockedUntil = new();
    private static readonly System.Collections.Generic.Dictionary<string, ForcedChasingState> _forceStateAfterReturn = new();
    private static DelayCoroutineRunner _delayRunner;
    private static bool _returnRestoreHooked;

    private sealed class DelayCoroutineRunner : MonoBehaviour { }


    private struct ForcedChasingState
    {
        public bool hasValue;
        public Vector3 position;
        public Quaternion rotation;
        public bool delayActivation;
        public float activationDelaySeconds;
    }

    private void Start()
    {
        SyncEnemyIdFromEncounterEnemy();

        if (ApplyConsumedEncounterIfNeeded())
            return;

        if (playerTransform == null)
        {
            var pm = FindObjectOfType<PlayerMainManager>(true);
            if (pm != null) playerTransform = pm.transform;
        }

        ApplyForcedReturnStateIfPending();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_entered) return;
        if (!other || !IsPlayerTransform(other.transform)) return;
        if (IsBlockedByCooldown()) return;
        BeginBattleTransition(other.transform, other.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_entered) return;
        if (!other || !IsPlayerTransform(other.transform)) return;
        if (IsBlockedByCooldown()) return;
        BeginBattleTransition(other.transform, other.name);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision2D(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision2D(collision);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_entered) return;
        if (collision == null || collision.collider == null) return;
        var other = collision.collider;
        if (!IsPlayerTransform(other.transform)) return;
        if (IsBlockedByCooldown()) return;
        BeginBattleTransition(other.transform, other.name);
    }


    public bool TryEnterFromExternal(Collider2D other)
    {
        if (_entered) return false;
        if (other == null) return false;
        if (!IsPlayerTransform(other.transform)) return false;
        if (IsBlockedByCooldown()) return false;

        BeginBattleTransition(other.transform, other.name);
        return true;
    }

    public bool TryEnterFromExternal(Collider other)
    {
        if (_entered) return false;
        if (other == null) return false;
        if (!IsPlayerTransform(other.transform)) return false;
        if (IsBlockedByCooldown()) return false;

        BeginBattleTransition(other.transform, other.name);
        return true;
    }

    private void BeginBattleTransition(Transform player, string colliderName)
    {
        _entered = true;
        StartCoroutine(CoEnterBattle(player, colliderName));
    }

    private IEnumerator CoEnterBattle(Transform player, string colliderName)
    {
        var pauseState = ApplyPauseOnEnter(player);
        float wait = Mathf.Max(0f, pauseSecondsBeforeBattle);
        if (wait > 0f)
        {
            float elapsed = 0f;
            while (elapsed < wait)
            {
                MaintainPauseState(pauseState);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        EnterBattle(player, colliderName);
    }

    private void EnterBattle(Transform player, string colliderName)
    {
        string resolvedEnemyId = ResolveEnemyId();

        if (forceActivateChasingObject)
            SaveForcedChasingStateForReturn();

        var enemy = chasingObject != null
            ? chasingObject
            : (enemySnapshotTarget != null ? enemySnapshotTarget : transform);
        var returnPos = returnPointOverride != null ? returnPointOverride.position : player.position;
        RegisterCooldown();

        bool restoreCam = false;
        CameraModeId camMode = CameraModeId.Fixed;
        float camOrtho = 0f;
        Vector2 camFixedPos = returnPos;
        string camBoundsName = null;

        if (useReturnCameraOverride)
        {
            restoreCam = true;
            camMode = returnCameraModeOverride;
            camOrtho = returnOrthoSizeOverride;
            camFixedPos = returnFixedCameraAnchorOverride != null ? (Vector2)returnFixedCameraAnchorOverride.position : returnPos;
            camBoundsName = returnConfinerBoundsOverride != null ? returnConfinerBoundsOverride.name : null;
        }
        else if (captureCameraSnapshot)
        {
            var cm = CameraManager.Instance != null ? CameraManager.Instance : FindObjectOfType<CameraManager>(true);
            if (cm != null && cm.TryGetSnapshot(out camMode, out camOrtho, out Vector3 fixedPos3, out string boundsName))
            {
                restoreCam = true;
                camFixedPos = new Vector2(fixedPos3.x, fixedPos3.y);
                camBoundsName = string.IsNullOrWhiteSpace(boundsName) ? null : boundsName;
            }

            // 스냅샷 획득 실패 시: 우선 현재 실제 카메라 상태를 사용
            if (!restoreCam)
            {
                var liveCam = Camera.main;
                if (liveCam != null)
                {
                    restoreCam = true;
                    camMode = cm != null ? cm.CurrentMode : CameraModeId.Fixed;
                    camOrtho = liveCam.orthographic ? liveCam.orthographicSize : camOrtho;
                    camFixedPos = new Vector2(liveCam.transform.position.x, liveCam.transform.position.y);
                    camBoundsName = null;
                }
            }

            // 그래도 정보가 없으면 bootstrap을 마지막 fallback으로 사용
            if (!restoreCam)
            {
                var bootstrap = FindObjectOfType<SceneCameraBootstrap>(true);
                if (bootstrap != null)
                {
                    restoreCam = true;
                    camMode = bootstrap.startMode;
                    camOrtho = bootstrap.orthoSize > 0f ? bootstrap.orthoSize : 0f;

                    switch (bootstrap.startMode)
                    {
                        case CameraModeId.Fixed:
                        case CameraModeId.Cutscene:
                            if (bootstrap.fixedOrCutsceneAnchor != null)
                                camFixedPos = bootstrap.fixedOrCutsceneAnchor.position;
                            else if (bootstrap.followTarget != null)
                                camFixedPos = bootstrap.followTarget.position;
                            else
                                camFixedPos = returnPos;
                            camBoundsName = null;
                            break;

                        case CameraModeId.FollowConfined:
                            camBoundsName = bootstrap.confinerBounds != null ? bootstrap.confinerBounds.name : null;
                            break;

                        case CameraModeId.FollowFree:
                            camBoundsName = null;
                            break;
                    }
                }
            }
        }

        if (camMode == CameraModeId.Fixed && forceFixedReturnAnchor != null)
        {
            camFixedPos = forceFixedReturnAnchor.position;
            if (debugLog)
                Debug.Log($"[BattleCollisionTransition] override fixed return camera anchor => '{forceFixedReturnAnchor.name}' ({camFixedPos.x:F2},{camFixedPos.y:F2})");
        }

        PlayerReturnContext.SetReturnFromTrigger(
            returnSceneName: SceneManager.GetActiveScene().name,
            returnPosition: returnPos,
            graceSeconds: Mathf.Max(graceSeconds, reenterBlockSeconds),
            requestCameraRebind: requestCameraRebind,
            targetVcamName: worldVcamName,
            useOverlapSuppression: false,
            overlapRadius: 0f,
            overlapSeconds: 0f,
            restoreCameraState: restoreCam,
            cameraMode: camMode,
            cameraOrthoSize: camOrtho,
            cameraFixedPos: camFixedPos,
            cameraBoundsName: camBoundsName
        );

        if (debugLog)
            Debug.Log($"[BattleCollisionTransition] enter by '{colliderName}' -> scene='{battleSceneName}', enemyId='{resolvedEnemyId}', returnPos=({returnPos.x:F2},{returnPos.y:F2}), camRestore={restoreCam}, camMode={camMode}");

        QueueEncounterConsumptionIfNeeded();

        BattleSceneLoader.Go(battleSceneName, resolvedEnemyId, player, enemy);

        if (disableAfterEnter)
            gameObject.SetActive(false);
    }

    private void QueueEncounterConsumptionIfNeeded()
    {
        if (!consumeOnEnemyDefeat) return;

        string key = ResolveEncounterKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[BattleCollisionTransition] consumeOnEnemyDefeat is on, but encounterKey could not be resolved.", this);
            return;
        }

        BattleEncounterState.SetPending(SceneManager.GetActiveScene().name, key);
    }

    private bool ApplyConsumedEncounterIfNeeded()
    {
        if (!consumeOnEnemyDefeat || !disableWhenEncounterConsumed)
            return false;

        string key = ResolveEncounterKey();
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (!BattleEncounterState.IsConsumed(SceneManager.GetActiveScene().name, key))
            return false;

        if (debugLog)
            Debug.Log($"[BattleCollisionTransition] consumed encounter disabled key='{key}'", this);

        gameObject.SetActive(false);
        return true;
    }

    private string ResolveEncounterKey()
    {
        if (!string.IsNullOrWhiteSpace(encounterKey))
            return encounterKey.Trim();

        return name;
    }

    private string ResolveEnemyId()
    {
        SyncEnemyIdFromEncounterEnemy();

        string resolvedId = !string.IsNullOrWhiteSpace(enemyId) ? enemyId : "Enemy1";

        if (enemyDatabase != null && enemyDatabase.GetById(resolvedId) == null)
            Debug.LogWarning($"[BattleCollisionTransition] Enemy id '{resolvedId}' is not found in EnemyDatabaseSO.");

        return resolvedId;
    }

    private void SyncEnemyIdFromEncounterEnemy()
    {
        if (encounterEnemy == null) return;
        if (string.IsNullOrWhiteSpace(encounterEnemy.enemyId))
        {
            Debug.LogWarning("[BattleCollisionTransition] Encounter Enemy has an empty enemyId.");
            return;
        }

        enemyId = encounterEnemy.enemyId;
    }

    private void OnValidate()
    {
        SyncEnemyIdFromEncounterEnemy();
    }

    private struct PauseState
    {
        public bool hasTarget;
        public Transform target;
        public Vector3 lockedPosition;
        public Rigidbody2D rb;
        public PlayerMove playerMove;
        public bool hasPlayerTarget;
        public Transform playerTarget;
        public Vector3 playerLockedPosition;
        public Rigidbody2D playerRb;
    }

    private PauseState ApplyPauseOnEnter(Transform player)
    {
        Transform target = pauseTargetObject != null ? pauseTargetObject : ResolvePauseTarget(player);
        PlayerMove playerMove = ResolvePausePlayerMove(player);
        Transform playerTarget = playerMove != null ? playerMove.transform : ResolvePauseTarget(player);

        Rigidbody2D rb = pauseTargetRigidbody2D != null
            ? pauseTargetRigidbody2D
            : ResolvePauseRigidbody2D(target);
        Rigidbody2D playerRb = ResolvePauseRigidbody2D(playerTarget);

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (playerRb != null && playerRb != rb)
            playerRb.velocity = Vector2.zero;

        if (playerMove != null)
            playerMove.SetMoveInput(0, 0, false, false, false, false);

        if (pauseTargetController != null)
            pauseTargetController.enabled = false;

        var state = new PauseState
        {
            hasTarget = target != null,
            target = target,
            lockedPosition = target != null ? target.position : Vector3.zero,
            rb = rb,
            playerMove = playerMove,
            hasPlayerTarget = playerTarget != null,
            playerTarget = playerTarget,
            playerLockedPosition = playerTarget != null ? playerTarget.position : Vector3.zero,
            playerRb = playerRb
        };

        return state;
    }

    private void MaintainPauseState(PauseState state)
    {
        if (state.rb != null)
            state.rb.velocity = Vector2.zero;

        if (state.playerRb != null && state.playerRb != state.rb)
            state.playerRb.velocity = Vector2.zero;

        if (state.playerMove != null)
            state.playerMove.SetMoveInput(0, 0, false, false, false, false);

        if (state.hasTarget && state.target != null)
            state.target.position = state.lockedPosition;

        if (state.hasPlayerTarget && state.playerTarget != null && state.playerTarget != state.target)
            state.playerTarget.position = state.playerLockedPosition;
    }

    private Transform ResolvePauseTarget(Transform player)
    {
        if (playerTransform != null)
            return playerTransform;

        if (player == null)
            return null;

        var pm = player.GetComponentInParent<PlayerMove>();
        if (pm != null)
            return pm.transform;

        var manager = player.GetComponentInParent<PlayerMainManager>();
        if (manager != null)
            return manager.transform;

        return player;
    }

    private PlayerMove ResolvePausePlayerMove(Transform player)
    {
        if (player != null)
        {
            var pm = player.GetComponentInParent<PlayerMove>();
            if (pm != null)
                return pm;
        }

        if (playerTransform != null)
        {
            var pm = playerTransform.GetComponent<PlayerMove>();
            if (pm != null)
                return pm;

            pm = playerTransform.GetComponentInChildren<PlayerMove>(true);
            if (pm != null)
                return pm;
        }

        return FindObjectOfType<PlayerMove>(true);
    }

    private static Rigidbody2D ResolvePauseRigidbody2D(Transform target)
    {
        if (target == null)
            return null;

        var rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
            return rb;

        rb = target.GetComponentInParent<Rigidbody2D>();
        if (rb != null)
            return rb;

        return target.GetComponentInChildren<Rigidbody2D>(true);
    }

    private void Update()
    {
        if (!followAfterReenterBlock) return;
        if (!_followArmedAfterReturn) return;
        if (chasingObject == null) return;
        if (IsBlockedByCooldown()) return;

        var target = ResolveFollowTarget();
        if (target == null) return;

        Vector3 from = chasingObject.position;
        Vector3 to = target.position;
        to.z = from.z;
        chasingObject.position = Vector3.MoveTowards(from, to, Mathf.Max(0f, followMoveSpeed) * Time.deltaTime);
    }

    private string GetRuntimeKey()
    {
        return $"{gameObject.scene.name}::{name}";
    }

    private void ApplyForcedReturnStateIfPending()
    {
        if (!forceActivateChasingObject) return;

        string key = GetRuntimeKey();
        if (!_forceStateAfterReturn.TryGetValue(key, out ForcedChasingState state)) return;

        if (chasingObject == null)
        {
            _forceStateAfterReturn.Remove(key);
            return;
        }

        if (state.hasValue)
        {
            chasingObject.position = state.position;
            chasingObject.rotation = state.rotation;
            var rb = chasingObject.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        _followArmedAfterReturn = true;

        _forceStateAfterReturn.Remove(key);

        if (state.delayActivation)
            GetDelayRunner().StartCoroutine(CoActivateAfterDelay(chasingObject.gameObject, state.activationDelaySeconds));
        else if (!chasingObject.gameObject.activeSelf)
            chasingObject.gameObject.SetActive(true);

        TryDelayEnemySnapshotTargetOnReturn();

        if (debugLog)
            Debug.Log($"[BattleCollisionTransition] force restore after return: '{chasingObject.name}' pos={chasingObject.position}");
    }

    private static DelayCoroutineRunner GetDelayRunner()
    {
        if (_delayRunner != null) return _delayRunner;

        var go = new GameObject("BattleCollisionTransition.DelayRunner");
        DontDestroyOnLoad(go);
        _delayRunner = go.AddComponent<DelayCoroutineRunner>();
        return _delayRunner;
    }

    private static void EnsureReturnRestoreHook()
    {
        if (_returnRestoreHooked) return;

        SceneManager.sceneLoaded += OnSceneLoadedApplyPendingReturnState;
        _returnRestoreHooked = true;
    }

    private static void ReleaseReturnRestoreHook()
    {
        if (!_returnRestoreHooked) return;

        SceneManager.sceneLoaded -= OnSceneLoadedApplyPendingReturnState;
        _returnRestoreHooked = false;
    }

    private static void OnSceneLoadedApplyPendingReturnState(Scene scene, LoadSceneMode mode)
    {
        if (_forceStateAfterReturn.Count == 0)
        {
            ReleaseReturnRestoreHook();
            return;
        }

        var transitions = Object.FindObjectsOfType<BattleCollisionTransition>(true);
        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i] == null) continue;
            transitions[i].ApplyForcedReturnStateIfPending();
        }

        if (_forceStateAfterReturn.Count == 0)
            ReleaseReturnRestoreHook();
    }

    private void TryDelayEnemySnapshotTargetOnReturn()
    {
        if (!delayEnemySnapshotTargetOnReturn) return;
        if (enemySnapshotTarget == null) return;
        if (ShouldDelayChasingObjectOnReturn())
            return;

        var enemyObj = enemySnapshotTarget.gameObject;
        if (enemyObj == gameObject) return;
        if (!enemyObj.activeSelf) return;

        float wait = Mathf.Max(0f, enemySnapshotReactivateSeconds);
        if (wait <= 0f) return;

        GetDelayRunner().StartCoroutine(CoDelayEnemySnapshotTarget(enemyObj, wait));
    }

    private bool ShouldDelayChasingObjectOnReturn()
    {
        if (!delayEnemySnapshotTargetOnReturn) return false;
        if (chasingObject == null || enemySnapshotTarget == null) return false;
        if (enemySnapshotReactivateSeconds <= 0f) return false;

        return enemySnapshotTarget == chasingObject ||
               enemySnapshotTarget.IsChildOf(chasingObject) ||
               chasingObject.IsChildOf(enemySnapshotTarget);
    }

    private static IEnumerator CoActivateAfterDelay(GameObject enemyObj, float wait)
    {
        if (enemyObj == null) yield break;

        enemyObj.SetActive(false);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, wait));

        if (enemyObj != null)
        {
            enemyObj.SetActive(true);
            Physics2D.SyncTransforms();
        }
    }

    private IEnumerator CoDelayEnemySnapshotTarget(GameObject enemyObj, float wait)
    {
        enemyObj.SetActive(false);

        if (debugLog)
            Debug.Log($"[BattleCollisionTransition] disable enemySnapshotTarget for {wait:F2}s: '{enemyObj.name}'");

        yield return new WaitForSecondsRealtime(wait);

        if (enemyObj != null)
            enemyObj.SetActive(true);

        if (debugLog && enemyObj != null)
            Debug.Log($"[BattleCollisionTransition] re-enable enemySnapshotTarget: '{enemyObj.name}'");
    }

    private Transform ResolveFollowTarget()
    {
        if (followTargetObject != null) return followTargetObject;
        if (playerTransform != null) return playerTransform;
        if (allowTagFallback)
        {
            var player = GameObject.FindWithTag(playerTag);
            return player != null ? player.transform : null;
        }

        var pm = FindObjectOfType<PlayerMainManager>(true);
        return pm != null ? pm.transform : null;
    }

    private void SaveForcedChasingStateForReturn()
    {
        EnsureReturnRestoreHook();

        string key = GetRuntimeKey();

        if (chasingObject == null)
        {
            _forceStateAfterReturn[key] = default;
            return;
        }

        _forceStateAfterReturn[key] = new ForcedChasingState
        {
            hasValue = true,
            position = chasingObject.position,
            rotation = chasingObject.rotation,
            delayActivation = ShouldDelayChasingObjectOnReturn(),
            activationDelaySeconds = Mathf.Max(0f, enemySnapshotReactivateSeconds)
        };
    }

    private bool IsBlockedByCooldown()
    {
        bool sameSceneReturnContext = PlayerReturnContext.HasReturnPosition &&
                                      PlayerReturnContext.ReturnSceneName == SceneManager.GetActiveScene().name;
        if (sameSceneReturnContext &&
            (PlayerReturnContext.IsInGracePeriod || PlayerReturnContext.GraceSecondsPending > 0f))
            return true;

        string key = GetRuntimeKey();
        if (!_reenterBlockedUntil.TryGetValue(key, out float until)) return false;
        return Time.unscaledTime < until;
    }

    private void RegisterCooldown()
    {
        string key = GetRuntimeKey();
        _reenterBlockedUntil[key] = Time.unscaledTime + Mathf.Max(0f, reenterBlockSeconds);
    }

    private bool IsPlayerTransform(Transform other)
    {
        if (other == null) return false;

        if (playerTransform != null)
            return other == playerTransform || other.IsChildOf(playerTransform);

        if (allowTagFallback)
            return other.CompareTag(playerTag);

        return false;
    }

    private void HandleCollision2D(Collision2D collision)
    {
        if (_entered) return;
        if (collision == null || collision.collider == null) return;
        var other = collision.collider;
        if (!IsPlayerTransform(other.transform)) return;
        if (IsBlockedByCooldown()) return;
        BeginBattleTransition(other.transform, other.name);
    }
}
