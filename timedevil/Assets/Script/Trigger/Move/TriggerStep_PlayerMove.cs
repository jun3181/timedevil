// Assets/Script/Trigger/Steps/TriggerStep_PlayerMove.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ForcedMoveDir
{
    Up,
    Down,
    Left,
    Right,
    Custom
}

public enum ForcedWalkAnimType
{
    AutoFromMove,
    DownWalk,
    UpWalk,
    LeftWalk,
    RightWalk,
    None
}

[System.Serializable]
public struct ForcedMoveSegment
{
    public ForcedMoveDir direction;

    [Tooltip("direction=Custom 일 때만 사용(0이면 무시)")]
    public Vector2 customDirection;

    [Min(0f)]
    [Tooltip("이 구간에서 이동할 거리(월드 단위)")]
    public float distance;

    [Min(0f)]
    [Tooltip("0이면 즉시 이동(teleport). 0보다 크면 해당 시간 동안 강제 이동(Lerp).")]
    public float duration;

    [Tooltip("이 구간에서 재생할 걷기 애니메이션. AutoFromMove면 direction/customDirection을 기준으로 자동 선택")]
    public ForcedWalkAnimType walkAnimation;
}

[DisallowMultipleComponent]
public class TriggerStep_PlayerMove : TriggerStepBase
{
    [Header("Sequence (비우면 Single 설정 1회 실행)")]
    [SerializeField] private List<ForcedMoveSegment> segments = new();

    // -------------------------
    // ✅ 기존(레거시) 단일 이동 설정 (segments 비어있을 때만 사용)
    // -------------------------
    [Header("Single (Legacy)")]
    [SerializeField] private ForcedMoveDir direction = ForcedMoveDir.Right;

    [Tooltip("direction=Custom 일 때만 사용(0이면 무시)")]
    [SerializeField] private Vector2 customDirection = Vector2.right;

    [Min(0f)]
    [SerializeField] private float distance = 1f;

    [Min(0f)]
    [SerializeField] private float duration = 0.25f;

    [SerializeField] private ForcedWalkAnimType walkAnimation = ForcedWalkAnimType.AutoFromMove;

    // -------------------------
    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Input Lock")]
    [SerializeField] private bool lockPlayerInput = true;

    [SerializeField] private bool disablePlayerMainManagerWhileRunning = true;

    [Header("Animation")]
    [SerializeField] private bool setIdleAtEnd = true;
    [SerializeField] private string paramIsChange = "isChange";
    [SerializeField] private string paramHAxisRaw = "hAxisRaw";
    [SerializeField] private string paramVAxisRaw = "vAxisRaw";

    [Header("Rigidbody (optional)")]
    [SerializeField] private bool zeroVelocityBefore = true;
    [SerializeField] private bool zeroVelocityAfter = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        // 1) Player resolve
        PlayerMove pm = (ctx != null) ? ctx.playerMove : null;
        Transform playerTr = (ctx != null) ? ctx.player : null;

        if (!playerTr)
        {
            pm = Object.FindObjectOfType<PlayerMove>(true);
            playerTr = pm ? pm.transform : null;
        }

        if (!playerTr)
        {
            Debug.LogWarning("[TriggerStep_PlayerMove] Player Transform을 찾지 못했습니다.");
            yield break;
        }

        Animator anim = playerTr.GetComponent<Animator>();
        bool canDriveAnim = HasAnimParams(anim);

        PlayerMainManager pmm = null;
        bool pmmPrevEnabled = false;

        // 2) 실행할 구간 목록 준비 (segments 우선, 없으면 레거시 1개)
        List<ForcedMoveSegment> runList = null;
        if (segments != null && segments.Count > 0)
        {
            runList = segments;
        }
        else
        {
            runList = new List<ForcedMoveSegment>(1)
            {
                new ForcedMoveSegment
                {
                    direction = direction,
                    customDirection = customDirection,
                    distance = distance,
                    duration = duration,
                    walkAnimation = walkAnimation
                }
            };
        }

        // 3) Lock input
        bool heldLock = false;
        if (lockPlayerInput && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            heldLock = true;
        }

        if (disablePlayerMainManagerWhileRunning)
        {
            pmm = playerTr.GetComponent<PlayerMainManager>();
            if (!pmm) pmm = Object.FindObjectOfType<PlayerMainManager>(true);

            if (pmm != null)
            {
                pmmPrevEnabled = pmm.enabled;
                pmm.enabled = false;
            }
        }

        // 4) Stop current move input (중요: 입력이 남아 있으면 FixedUpdate에서 계속 밀 수 있음)
        if (pm != null && !canDriveAnim)
            pm.SetMoveInput(0, 0, false, false, false, false);

        // 5) Rigidbody velocity zero (optional)
        var rb = playerTr.GetComponent<Rigidbody2D>();
        if (rb && zeroVelocityBefore) rb.velocity = Vector2.zero;

        ForcedWalkAnimType lastAnim = ForcedWalkAnimType.DownWalk;

        // 6) 구간 순서 실행
        for (int i = 0; i < runList.Count; i++)
        {
            var seg = runList[i];

            Vector2 dir = ResolveDir(seg.direction, seg.customDirection);
            ForcedWalkAnimType resolvedAnim = ResolveAnim(seg.walkAnimation, dir);
            lastAnim = resolvedAnim;

            if (canDriveAnim)
                ApplyWalkAnimation(anim, resolvedAnim, true);

            if (dir.sqrMagnitude <= 0.000001f || seg.distance <= 0f)
            {
                if (debugLog) Debug.Log($"[TriggerStep_PlayerMove] seg[{i}] skipped move (dir/distance is zero), anim={resolvedAnim}");

                if (seg.duration > 0f)
                {
                    float wait = 0f;
                    while (wait < seg.duration)
                    {
                        if (canDriveAnim)
                            ApplyWalkAnimation(anim, resolvedAnim, false);

                        if (pm != null && !canDriveAnim)
                            pm.SetMoveInput(0, 0, false, false, false, false);

                        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                        wait += dt;
                        yield return null;
                    }
                }

                continue;
            }

            dir.Normalize();
            Vector3 delta = (Vector3)(dir * seg.distance);

            Vector3 from = playerTr.position;
            Vector3 to = from + delta;

            if (debugLog)
                Debug.Log($"[TriggerStep_PlayerMove] seg[{i}] from={from} to={to} dur={seg.duration:0.###} (dir={dir}, dist={seg.distance:0.###}, anim={resolvedAnim})");

            // 구간 이동
            if (seg.duration <= 0f)
            {
                playerTr.position = to;
            }
            else
            {
                float t = 0f;
                while (t < seg.duration)
                {
                    float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    t += dt;

                    float u = Mathf.Clamp01(t / seg.duration);
                    float k = (ease != null) ? ease.Evaluate(u) : u;

                    playerTr.position = Vector3.LerpUnclamped(from, to, k);

                    if (canDriveAnim)
                        ApplyWalkAnimation(anim, resolvedAnim, false);

                    // 계속 입력 0 유지 (안전)
                    if (pm != null && !canDriveAnim)
                        pm.SetMoveInput(0, 0, false, false, false, false);

                    yield return null;
                }

                playerTr.position = to;
            }

            if (rb && zeroVelocityAfter) rb.velocity = Vector2.zero;

            // 다음 구간 전에 1프레임 정리(원치 않으면 빼도 됨)
            yield return null;
        }

        if (canDriveAnim && setIdleAtEnd)
            SetIdle(anim, lastAnim);

        if (pmm != null)
            pmm.enabled = pmmPrevEnabled;

        // 7) Unlock input
        if (heldLock && GameManager.Instance != null)
            GameManager.Instance.UnlockAction();
    }

    private Vector2 ResolveDir(ForcedMoveDir dir, Vector2 custom)
    {
        switch (dir)
        {
            case ForcedMoveDir.Up: return Vector2.up;
            case ForcedMoveDir.Down: return Vector2.down;
            case ForcedMoveDir.Left: return Vector2.left;
            case ForcedMoveDir.Right: return Vector2.right;
            case ForcedMoveDir.Custom: return custom;
            default: return Vector2.zero;
        }
    }

    private ForcedWalkAnimType ResolveAnim(ForcedWalkAnimType selected, Vector2 dir)
    {
        if (selected != ForcedWalkAnimType.AutoFromMove)
            return selected;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return (dir.x >= 0f) ? ForcedWalkAnimType.RightWalk : ForcedWalkAnimType.LeftWalk;

        return (dir.y >= 0f) ? ForcedWalkAnimType.UpWalk : ForcedWalkAnimType.DownWalk;
    }

    private bool HasAnimParams(Animator anim)
    {
        if (!anim) return false;

        bool hasChange = false;
        bool hasH = false;
        bool hasV = false;

        var pars = anim.parameters;
        for (int i = 0; i < pars.Length; i++)
        {
            var p = pars[i];
            if (p.name == paramIsChange && p.type == AnimatorControllerParameterType.Bool) hasChange = true;
            else if (p.name == paramHAxisRaw && p.type == AnimatorControllerParameterType.Int) hasH = true;
            else if (p.name == paramVAxisRaw && p.type == AnimatorControllerParameterType.Int) hasV = true;
        }

        if (!hasChange || !hasH || !hasV)
        {
            if (debugLog)
                Debug.LogWarning($"[TriggerStep_PlayerMove] Animator 파라미터 누락: '{paramIsChange}', '{paramHAxisRaw}', '{paramVAxisRaw}'");
            return false;
        }

        return true;
    }

    private void ApplyWalkAnimation(Animator anim, ForcedWalkAnimType animType, bool isChange)
    {
        if (!anim || animType == ForcedWalkAnimType.None)
        {
            if (anim) anim.SetBool(paramIsChange, false);
            return;
        }

        int h = 0;
        int v = 0;

        switch (animType)
        {
            case ForcedWalkAnimType.DownWalk: v = -1; break;
            case ForcedWalkAnimType.UpWalk: v = 1; break;
            case ForcedWalkAnimType.LeftWalk: h = -1; break;
            case ForcedWalkAnimType.RightWalk: h = 1; break;
        }

        anim.SetInteger(paramHAxisRaw, h);
        anim.SetInteger(paramVAxisRaw, v);
        anim.SetBool(paramIsChange, isChange);
    }

    private void SetIdle(Animator anim, ForcedWalkAnimType fromAnim)
    {
        if (!anim) return;

        switch (fromAnim)
        {
            case ForcedWalkAnimType.DownWalk:
            case ForcedWalkAnimType.UpWalk:
                anim.SetInteger(paramHAxisRaw, 0);
                anim.SetInteger(paramVAxisRaw, 0);
                break;

            case ForcedWalkAnimType.LeftWalk:
            case ForcedWalkAnimType.RightWalk:
                anim.SetInteger(paramHAxisRaw, 0);
                anim.SetInteger(paramVAxisRaw, 0);
                break;
        }

        anim.SetBool(paramIsChange, false);
    }
}
