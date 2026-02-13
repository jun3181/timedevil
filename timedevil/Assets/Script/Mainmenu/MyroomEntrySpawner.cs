// Assets/Script/Scene/MyroomEntrySpawner.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MyroomEntrySpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform room1Spawn; // 새 게임 스폰
    [SerializeField] private Transform room2Spawn; // 이어하기 스폰

    [Header("Options")]
    [SerializeField] private bool forceClearActionLocksOnStart = true;
    [SerializeField] private int maxFindPlayerFrames = 60;
    [SerializeField] private bool keepPlayerZ = true;

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
            Debug.LogWarning("[MyroomEntrySpawner] Player를 찾지 못했습니다.");
            yield break;
        }

        Transform target = (GameStartContext.Mode == GameStartMode.NewGame) ? room1Spawn : room2Spawn;
        if (target == null)
        {
            Debug.LogWarning("[MyroomEntrySpawner] SpawnPoint가 비어있습니다. (Room1/Room2)");
            yield break;
        }

        Vector3 pos = target.position;
        if (keepPlayerZ) pos.z = player.position.z;

        player.position = pos;

        Debug.Log($"[MyroomEntrySpawner] Mode={GameStartContext.Mode} -> Spawn='{target.name}' pos={pos}");
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
