using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MonsterMoverAction
{
    StartChase,
    StopChase,
    SetTargetOnly
}

[DisallowMultipleComponent]
public class TriggerStep_MonsterMover : TriggerStepBase
{
    [Header("Action")]
    [SerializeField] private MonsterMoverAction action = MonsterMoverAction.StartChase;

    [Header("Targets")]
    [Tooltip("비어 있으면 실행 시점에 이 스텝 GameObject에서 MonsterMover를 찾습니다.")]
    [SerializeField] private List<MonsterMover> movers = new();

    [Header("Target Resolve")]
    [SerializeField] private bool useContextPlayerAsTarget = true;
    [SerializeField] private Transform targetOverride;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        var runMovers = ResolveMovers();
        if (runMovers.Count == 0)
        {
            if (debugLog) Debug.LogWarning("[TriggerStep_MonsterMover] MonsterMover를 찾지 못했습니다.");
            yield break;
        }

        Transform target = ResolveTarget(ctx);

        for (int i = 0; i < runMovers.Count; i++)
        {
            var mover = runMovers[i];
            if (!mover) continue;

            switch (action)
            {
                case MonsterMoverAction.StartChase:
                    mover.StartChase(target);
                    break;

                case MonsterMoverAction.StopChase:
                    mover.StopChase();
                    break;

                case MonsterMoverAction.SetTargetOnly:
                    mover.SetTarget(target);
                    break;
            }

            if (debugLog)
            {
                Debug.Log($"[TriggerStep_MonsterMover] action={action} mover='{mover.name}' target='{(target ? target.name : "null")}'");
            }
        }

        yield break;
    }

    private List<MonsterMover> ResolveMovers()
    {
        if (movers != null && movers.Count > 0)
            return movers;

        var single = GetComponent<MonsterMover>();
        if (single != null)
            return new List<MonsterMover> { single };

        return new List<MonsterMover>();
    }

    private Transform ResolveTarget(TriggerContext ctx)
    {
        if (targetOverride != null)
            return targetOverride;

        if (useContextPlayerAsTarget && ctx != null)
        {
            if (ctx.player != null) return ctx.player;
            if (ctx.instigator != null) return ctx.instigator.transform;
        }

        return null;
    }
}
