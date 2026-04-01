// Assets/Script/Trigger/Move/TriggerStep_PlayerFollow.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_PlayerFollow : TriggerStepBase
{
    [Header("Follow Setup")]
    [Tooltip("이 Step으로 이동시킬 대상. 비우면 ctx.player(플레이어)를 사용합니다.")]
    [SerializeField] private Transform applyTarget;

    [Tooltip("따라갈 대상")]
    [SerializeField] private Transform followTarget;

    [Tooltip("이동 속도(월드 단위/초)")]
    [Min(0f)][SerializeField] private float moveSpeed = 2f;

    [Header("Stop Condition")]
    [Tooltip("이 지점에 도달하면 추적을 멈춥니다.")]
    [SerializeField] private Transform stopPoint;

    [Tooltip("정지 지점 판정 거리")]
    [Min(0f)][SerializeField] private float stopDistance = 0.05f;

    [Header("Collision TriggerStep")]
    [Tooltip("적용 대상이 followTarget과 부딪히면 실행할 Step")]
    [SerializeField] private TriggerStepBase onHitTriggerStep;

    [Tooltip("Collider가 없을 때 사용할 거리 기반 충돌 판정")]
    [Min(0f)][SerializeField] private float hitDistanceFallback = 0.05f;

    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = false;

    [Tooltip("안전장치: 0 이하이면 무제한")]
    [Min(0f)][SerializeField] private float maxFollowSeconds = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        Transform mover = ResolveApplyTarget(ctx);
        Transform target = followTarget;

        if (!mover)
        {
            Debug.LogWarning("[TriggerStep_PlayerFollow] applyTarget(또는 ctx.player)을 찾지 못했습니다.");
            yield break;
        }

        if (!target)
        {
            Debug.LogWarning("[TriggerStep_PlayerFollow] followTarget이 비어 있습니다.");
            yield break;
        }

        var moverCol = mover.GetComponent<Collider2D>();
        var targetCol = target.GetComponent<Collider2D>();

        float elapsed = 0f;

        while (true)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f)
            {
                yield return null;
                continue;
            }

            // 1) 먼저 정지 지점 도달 여부 확인
            if (HasReachedStopPoint(mover.position))
            {
                if (debugLog) Debug.Log("[TriggerStep_PlayerFollow] Stop point reached. Follow stopped.");
                yield break;
            }

            // 2) 대상 추적 이동
            Vector3 next = Vector3.MoveTowards(mover.position, target.position, moveSpeed * dt);
            mover.position = next;

            // 3) 충돌 시 지정 Step 실행
            if (HasHitTarget(mover, target, moverCol, targetCol))
            {
                if (debugLog) Debug.Log("[TriggerStep_PlayerFollow] Hit detected. Execute onHitTriggerStep.");

                if (onHitTriggerStep != null)
                    yield return onHitTriggerStep.Execute(ctx);

                yield break;
            }

            // 4) 타임아웃(옵션)
            if (maxFollowSeconds > 0f)
            {
                elapsed += dt;
                if (elapsed >= maxFollowSeconds)
                {
                    if (debugLog) Debug.Log("[TriggerStep_PlayerFollow] Timeout reached. Follow stopped.");
                    yield break;
                }
            }

            yield return null;
        }
    }

    private Transform ResolveApplyTarget(TriggerContext ctx)
    {
        if (applyTarget != null)
            return applyTarget;

        if (ctx != null && ctx.player != null)
            return ctx.player;

        return null;
    }

    private bool HasReachedStopPoint(Vector3 moverPos)
    {
        if (!stopPoint)
            return false;

        return (moverPos - stopPoint.position).sqrMagnitude <= stopDistance * stopDistance;
    }

    private bool HasHitTarget(Transform mover, Transform target, Collider2D moverCol, Collider2D targetCol)
    {
        if (moverCol != null && targetCol != null)
            return moverCol.bounds.Intersects(targetCol.bounds);

        return (mover.position - target.position).sqrMagnitude <= hitDistanceFallback * hitDistanceFallback;
    }
}
