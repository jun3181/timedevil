// Assets/Script/Scene/MyroomEntryApplier.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-20000)]
[DisallowMultipleComponent]
public class MyroomEntryApplier : MonoBehaviour
{
    [Header("Spawn Points")]
    [FormerlySerializedAs("room1Spawn")]
    [SerializeField] private Transform spawnRoom1NewGame;
    [FormerlySerializedAs("room2Spawn")]
    [SerializeField] private Transform spawnRoom2LoadGamePlayerDead;
    [FormerlySerializedAs("room4Spawn")]
    [SerializeField] private Transform spawnRoom4End;

    [Header("Fallback (when no explicit context)")]
    [Tooltip("If enabled, fallbackPoint is applied when MyroomEntryContext is empty. If disabled, no forced reposition occurs.")]
    [SerializeField] private bool applyFallbackWhenNoContext = false;
    [SerializeField] private MyroomEntryPoint fallbackPoint = MyroomEntryPoint.Spawn_Room2_LoadGame_PlayerDead;

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
        Scene activeScene = SceneManager.GetActiveScene();

        if (SceneArrivalContext.HasPendingForScene(activeScene.name))
        {
            _entry = MyroomEntryPoint.None;
            return;
        }

        if (PlayerReturnContext.HasReturnPosition &&
            !string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName) &&
            PlayerReturnContext.ReturnSceneName == activeScene.name)
        {
            MyroomEntryContext.Clear();
            _entry = MyroomEntryPoint.None;

            if (debugLog)
                Debug.Log("[MyroomEntryApplier] Return context active -> skip Myroom entry spawn override.", this);

            return;
        }

        MyroomEntryPoint consumed;
        bool hasExplicitEntry = MyroomEntryContext.TryConsume(out consumed);
        if (hasExplicitEntry)
        {
            _entry = consumed;
            s_appliedSceneHandle = activeScene.handle;
            return;
        }

        int handle = activeScene.handle;
        if (s_appliedSceneHandle == handle) return;
        s_appliedSceneHandle = handle;

        _entry = MyroomEntryPoint.None;

        if (applyFallbackWhenNoContext)
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
            Debug.LogWarning("[MyroomEntryApplier] SpawnPoint missing.");
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
            case MyroomEntryPoint.Spawn_Room1_NewGame: return spawnRoom1NewGame;
            case MyroomEntryPoint.Spawn_Room2_LoadGame_PlayerDead: return spawnRoom2LoadGamePlayerDead;
            case MyroomEntryPoint.Spawn_Room4_end: return spawnRoom4End;
            default: return null;
        }
    }

    public bool TryGetSpawn(MyroomEntryPoint entry, out Transform spawn)
    {
        spawn = ResolveTarget(entry);
        return spawn != null;
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
