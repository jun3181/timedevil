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

    [Header("Fallback (컨텍스트가 없을 때)")]
    [Tooltip("대부분의 경우(꿈 갔다가 돌아오기 등) Room2가 기본이 되도록 권장")]
    [SerializeField] private MyroomEntryPoint fallbackPoint = MyroomEntryPoint.Room2;

    [Header("Options")]
    [SerializeField] private bool forceClearActionLocksOnStart = true;
    [SerializeField] private int maxFindPlayerFrames = 60;
    [SerializeField] private bool keepPlayerZ = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private static int s_appliedSceneHandle = -1;
    private MyroomEntryPoint _entry;

    private void Awake()
    {
        // 같은 씬에서 중복 적용 방지
        int handle = SceneManager.GetActiveScene().handle;
        if (s_appliedSceneHandle == handle) return;
        s_appliedSceneHandle = handle;

        // 여기서 1회성 Consume
        _entry = MyroomEntryContext.Consume(fallbackPoint);
    }

    private IEnumerator Start()
    {
        if (forceClearActionLocksOnStart && GameManager.Instance != null)
            GameManager.Instance.ForceClearActionLocks();

        // 플레이어 스폰/Enable 타이밍 때문에 몇 프레임 기다리기
        Transform player = null;
        for (int i = 0; i < maxFindPlayerFrames; i++)
        {
            player = ResolvePlayerTransform();
            if (player != null) break;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning("[MyroomEntryApplier] Player를 찾지 못했습니다.");
            yield break;
        }

        Transform target = ResolveTarget(_entry);
        if (target == null)
        {
            Debug.LogWarning("[MyroomEntryApplier] SpawnPoint가 비어있습니다. (Room1/Room2)");
            yield break;
        }

        Vector3 pos = target.position;
        if (keepPlayerZ) pos.z = player.position.z;

        player.position = pos;

        if (debugLog)
            Debug.Log($"[MyroomEntryApplier] entry={_entry} -> '{target.name}' pos={pos}");
    }

    private Transform ResolveTarget(MyroomEntryPoint entry)
    {
        switch (entry)
        {
            case MyroomEntryPoint.Room1: return room1Spawn;
            case MyroomEntryPoint.Room2: return room2Spawn;
            default: return (fallbackPoint == MyroomEntryPoint.Room1) ? room1Spawn : room2Spawn;
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
