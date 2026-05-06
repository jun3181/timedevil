using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public class BattleCollisionTransition : MonoBehaviour
{
    [Header("Battle")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private string enemyId = "Enemy1";
    [SerializeField] private Transform enemySnapshotTarget;
    [SerializeField] private Transform chasingObject;
    [SerializeField] private bool forceActivateChasingObject = true;

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

    private struct ForcedChasingState
    {
        public bool hasValue;
        public Vector3 position;
        public Quaternion rotation;
    }

    private void Start()
    {
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
        if (_entered) return;
        if (collision == null || collision.collider == null) return;
        var other = collision.collider;
        if (!IsPlayerTransform(other.transform)) return;
        if (IsBlockedByCooldown()) return;
        BeginBattleTransition(other.transform, other.name);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (_entered) return;
        if (collision == null || collision.collider == null) return;
        var other = collision.collider;
        if (!IsPlayerTransform(other.transform)) return;
        if (IsBlockedByCooldown()) return;
        BeginBattleTransition(other.transform, other.name);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (_entered) return;
        if (collision == null || collision.collider == null) return;
        var other = collision.collider;
        if (!other.CompareTag(playerTag)) return;
        if (IsBlockedByCooldown()) return;
        BeginBattleTransition(other.transform, other.name);
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

    private void BeginBattleTransition(Transform player, string colliderName)
    {
        _entered = true;
        StartCoroutine(CoEnterBattle(player, colliderName));
    }

    private IEnumerator CoEnterBattle(Transform player, string colliderName)
    {
        var pauseState = ApplyPauseOnEnter();
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

            // 씬 순차 진행 시 CameraManager가 이전 씬 모드를 들고 있는 경우가 있어
            // 현재 씬의 SceneCameraBootstrap 설정이 있으면 그것을 우선 복귀 기준으로 사용한다.
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
            Debug.Log($"[BattleCollisionTransition] enter by '{colliderName}' -> scene='{battleSceneName}', enemyId='{enemyId}', returnPos=({returnPos.x:F2},{returnPos.y:F2}), camRestore={restoreCam}, camMode={camMode}");

        BattleSceneLoader.Go(battleSceneName, enemyId, player, enemy);

        if (disableAfterEnter)
            gameObject.SetActive(false);
    }

    private struct PauseState
    {
        public bool hasTarget;
        public Transform target;
        public Vector3 lockedPosition;
        public Rigidbody2D rb;
    }

    private PauseState ApplyPauseOnEnter()
    {
        Transform target = pauseTargetObject != null ? pauseTargetObject : null;
        Rigidbody2D rb = pauseTargetRigidbody2D != null
            ? pauseTargetRigidbody2D
            : (target != null ? target.GetComponent<Rigidbody2D>() : null);

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (pauseTargetController != null)
            pauseTargetController.enabled = false;

        var state = new PauseState
        {
            hasTarget = target != null,
            target = target,
            lockedPosition = target != null ? target.position : Vector3.zero,
            rb = rb
        };

        return state;
    }

    private void MaintainPauseState(PauseState state)
    {
        if (state.rb != null)
            state.rb.velocity = Vector2.zero;

        if (state.hasTarget && state.target != null)
            state.target.position = state.lockedPosition;
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
        if (chasingObject == null) return;

        string key = GetRuntimeKey();
        if (!_forceStateAfterReturn.TryGetValue(key, out ForcedChasingState state)) return;

        if (!chasingObject.gameObject.activeSelf)
            chasingObject.gameObject.SetActive(true);

        if (state.hasValue)
        {
            chasingObject.position = state.position;
            chasingObject.rotation = state.rotation;
            var rb = chasingObject.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        _followArmedAfterReturn = true;

        _forceStateAfterReturn.Remove(key);

        if (debugLog)
            Debug.Log($"[BattleCollisionTransition] force restore after return: '{chasingObject.name}' pos={chasingObject.position}");
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
            rotation = chasingObject.rotation
        };
    }

    private bool IsBlockedByCooldown()
    {
        if (PlayerReturnContext.IsInGracePeriod || PlayerReturnContext.GraceSecondsPending > 0f) return true;

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
}
