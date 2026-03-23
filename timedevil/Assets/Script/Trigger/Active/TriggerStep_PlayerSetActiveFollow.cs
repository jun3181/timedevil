using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_PlayerSetActiveFollow : TriggerStepBase
{
    [Header("Monsters To Activate")]
    [SerializeField] private List<GameObject> targetObjects = new();

    [Header("Follow")]
    [SerializeField] private bool autoFindPlayerIfMissing = true;

    [Header("Despawn")]
    [Tooltip("몬스터가 닿으면 사라질 지점(BoxCollider2D 등)")]
    [SerializeField] private List<Collider2D> despawnZones = new();
    [SerializeField] private bool deactivateOnDespawn = true;

    [Header("Timing")]
    [SerializeField] private bool waitOneFrameAfterActivate = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        var player = ResolvePlayer(ctx);
        if (player == null)
        {
            Debug.LogWarning("[TriggerStep_PlayerSetActiveFollow] Player Transform을 찾지 못했습니다.");
            yield break;
        }

        if (targetObjects == null || targetObjects.Count == 0)
        {
            if (debugLog)
                Debug.LogWarning("[TriggerStep_PlayerSetActiveFollow] 활성화할 targetObjects가 비어 있습니다.");
            yield break;
        }

        for (int i = 0; i < targetObjects.Count; i++)
        {
            var target = targetObjects[i];
            if (target == null)
                continue;

            target.SetActive(true);
            SetupFollow(target, player);
            SetupDespawn(target);

            if (debugLog)
                Debug.Log($"[TriggerStep_PlayerSetActiveFollow] Activate+Follow -> {target.name}");
        }

        if (waitOneFrameAfterActivate)
            yield return null;
    }

    private Transform ResolvePlayer(TriggerContext ctx)
    {
        if (ctx != null && ctx.player != null)
            return ctx.player;

        if (!autoFindPlayerIfMissing)
            return null;

        var playerMove = Object.FindObjectOfType<PlayerMove>(true);
        if (playerMove != null)
            return playerMove.transform;

        var playerAction = Object.FindObjectOfType<PlayerAction>(true);
        return playerAction != null ? playerAction.transform : null;
    }

    private void SetupFollow(GameObject target, Transform player)
    {
        var mover = target.GetComponent<UndeadMover>();
        if (mover == null)
        {
            if (debugLog)
                Debug.LogWarning($"[TriggerStep_PlayerSetActiveFollow] {target.name} 에 UndeadMover가 없습니다.");
            return;
        }

        mover.SetPlayer(player);
        mover.StartPatrol();
    }

    private void SetupDespawn(GameObject target)
    {
        if (despawnZones == null || despawnZones.Count == 0)
            return;

        var despawn = target.GetComponent<MonsterDespawnOnZone>();
        if (despawn == null)
            despawn = target.AddComponent<MonsterDespawnOnZone>();

        despawn.Configure(despawnZones, deactivateOnDespawn, debugLog);
    }
}
