using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BattleCollisionTransition : MonoBehaviour
{
    [Header("Battle")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private string enemyId = "Enemy1";
    [SerializeField] private Transform enemySnapshotTarget;

    [Header("Return")]
    [SerializeField] private Transform returnPointOverride;
    [SerializeField] private float graceSeconds = 0.5f;

    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool disableAfterEnter = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool _entered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_entered) return;
        if (!other || !other.CompareTag(playerTag)) return;

        _entered = true;
        var player = other.transform;
        var enemy = enemySnapshotTarget != null ? enemySnapshotTarget : transform;
        var returnPos = returnPointOverride != null ? returnPointOverride.position : player.position;

        PlayerReturnContext.SetReturnFromTrigger(
            returnSceneName: SceneManager.GetActiveScene().name,
            returnPosition: returnPos,
            graceSeconds: graceSeconds,
            requestCameraRebind: false,
            targetVcamName: null,
            useOverlapSuppression: false,
            overlapRadius: 0f,
            overlapSeconds: 0f
        );

        if (debugLog)
            Debug.Log($"[BattleCollisionTransition] enter by '{other.name}' -> scene='{battleSceneName}', enemyId='{enemyId}', returnPos=({returnPos.x:F2},{returnPos.y:F2})");

        BattleSceneLoader.Go(battleSceneName, enemyId, player, enemy);

        if (disableAfterEnter)
            gameObject.SetActive(false);
    }
}
