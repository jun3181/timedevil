// Assets/Script/Scene/MyroomEntryApplier.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-20000)]
[DisallowMultipleComponent]
public class MyroomEntryApplier : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform room1Spawn;
    [SerializeField] private Transform room2Spawn;

    [Header("Fallback (when no explicit context)")]
    [Tooltip("If enabled, fallbackPoint is applied when MyroomEntryContext is empty. If disabled, no forced reposition occurs.")]
    [SerializeField] private bool applyFallbackWhenNoContext = false;
    [SerializeField] private MyroomEntryPoint fallbackPoint = MyroomEntryPoint.Room2;

    [Header("Options")]
    [SerializeField] private bool forceClearActionLocksOnStart = true;
    [SerializeField] private int maxFindPlayerFrames = 60;
    [SerializeField] private bool keepPlayerZ = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private static int s_appliedSceneHandle = -1;
    private MyroomEntryPoint _entry = MyroomEntryPoint.None;

    private void Awake()
    {
        int handle = SceneManager.GetActiveScene().handle;
        if (s_appliedSceneHandle == handle) return;
        s_appliedSceneHandle = handle;

        MyroomEntryPoint consumed;
        bool hasExplicitEntry = MyroomEntryContext.TryConsume(out consumed);
        _entry = hasExplicitEntry ? consumed : MyroomEntryPoint.None;

        if (!hasExplicitEntry && applyFallbackWhenNoContext)
            _entry = fallbackPoint;
    }

    private IEnumerator Start()
    {
        if (forceClearActionLocksOnStart && GameManager.Instance != null)
            GameManager.Instance.ForceClearActionLocks();

        Transform player = null;
        for (int i = 0; i < maxFindPlayerFrames; i++)
        {
            player = ResolvePlayerTransform();
            if (player != null) break;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning("[MyroomEntryApplier] Player not found.");
            yield break;
        }

        if (_entry == MyroomEntryPoint.None)
        {
            if (debugLog)
                Debug.Log("[MyroomEntryApplier] No entry context -> skip forced spawn override.");
            yield break;
        }

        Transform target = ResolveTarget(_entry);
        if (target == null)
        {
            Debug.LogWarning("[MyroomEntryApplier] SpawnPoint missing. (Room1/Room2)");
            yield break;
        }

        Vector3 pos = target.position;
        if (keepPlayerZ) pos.z = player.position.z;

        player.position = pos;

        if (debugLog)
            Debug.Log("[MyroomEntryApplier] entry=" + _entry + " -> '" + target.name + "' pos=" + pos);
    }

    private Transform ResolveTarget(MyroomEntryPoint entry)
    {
        switch (entry)
        {
            case MyroomEntryPoint.Room1: return room1Spawn;
            case MyroomEntryPoint.Room2: return room2Spawn;
            default: return null;
        }
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
