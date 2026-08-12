using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-19000)]
[DisallowMultipleComponent]
public class SceneEntrySpawnPoint : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private string spawnKey = "";

    [Header("Options")]
    [SerializeField] private int maxFindPlayerFrames = 60;
    [SerializeField] private bool keepPlayerZ = true;
    [SerializeField] private bool forceClearActionLocksOnApply = true;
    [SerializeField] private bool syncPhysicsAfterApply = true;
    [SerializeField] private bool notifyCameraWarp = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private IEnumerator Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!SceneEntrySpawnContext.Matches(sceneName, spawnKey))
            yield break;

        Transform player = null;
        int waitFrames = Mathf.Max(1, maxFindPlayerFrames);
        for (int i = 0; i < waitFrames; i++)
        {
            player = ResolvePlayerTransform();
            if (player != null) break;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning($"[SceneEntrySpawnPoint] Player not found for key='{spawnKey}'.", this);
            SceneEntrySpawnContext.TryConsume(sceneName, spawnKey);
            yield break;
        }

        Vector3 from = player.position;
        Vector3 to = transform.position;
        if (keepPlayerZ)
            to.z = player.position.z;

        player.position = to;

        if (syncPhysicsAfterApply)
            Physics2D.SyncTransforms();

        if (forceClearActionLocksOnApply && GameManager.Instance != null)
            GameManager.Instance.ForceClearActionLocks();

        if (notifyCameraWarp && CameraManager.Instance != null)
        {
            Vector3 delta = to - from;
            delta.z = 0f;
            CameraManager.Instance.NotifyTargetWarp(player, delta);
        }

        SceneEntrySpawnContext.TryConsume(sceneName, spawnKey);

        if (debugLog)
            Debug.Log($"[SceneEntrySpawnPoint] Applied key='{spawnKey}' pos={to}", this);
    }

    private Transform ResolvePlayerTransform()
    {
        var pmm = FindObjectOfType<PlayerMainManager>(true);
        if (pmm) return pmm.transform;

        var pm = FindObjectOfType<PlayerMove>(true);
        if (pm) return pm.transform;

        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }
}
