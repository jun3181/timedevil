// Assets/Script/tutorial/TutorialEntryApplier.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TutorialEntryContext
{
    private static bool _hasPending;
    private static string _targetSceneName;
    private static Vector2 _targetPosition;

    public static void SetNext(string targetSceneName, Vector2 targetPosition)
    {
        _targetSceneName = targetSceneName;
        _targetPosition = targetPosition;
        _hasPending = !string.IsNullOrWhiteSpace(_targetSceneName);
    }

    public static bool TryConsume(string activeSceneName, out Vector2 targetPosition)
    {
        targetPosition = default;

        if (!_hasPending) return false;
        if (string.IsNullOrWhiteSpace(activeSceneName)) return false;
        if (!string.Equals(_targetSceneName, activeSceneName, System.StringComparison.Ordinal)) return false;

        targetPosition = _targetPosition;
        Clear();
        return true;
    }

    public static void Clear()
    {
        _hasPending = false;
        _targetSceneName = null;
        _targetPosition = Vector2.zero;
    }
}

[DefaultExecutionOrder(-20000)]
[DisallowMultipleComponent]
public class TutorialEntryApplier : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool keepPlayerZ = true;
    [SerializeField] private int maxFindPlayerFrames = 60;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private IEnumerator Start()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        if (!TutorialEntryContext.TryConsume(activeScene, out Vector2 targetPos2))
        {
            if (debugLog)
                Debug.Log("[TutorialEntryApplier] No pending tutorial entry context.");
            yield break;
        }

        Transform player = null;
        for (int i = 0; i < maxFindPlayerFrames; i++)
        {
            player = ResolvePlayerTransform();
            if (player != null) break;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning("[TutorialEntryApplier] Player not found.");
            yield break;
        }

        Vector3 targetPos3 = new Vector3(targetPos2.x, targetPos2.y, player.position.z);
        if (!keepPlayerZ) targetPos3.z = 0f;

        player.position = targetPos3;

        if (debugLog)
            Debug.Log($"[TutorialEntryApplier] Applied tutorial entry pos=({targetPos3.x:F2},{targetPos3.y:F2},{targetPos3.z:F2}) in scene='{activeScene}'");
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
