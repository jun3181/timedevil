using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PivotSequencePlayMode
{
    Sequential,
    Parallel
}


public enum PivotFinalFacingApplyMode
{
    IdleDirect,
    WalkThenIdle
}

public enum PivotFacingMode
{
    KeepCurrent,
    AutoFromMoveDirection,
    Up,
    Down,
    Left,
    Right
}

[System.Serializable]
public struct PivotSequenceEntry
{
    public Transform target;
    public Transform pivot;

    [Min(0f)] public float duration;
    [Min(0f)] public float speed;

    public Animator animatorOverride;
    public bool setIdleAtEnd;
    public PivotFacingMode finalFacing;
}

[System.Serializable]
public class PivotSequenceElement
{
    public PivotSequencePlayMode playMode = PivotSequencePlayMode.Sequential;
    public List<PivotSequenceEntry> entries = new();
}

[DisallowMultipleComponent]
public class TriggerStep_PivotSequenceMove : TriggerStepBase
{
    private const string DefaultParamIsChange = "isChange";
    private const string DefaultParamHAxisRaw = "hAxisRaw";
    private const string DefaultParamVAxisRaw = "vAxisRaw";

    [Header("Sequence")]
    [SerializeField] private List<PivotSequenceElement> elements = new();

    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Final Facing")]
    [SerializeField] private PivotFinalFacingApplyMode finalFacingApplyMode = PivotFinalFacingApplyMode.IdleDirect;
    [SerializeField, Min(0)] private int finalFacingHoldFrames = 2;

    [Header("Input Lock")]
    [SerializeField] private bool lockPlayerInput = false;
    [SerializeField] private bool unlockInputAtEnd = true;

    [Header("Animation Params")]
    [SerializeField] private string paramIsChange = DefaultParamIsChange;
    [SerializeField] private string paramHAxisRaw = DefaultParamHAxisRaw;
    [SerializeField] private string paramVAxisRaw = DefaultParamVAxisRaw;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        bool heldLock = false;
        if (lockPlayerInput && GameManager.Instance != null)
        {
            GameManager.Instance.LockAction();
            heldLock = true;
        }

        for (int ei = 0; ei < elements.Count; ei++)
        {
            var element = elements[ei];
            if (element == null || element.entries == null || element.entries.Count == 0)
                continue;

            if (element.playMode == PivotSequencePlayMode.Parallel)
                yield return RunParallelElement(ei, element.entries);
            else
                yield return RunSequentialElement(ei, element.entries);
        }

        if (heldLock && unlockInputAtEnd && GameManager.Instance != null)
            GameManager.Instance.UnlockAction();
    }

    private IEnumerator RunSequentialElement(int elementIndex, List<PivotSequenceEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
            yield return RunOneMove(elementIndex, i, entries[i]);
    }

    private IEnumerator RunParallelElement(int elementIndex, List<PivotSequenceEntry> entries)
    {
        var dones = new bool[entries.Count];
        int doneCount = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            int index = i;
            StartCoroutine(CoRunAndMark(elementIndex, index, entries[index], () =>
            {
                if (!dones[index])
                {
                    dones[index] = true;
                    doneCount++;
                }
            }));
        }

        while (doneCount < entries.Count)
            yield return null;
    }

    private IEnumerator CoRunAndMark(int elementIndex, int entryIndex, PivotSequenceEntry entry, System.Action onDone)
    {
        yield return RunOneMove(elementIndex, entryIndex, entry);
        onDone?.Invoke();
    }

    private IEnumerator RunOneMove(int elementIndex, int entryIndex, PivotSequenceEntry entry)
    {
        if (entry.target == null || entry.pivot == null)
        {
            if (debugLog) Debug.LogWarning($"[TriggerStep_PivotSequenceMove] skipped element={elementIndex}, entry={entryIndex} (target/pivot null)");
            yield break;
        }

        var animator = entry.animatorOverride != null ? entry.animatorOverride : entry.target.GetComponent<Animator>();
        bool canDriveAnim = HasAnimParams(animator);

        Vector3 from = entry.target.position;
        Vector3 to = entry.pivot.position;
        Vector2 dir = (Vector2)(to - from);
        ForcedWalkAnimType animType = ResolveAnimByDirection(dir);

        float distance = Vector2.Distance(from, to);
        float duration = entry.duration > 0f ? entry.duration : (entry.speed > 0f ? distance / entry.speed : 0f);

        if (debugLog)
            Debug.Log($"[TriggerStep_PivotSequenceMove] element={elementIndex}, entry={entryIndex}, from={from}, to={to}, duration={duration:0.###}, anim={animType}");

        if (canDriveAnim)
        {
            ApplyWalkAnimation(animator, animType, true);
            yield return null;
        }

        if (duration <= 0f || distance <= 0.0001f)
        {
            entry.target.position = to;
        }
        else
        {
            float t = 0f;
            while (t < duration)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += dt;

                float u = Mathf.Clamp01(t / duration);
                float k = ease != null ? ease.Evaluate(u) : u;
                entry.target.position = Vector3.LerpUnclamped(from, to, k);

                if (canDriveAnim)
                    ApplyWalkAnimation(animator, animType, true);

                yield return null;
            }

            entry.target.position = to;
        }

        if (canDriveAnim)
        {
            // 이동 루프 종료 직후 걷기 플래그를 명시적으로 끈 뒤, 최종 바라보기를 적용한다.
            // (Animator 전이 조건이 엄격한 컨트롤러에서 걷기 상태 고정 방지)
            ApplyWalkAnimation(animator, animType, false);
            yield return null;

            if (entry.setIdleAtEnd)
                SetIdle(animator);

            yield return ApplyFinalFacingStable(animator, entry.finalFacing, animType);
        }
    }

    private ForcedWalkAnimType ResolveAnimByDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return dir.x >= 0f ? ForcedWalkAnimType.RightWalk : ForcedWalkAnimType.LeftWalk;

        return dir.y >= 0f ? ForcedWalkAnimType.UpWalk : ForcedWalkAnimType.DownWalk;
    }

    private bool HasAnimParams(Animator anim)
    {
        if (!anim) return false;

        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        string pH = string.IsNullOrWhiteSpace(paramHAxisRaw) ? DefaultParamHAxisRaw : paramHAxisRaw;
        string pV = string.IsNullOrWhiteSpace(paramVAxisRaw) ? DefaultParamVAxisRaw : paramVAxisRaw;

        bool hasChange = false, hasH = false, hasV = false;
        var pars = anim.parameters;
        for (int i = 0; i < pars.Length; i++)
        {
            var p = pars[i];
            if (p.name == pChange && p.type == AnimatorControllerParameterType.Bool) hasChange = true;
            else if (p.name == pH && p.type == AnimatorControllerParameterType.Int) hasH = true;
            else if (p.name == pV && p.type == AnimatorControllerParameterType.Int) hasV = true;
        }

        return hasChange && hasH && hasV;
    }

    private void ApplyWalkAnimation(Animator anim, ForcedWalkAnimType animType, bool isChange)
    {
        if (!anim) return;

        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        string pH = string.IsNullOrWhiteSpace(paramHAxisRaw) ? DefaultParamHAxisRaw : paramHAxisRaw;
        string pV = string.IsNullOrWhiteSpace(paramVAxisRaw) ? DefaultParamVAxisRaw : paramVAxisRaw;

        int h = 0, v = 0;
        switch (animType)
        {
            case ForcedWalkAnimType.DownWalk: v = -1; break;
            case ForcedWalkAnimType.UpWalk: v = 1; break;
            case ForcedWalkAnimType.LeftWalk: h = -1; break;
            case ForcedWalkAnimType.RightWalk: h = 1; break;
        }

        anim.SetInteger(pH, h);
        anim.SetInteger(pV, v);
        anim.SetBool(pChange, isChange);
    }


    private IEnumerator ApplyFinalFacingStable(Animator anim, PivotFacingMode mode, ForcedWalkAnimType moveAnimType)
    {
        if (!anim) yield break;
        if (mode == PivotFacingMode.KeepCurrent) yield break;

        int hold = Mathf.Max(0, finalFacingHoldFrames);

        if (finalFacingApplyMode == PivotFinalFacingApplyMode.WalkThenIdle)
        {
            // 1) 최종 방향으로 "걷는 상태"를 한 프레임이라도 거치게 하여
            //    AnyState -> Walk 전이를 선호하는 컨트롤러에서도 확실히 상태가 바뀌도록 한다.
            ApplyFinalFacing(anim, mode, moveAnimType, true);
            yield return null;
        }

        // 2) 최종적으로는 Idle 고정
        if (hold <= 0)
        {
            ApplyFinalFacing(anim, mode, moveAnimType, false);
            yield break;
        }

        for (int i = 0; i < hold; i++)
        {
            ApplyFinalFacing(anim, mode, moveAnimType, false);
            yield return null;
        }
    }

    private void ApplyFinalFacing(Animator anim, PivotFacingMode mode, ForcedWalkAnimType moveAnimType, bool isChange)
    {
        if (!anim) return;
        if (mode == PivotFacingMode.KeepCurrent) return;

        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        string pH = string.IsNullOrWhiteSpace(paramHAxisRaw) ? DefaultParamHAxisRaw : paramHAxisRaw;
        string pV = string.IsNullOrWhiteSpace(paramVAxisRaw) ? DefaultParamVAxisRaw : paramVAxisRaw;

        ForcedWalkAnimType facing = moveAnimType;
        switch (mode)
        {
            case PivotFacingMode.AutoFromMoveDirection: facing = moveAnimType; break;
            case PivotFacingMode.Up: facing = ForcedWalkAnimType.UpWalk; break;
            case PivotFacingMode.Down: facing = ForcedWalkAnimType.DownWalk; break;
            case PivotFacingMode.Left: facing = ForcedWalkAnimType.LeftWalk; break;
            case PivotFacingMode.Right: facing = ForcedWalkAnimType.RightWalk; break;
            default: return;
        }

        int h = 0, v = 0;
        switch (facing)
        {
            case ForcedWalkAnimType.DownWalk: v = -1; break;
            case ForcedWalkAnimType.UpWalk: v = 1; break;
            case ForcedWalkAnimType.LeftWalk: h = -1; break;
            case ForcedWalkAnimType.RightWalk: h = 1; break;
        }

        anim.SetInteger(pH, h);
        anim.SetInteger(pV, v);
        anim.SetBool(pChange, isChange);
    }

    private void SetIdle(Animator anim)
    {
        if (!anim) return;

        string pChange = string.IsNullOrWhiteSpace(paramIsChange) ? DefaultParamIsChange : paramIsChange;
        string pH = string.IsNullOrWhiteSpace(paramHAxisRaw) ? DefaultParamHAxisRaw : paramHAxisRaw;
        string pV = string.IsNullOrWhiteSpace(paramVAxisRaw) ? DefaultParamVAxisRaw : paramVAxisRaw;

        anim.SetInteger(pH, 0);
        anim.SetInteger(pV, 0);
        anim.SetBool(pChange, false);
    }
}
