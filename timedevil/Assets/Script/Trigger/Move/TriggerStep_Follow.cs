using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_Follow : TriggerStepBase
{
    [Header("Targets")]
    [Tooltip("실제로 이동시킬 대상. 비우면 이 컴포넌트가 붙은 오브젝트를 사용")]
    [SerializeField] private Transform movingTarget;

    [Tooltip("따라갈 대상")]
    [SerializeField] private Transform followTarget;

    [Header("Move")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 3f;

    [Min(0f)]
    [Tooltip("followTarget에 이 거리 이내로 들어오면 종료")]
    [SerializeField] private float stopDistanceToFollow = 0.1f;

    [SerializeField] private bool useUnscaledTime = false;

    [Header("Stop Point")]
    [Tooltip("지정 시 해당 지점에 도달하면 추적을 종료")]
    [SerializeField] private Transform stopPoint;

    [Min(0f)]
    [SerializeField] private float stopDistanceToPoint = 0.1f;

    [Header("Collision")]
    [Tooltip("이동 대상의 Collider2D. 비우면 movingTarget에서 자동 탐색")]
    [SerializeField] private Collider2D movingCollider;

    [Tooltip("이 레이어와 충돌 감지 시 onCollisionStep 실행 후 종료")]
    [SerializeField] private LayerMask collisionMask = ~0;

    [Min(0f)]
    [SerializeField] private float castSkin = 0.01f;

    [Tooltip("부딪혔을 때 실행할 TriggerStep (옵션)")]
    [SerializeField] private TriggerStepBase onCollisionStep;

    [Header("Safety")]
    [Tooltip("0이면 무제한. 지정 시 해당 시간(초) 이후 강제 종료")]
    [Min(0f)]
    [SerializeField] private float maxFollowSeconds = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private readonly RaycastHit2D[] _castHits = new RaycastHit2D[8];

    public override IEnumerator Execute(TriggerContext ctx)
    {
        Transform mover = movingTarget ? movingTarget : transform;

        if (!followTarget)
        {
            Debug.LogWarning("[TriggerStep_Follow] followTarget이 비어 있습니다.");
            yield break;
        }

        if (!movingCollider && mover)
            movingCollider = mover.GetComponent<Collider2D>();

        float elapsed = 0f;

        while (true)
        {
            if (!mover || !followTarget)
                yield break;

            Vector3 current = mover.position;
            Vector3 targetPos = followTarget.position;
            targetPos.z = current.z;

            // 1) 도착 조건: follow target
            if ((targetPos - current).sqrMagnitude <= stopDistanceToFollow * stopDistanceToFollow)
            {
                if (debugLog) Debug.Log("[TriggerStep_Follow] stopped: reached follow target distance.");
                yield break;
            }

            // 2) 도착 조건: stop point
            if (stopPoint)
            {
                Vector3 stopPos = stopPoint.position;
                stopPos.z = current.z;

                if ((stopPos - current).sqrMagnitude <= stopDistanceToPoint * stopDistanceToPoint)
                {
                    if (debugLog) Debug.Log("[TriggerStep_Follow] stopped: reached stop point.");
                    yield break;
                }
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f)
            {
                yield return null;
                continue;
            }

            Vector3 toTarget = targetPos - current;
            Vector3 dir3 = toTarget.normalized;
            Vector2 dir2 = new Vector2(dir3.x, dir3.y);

            float moveDist = moveSpeed * dt;
            if (moveDist <= 0f)
            {
                yield return null;
                continue;
            }

            // 3) 충돌 체크
            if (movingCollider)
            {
                ContactFilter2D filter = new ContactFilter2D
                {
                    useLayerMask = true,
                    layerMask = collisionMask,
                    useTriggers = true
                };

                int hitCount = movingCollider.Cast(dir2, filter, _castHits, moveDist + castSkin);
                if (hitCount > 0)
                {
                    var hitCollider = _castHits[0].collider;
                    if (debugLog) Debug.Log($"[TriggerStep_Follow] collision with '{hitCollider.name}'");

                    bool battleTriggered = false;
                    var battleTransition = mover != null ? mover.GetComponent<BattleCollisionTransition>() : null;
                    if (battleTransition != null)
                    {
                        battleTriggered = battleTransition.TryEnterFromExternal(hitCollider);
                        if (debugLog && battleTriggered)
                            Debug.Log("[TriggerStep_Follow] forwarded collision to BattleCollisionTransition.");
                    }

                    if (onCollisionStep)
                    {
                        IEnumerator it = null;
                        try
                        {
                            it = onCollisionStep.Execute(ctx);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[TriggerStep_Follow] onCollisionStep Execute() throw: {e}");
                        }

                        if (it != null)
                            yield return it;
                    }

                    yield break;
                }
            }

            mover.position = Vector3.MoveTowards(current, targetPos, moveDist);

            if (maxFollowSeconds > 0f)
            {
                elapsed += dt;
                if (elapsed >= maxFollowSeconds)
                {
                    if (debugLog) Debug.Log("[TriggerStep_Follow] stopped: maxFollowSeconds.");
                    yield break;
                }
            }

            yield return null;
        }
    }
}
