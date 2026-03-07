// Assets/Script/Trigger/Move/TriggerStep_PlayerWalkAnimation.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ForcedWalkAnimDir
{
    Down,
    Up,
    Left,
    Right
}

[System.Serializable]
public struct ForcedWalkAnimSegment
{
    public ForcedWalkAnimDir direction;

    [Min(0f)]
    [Tooltip("해당 방향 Walk를 유지할 시간(초)")]
    public float duration;
}

[DisallowMultipleComponent]
public class TriggerStep_PlayerWalkAnimation : TriggerStepBase
{
    [Header("Sequence")]
    [SerializeField] private List<ForcedWalkAnimSegment> segments = new();

    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Control")]
    [SerializeField] private bool lockActionViaGameManager = false;
    [SerializeField] private bool disablePlayerMainManagerWhileRunning = true;
    [SerializeField] private bool zeroRigidbodyVelocity = true;

    [Header("End")]
    [SerializeField] private bool setIdleAtEnd = true;

    [Header("Animator Param Names")]
    [SerializeField] private string paramIsChange = "isChange";
    [SerializeField] private string paramHAxisRaw = "hAxisRaw";
    [SerializeField] private string paramVAxisRaw = "vAxisRaw";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (segments == null || segments.Count == 0)
        {
            if (debugLog) Debug.LogWarning("[TriggerStep_PlayerWalkAnimation] segments가 비어있어서 실행하지 않습니다.");
            yield break;
        }

        Transform playerTr = ResolvePlayerTransform(ctx);
        if (!playerTr)
        {
            Debug.LogWarning("[TriggerStep_PlayerWalkAnimation] Player Transform을 찾지 못했습니다.");
            yield break;
        }

        Animator anim = playerTr.GetComponent<Animator>();
        if (!anim)
        {
            Debug.LogWarning("[TriggerStep_PlayerWalkAnimation] Player Animator를 찾지 못했습니다.");
            yield break;
        }

        if (!HasRequiredParams(anim))
        {
            Debug.LogWarning($"[TriggerStep_PlayerWalkAnimation] Animator 파라미터 누락: '{paramIsChange}', '{paramHAxisRaw}', '{paramVAxisRaw}'");
            yield break;
        }

        bool heldLock = false;
        PlayerMainManager pmm = null;
        bool pmmPrevEnabled = false;
        Rigidbody2D rb = playerTr.GetComponent<Rigidbody2D>();

        if (lockActionViaGameManager && GameManager.Instance != null)
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

        if (rb && zeroRigidbodyVelocity)
            rb.velocity = Vector2.zero;

        ForcedWalkAnimDir lastDir = ForcedWalkAnimDir.Down;

        for (int i = 0; i < segments.Count; i++)
        {
            ForcedWalkAnimSegment seg = segments[i];
            if (seg.duration <= 0f)
            {
                if (debugLog)
                    Debug.Log($"[TriggerStep_PlayerWalkAnimation] seg[{i}] skipped (duration <= 0)");
                continue;
            }

            lastDir = seg.direction;

            ApplyDirection(anim, seg.direction, true);
            yield return null; // 전이 1프레임 보장

            float t = 0f;
            while (t < seg.duration)
            {
                ApplyDirection(anim, seg.direction, false);

                if (rb && zeroRigidbodyVelocity)
                    rb.velocity = Vector2.zero;

                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += dt;
                yield return null;
            }

            if (debugLog)
                Debug.Log($"[TriggerStep_PlayerWalkAnimation] seg[{i}] done dir={seg.direction} dur={seg.duration:0.###}");
        }

        if (setIdleAtEnd)
            SetIdle(anim, lastDir);

        if (rb && zeroRigidbodyVelocity)
            rb.velocity = Vector2.zero;

        if (pmm != null)
            pmm.enabled = pmmPrevEnabled;

        if (heldLock && GameManager.Instance != null)
            GameManager.Instance.UnlockAction();
    }

    private Transform ResolvePlayerTransform(TriggerContext ctx)
    {
        if (ctx != null)
        {
            if (ctx.player != null) return ctx.player;
            if (ctx.playerMove != null) return ctx.playerMove.transform;
            if (ctx.instigator != null) return ctx.instigator.transform;
        }

        PlayerMove pm = Object.FindObjectOfType<PlayerMove>(true);
        if (pm != null) return pm.transform;

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }

    private void ApplyDirection(Animator anim, ForcedWalkAnimDir dir, bool isChange)
    {
        int h = 0;
        int v = 0;

        switch (dir)
        {
            case ForcedWalkAnimDir.Down: v = -1; break;
            case ForcedWalkAnimDir.Up: v = 1; break;
            case ForcedWalkAnimDir.Left: h = -1; break;
            case ForcedWalkAnimDir.Right: h = 1; break;
        }

        anim.SetInteger(paramHAxisRaw, h);
        anim.SetInteger(paramVAxisRaw, v);
        anim.SetBool(paramIsChange, isChange);
    }

    private void SetIdle(Animator anim, ForcedWalkAnimDir dir)
    {
        // 현재 바라보던 방향의 Idle로 내려가도록 해당 축만 0으로 만든다.
        switch (dir)
        {
            case ForcedWalkAnimDir.Down:
            case ForcedWalkAnimDir.Up:
                anim.SetInteger(paramHAxisRaw, 0);
                anim.SetInteger(paramVAxisRaw, 0);
                break;

            case ForcedWalkAnimDir.Left:
            case ForcedWalkAnimDir.Right:
                anim.SetInteger(paramHAxisRaw, 0);
                anim.SetInteger(paramVAxisRaw, 0);
                break;
        }

        anim.SetBool(paramIsChange, false);
    }

    private bool HasRequiredParams(Animator anim)
    {
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

        return hasChange && hasH && hasV;
    }
}
