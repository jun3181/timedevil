// Assets/Script/Scene/MyroomEntrySpawner.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MyroomEntrySpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform room1Spawn; // 새 게임 스폰
    [SerializeField] private Transform room2Spawn; // 이어하기/복귀 스폰

    [Header("Options")]
    [SerializeField] private bool forceClearActionLocksOnStart = true;
    [SerializeField] private int maxFindPlayerFrames = 60;
    [SerializeField] private bool keepPlayerZ = true;

    [Tooltip("다른 스크립트(Start/OnEnable)가 플레이어 위치를 덮어쓰는 경우를 이기기 위해, 몇 프레임 뒤에 한 번 더 위치를 고정합니다.")]
    [SerializeField] private int settleFrames = 2;

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

        // 1) fallback은 기존대로 GameStartContext 기반
        MyroomEntryPoint fallback =
            (GameStartContext.Mode == GameStartMode.NewGame) ? MyroomEntryPoint.Room1 : MyroomEntryPoint.Room2;

        // 2) 원샷 지시가 있으면 그걸 최우선(없으면 fallback)
        MyroomEntryPoint entry = MyroomEntryContext.Consume(fallback);

        Transform target = (entry == MyroomEntryPoint.Room1) ? room1Spawn : room2Spawn;
        if (target == null)
        {
            Debug.LogWarning("[MyroomEntrySpawner] SpawnPoint가 비어있습니다. (Room1/Room2)");
            yield break;
        }

        // 다른 Start들이 먼저 돌게 약간 양보
        for (int i = 0; i < settleFrames; i++) yield return null;

        // 1차 워프
        WarpPlayer(player, target);

        // 덮어쓰기 방지: 몇 프레임 뒤에 다시 한 번 확정 워프
        for (int i = 0; i < settleFrames; i++) yield return null;
        WarpPlayer(player, target);

        Debug.Log($"[MyroomEntrySpawner] entry={entry} (fallback={fallback}) Mode={GameStartContext.Mode} -> Spawn='{target.name}' pos={target.position}");
    }

    private void WarpPlayer(Transform player, Transform target)
    {
        Vector3 pos = target.position;
        if (keepPlayerZ) pos.z = player.position.z;

        player.position = pos;

        // Rigidbody2D가 있으면 튕김/잔속도 방지
        var rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.velocity = Vector2.zero;
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
