using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool disableAfterEnter = true;
    [SerializeField] private float reenterBlockSeconds = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool _entered;
    private static readonly System.Collections.Generic.Dictionary<string, float> _reenterBlockedUntil = new();
    private static readonly System.Collections.Generic.HashSet<string> _forceActivateAfterReturnKeys = new();

    private void Start()
    {
        ApplyForcedReturnStateIfPending();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_entered) return;
        if (!other || !other.CompareTag(playerTag)) return;
        if (IsBlockedByCooldown()) return;
        EnterBattle(other.transform, other.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_entered) return;
        if (!other || !other.CompareTag(playerTag)) return;
        if (IsBlockedByCooldown()) return;
        EnterBattle(other.transform, other.name);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_entered) return;
        if (collision == null || collision.collider == null) return;
        var other = collision.collider;
        if (!other.CompareTag(playerTag)) return;
        if (IsBlockedByCooldown()) return;
        EnterBattle(other.transform, other.name);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_entered) return;
        if (collision == null || collision.collider == null) return;
        var other = collision.collider;
        if (!other.CompareTag(playerTag)) return;
        if (IsBlockedByCooldown()) return;
        EnterBattle(other.transform, other.name);
    }

    private void EnterBattle(Transform player, string colliderName)
    {
        _entered = true;
        if (forceActivateChasingObject)
            _forceActivateAfterReturnKeys.Add(GetRuntimeKey());

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

    private string GetRuntimeKey()
    {
        return $"{gameObject.scene.name}::{name}";
    }

    private void ApplyForcedReturnStateIfPending()
    {
        if (!forceActivateChasingObject) return;
        if (chasingObject == null) return;

        string key = GetRuntimeKey();
        if (!_forceActivateAfterReturnKeys.Contains(key)) return;

        if (!chasingObject.gameObject.activeSelf)
            chasingObject.gameObject.SetActive(true);

        _forceActivateAfterReturnKeys.Remove(key);

        if (debugLog)
            Debug.Log($"[BattleCollisionTransition] force-activated chasingObject after return: '{chasingObject.name}'");
    }

    private bool IsBlockedByCooldown()
    {
        string key = GetRuntimeKey();
        if (!_reenterBlockedUntil.TryGetValue(key, out float until)) return false;
        return Time.unscaledTime < until;
    }

    private void RegisterCooldown()
    {
        string key = GetRuntimeKey();
        _reenterBlockedUntil[key] = Time.unscaledTime + Mathf.Max(0f, reenterBlockSeconds);
    }
}
