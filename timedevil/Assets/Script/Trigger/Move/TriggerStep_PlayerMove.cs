// Assets/Script/Trigger/Steps/TriggerStep_PlayerMove.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TriggerMoveDir
{
    Up,
    Down,
    Left,
    Right,
    Custom
}

[System.Serializable]
public struct TriggerMoveSegment
{
    public TriggerMoveDir dir;

    [Tooltip("dir=Custom일 때만 사용")]
    public Vector2 customDirection;

    [Min(0f)]
    public float seconds;
}

[DisallowMultipleComponent]
public class TriggerStep_PlayerMove : TriggerStepBase
{
    [Header("Sequence (순서대로 실행)")]
    public List<TriggerMoveSegment> segments = new();

    [Header("Move")]
    [Min(0f)] public float speed = 3f;
    [Tooltip("방향 벡터를 정규화해서 속도를 일정하게 유지")]
    public bool normalizeDirection = true;

    [Header("Time")]
    public bool useUnscaledTime = true;

    [Header("Lock")]
    public bool lockPlayerInput = true;

    [Tooltip("강제 이동 중 PlayerMove를 꺼서(velocity 덮어쓰기 방지) 충돌/물리 꼬임을 줄임")]
    public bool disablePlayerMoveWhileMoving = true;

    [Header("Physics")]
    [Tooltip("Rigidbody2D가 있으면 MovePosition으로 이동(권장)")]
    public bool preferRigidbodyMove = true;

    [Header("Debug")]
    public bool debugLog = false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (segments == null || segments.Count == 0)
        {
            if (debugLog) Debug.Log("[TriggerStep_PlayerMove] segments 비어있음");
            yield break;
        }

        // Player Transform 확보
        Transform playerTr = (ctx != null) ? ctx.player : null;
        PlayerMove pm = (ctx != null) ? ctx.playerMove : null;

        if (!playerTr)
        {
            if (!pm) pm = Object.FindObjectOfType<PlayerMove>(true);
            playerTr = pm ? pm.transform : null;
        }

        if (!playerTr)
        {
            Debug.LogWarning("[TriggerStep_PlayerMove] 플레이어 Transform을 찾지 못했습니다.");
            yield break;
        }

        // 입력 잠금
        bool lockedByMe = false;
        if (lockPlayerInput && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            lockedByMe = true;
        }

        // PlayerMove 비활성(선택)
        bool pmWasEnabled = false;
        if (disablePlayerMoveWhileMoving && pm != null)
        {
            pmWasEnabled = pm.enabled;
            pm.enabled = false;
        }

        // Rigidbody2D 우선 이동(선택)
        Rigidbody2D rb = null;
        if (preferRigidbodyMove)
        {
            rb = playerTr.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 강제 이동 시작 전 속도 제거(혹시 남아있으면)
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        // 구간 순차 실행
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.seconds <= 0f) continue;

            Vector2 dir = ResolveDir(seg);
            if (dir == Vector2.zero)
            {
                // 제자리 대기(원하면 이런 식으로 “정지 n초”도 가능)
                yield return Wait(seg.seconds);
                continue;
            }

            if (normalizeDirection) dir = dir.normalized;

            if (debugLog)
                Debug.Log($"[TriggerStep_PlayerMove] seg[{i}] dir={seg.dir} custom={seg.customDirection} sec={seg.seconds:0.00}");

            float t = 0f;
            while (t < seg.seconds)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += dt;

                Vector2 delta = dir * (speed * dt);

                if (rb != null)
                {
                    rb.MovePosition(rb.position + delta);
                }
                else
                {
                    playerTr.position += (Vector3)delta;
                }

                yield return null;
            }
        }

        // 마무리
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (disablePlayerMoveWhileMoving && pm != null)
        {
            pm.enabled = pmWasEnabled;
        }

        if (lockedByMe && GameManager.Instance != null)
        {
            GameManager.Instance.UnlockAction();
        }
    }

    private Vector2 ResolveDir(TriggerMoveSegment seg)
    {
        return seg.dir switch
        {
            TriggerMoveDir.Up => Vector2.up,
            TriggerMoveDir.Down => Vector2.down,
            TriggerMoveDir.Left => Vector2.left,
            TriggerMoveDir.Right => Vector2.right,
            TriggerMoveDir.Custom => seg.customDirection,
            _ => Vector2.zero
        };
    }

    private IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f) yield break;

        if (useUnscaledTime) yield return new WaitForSecondsRealtime(seconds);
        else yield return new WaitForSeconds(seconds);
    }
}
    