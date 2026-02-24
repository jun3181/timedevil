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

    // -------------------------
    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Input Lock")]
    [SerializeField] private bool lockPlayerInput = true;

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
                    duration = duration
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

        // 4) Stop current move input (중요: 입력이 남아 있으면 FixedUpdate에서 계속 밀 수 있음)
        if (pm != null)
            pm.SetMoveInput(0, 0, false, false, false, false);

        // 5) Rigidbody velocity zero (optional)
        var rb = playerTr.GetComponent<Rigidbody2D>();
        if (rb && zeroVelocityBefore) rb.velocity = Vector2.zero;

        // 6) 구간 순서 실행
        for (int i = 0; i < runList.Count; i++)
        {
            var seg = runList[i];

            Vector2 dir = ResolveDir(seg.direction, seg.customDirection);
            if (dir.sqrMagnitude <= 0.000001f || seg.distance <= 0f)
            {
                if (debugLog) Debug.Log($"[TriggerStep_PlayerMove] seg[{i}] skipped (dir/distance is zero)");
                continue;
            }

            dir.Normalize();
            Vector3 delta = (Vector3)(dir * seg.distance);

            Vector3 from = playerTr.position;
            Vector3 to = from + delta;

            if (debugLog)
                Debug.Log($"[TriggerStep_PlayerMove] seg[{i}] from={from} to={to} dur={seg.duration:0.###} (dir={dir}, dist={seg.distance:0.###})");

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

                    // 계속 입력 0 유지 (안전)
                    if (pm != null)
                        pm.SetMoveInput(0, 0, false, false, false, false);

                    yield return null;
                }

                playerTr.position = to;
            }

            if (rb && zeroVelocityAfter) rb.velocity = Vector2.zero;

            // 다음 구간 전에 1프레임 정리(원치 않으면 빼도 됨)
            yield return null;
        }

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
}
