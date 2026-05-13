// Assets/Script/Trigger/Steps/TriggerStep_HandDrop.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HandDropMoveDirection
{
    Up,
    Down,
    Left,
    Right
}

[System.Serializable]
public struct HandDropMoveSegment
{
    public HandDropMoveDirection direction;
    [Min(0.01f)] public float distance;
    [Min(0.01f)] public float duration;
}

public enum HandDropExecutionMode
{
    Simultaneous,
    Sequential
}

[System.Serializable]
public class HandDropTargetEntry
{
    public string name;
    public GameObject targetObject;
    public bool forceDeactivateThenActivate = true;
    public bool resetToInitialPosition = true;
    public bool useMoveSequence = false;
    public List<HandDropMoveSegment> moveSequence = new List<HandDropMoveSegment>();
    public float moveDistanceX = 0f;
    public float moveDistanceY = -3f;
    [Min(0.01f)] public float dropDuration = 0.12f;

    [Header("Animator (Optional)")]
    public bool driveAnimatorLikePlayerMove = false;
    public Animator animatorTarget;
}

[DisallowMultipleComponent]
public class TriggerStep_HandDrop : TriggerStepBase
{
    [Header("Hand")]
    [SerializeField] private GameObject handObject;   // 손 오브젝트(비활성 시작 권장)

    [Header("Legacy Move (X/Y)")]
    [Tooltip("X로 얼마나 이동할지(월드 기준). +면 오른쪽, -면 왼쪽")]
    [SerializeField] private float moveDistanceX = 0f;

    [Tooltip("Y로 얼마나 이동할지(월드 기준). +면 위, -면 아래")]
    [SerializeField] private float moveDistanceY = -3f;

    [Header("Sequence Move")]
    [Tooltip("체크하면 direction + sequence 기반 이동을 사용합니다.")]
    [SerializeField] private bool useMoveSequence = false;
    [SerializeField] private List<HandDropMoveSegment> moveSequence = new List<HandDropMoveSegment>();

    [Header("Timing")]
    [Min(0.01f)][SerializeField] private float dropDuration = 0.12f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Easing")]
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animator (Optional)")]
    [Tooltip("체크하면 이동 시 PlayerMove 스타일 파라미터를 함께 세팅합니다.")]
    [SerializeField] private bool driveAnimatorLikePlayerMove = false;
    [Tooltip("비우면 handObject에서 Animator를 찾습니다. 필요하면 직접 지정하세요.")]
    [SerializeField] private Animator animatorTarget;
    [SerializeField] private bool strictAnimatorParamCheck = true;
    [SerializeField] private string paramIsChange = "isChange";
    [SerializeField] private string paramHAxisRaw = "hAxisRaw";
    [SerializeField] private string paramVAxisRaw = "vAxisRaw";
    [SerializeField] private bool setIdleAtEnd = true;

    [Header("Options")]
    [SerializeField] private bool forceDeactivateThenActivate = true;

    [Header("Multi Target")]
    [SerializeField] private bool useMultiTarget = false;
    [SerializeField] private HandDropExecutionMode executionMode = HandDropExecutionMode.Simultaneous;
    [SerializeField] private List<HandDropTargetEntry> targets = new List<HandDropTargetEntry>();

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private bool _cachedStart;
    private Vector3 _startPos;
    private readonly Dictionary<GameObject, Vector3> _startPosByObject = new Dictionary<GameObject, Vector3>();

    private void CacheStartIfNeeded()
    {
        if (_cachedStart) return;
        if (!handObject) return;

        _startPos = handObject.transform.position;   // 씬에 배치된 시작 위치
        _cachedStart = true;
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (useMultiTarget)
        {
            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning("[TriggerStep_HandDrop] useMultiTarget=true 인데 targets가 비어있습니다.");
                yield break;
            }

            if (executionMode == HandDropExecutionMode.Sequential)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var entry = targets[i];
                    if (entry == null || !entry.targetObject)
                    {
                        if (debugLog) Debug.LogWarning($"[TriggerStep_HandDrop] targets[{i}] skip: targetObject 비어있음");
                        continue;
                    }
                    yield return ExecuteOne(entry.targetObject, entry.forceDeactivateThenActivate, entry.resetToInitialPosition,
                        entry.useMoveSequence, entry.moveSequence, entry.moveDistanceX, entry.moveDistanceY, entry.dropDuration,
                        entry.driveAnimatorLikePlayerMove, entry.animatorTarget);
                }
            }
            else
            {
                int pending = 0;
                for (int i = 0; i < targets.Count; i++)
                {
                    var entry = targets[i];
                    if (entry == null || !entry.targetObject)
                    {
                        if (debugLog) Debug.LogWarning($"[TriggerStep_HandDrop] targets[{i}] skip: targetObject 비어있음");
                        continue;
                    }

                    pending++;
                    StartCoroutine(CoExecuteOneAndSignal(entry, () => pending--));
                }

                while (pending > 0)
                    yield return null;
            }
            yield break;
        }

        if (!handObject)
        {
            Debug.LogWarning("[TriggerStep_HandDrop] handObject가 비어있습니다.");
            yield break;
        }

        yield return ExecuteOne(handObject, forceDeactivateThenActivate, true, useMoveSequence, moveSequence,
            moveDistanceX, moveDistanceY, dropDuration, driveAnimatorLikePlayerMove, animatorTarget);
    }

    private IEnumerator CoExecuteOneAndSignal(HandDropTargetEntry entry, System.Action onDone)
    {
        yield return ExecuteOne(entry.targetObject, entry.forceDeactivateThenActivate, entry.resetToInitialPosition,
            entry.useMoveSequence, entry.moveSequence, entry.moveDistanceX, entry.moveDistanceY, entry.dropDuration,
            entry.driveAnimatorLikePlayerMove, entry.animatorTarget);
        onDone?.Invoke();
    }

    private IEnumerator ExecuteOne(
        GameObject targetObject,
        bool forceDeactivateActivate,
        bool resetToInitialPos,
        bool useSequence,
        List<HandDropMoveSegment> sequence,
        float distanceX,
        float distanceY,
        float duration,
        bool driveAnimator,
        Animator animatorOverride)
    {
        CacheStartIfNeeded(targetObject);

        var tr = targetObject.transform;
        Animator anim = ResolveAnimator(targetObject, animatorOverride);

        bool canDriveAnimator = driveAnimator;
        if (canDriveAnimator && (!anim || (strictAnimatorParamCheck && !HasRequiredParams(anim))))
        {
            if (!anim)
                Debug.LogWarning($"[TriggerStep_HandDrop] Animator를 찾지 못했습니다. handObject='{targetObject.name}' animatorTarget='{(animatorOverride ? animatorOverride.name : "null")}'. Animator 구동만 건너뜁니다.");
            else
                Debug.LogWarning($"[TriggerStep_HandDrop] Animator 파라미터 누락: '{paramIsChange}', '{paramHAxisRaw}', '{paramVAxisRaw}' @ '{anim.name}'. Animator 구동만 건너뜁니다.");

            canDriveAnimator = false;
        }

        // 1) 비활성 -> 활성
        if (forceDeactivateActivate)
        {
            bool isSelfObject = ReferenceEquals(targetObject, gameObject);
            if (isSelfObject)
            {
                Debug.LogWarning("[TriggerStep_HandDrop] handObject가 자기 자신입니다. SetActive(false) 시 코루틴이 중단되어 비활성/활성 토글을 건너뜁니다.");
                if (!targetObject.activeSelf) targetObject.SetActive(true);
            }
            else
            {
                targetObject.SetActive(false);
                targetObject.SetActive(true);
            }
        }
        else
        {
            if (!targetObject.activeSelf) targetObject.SetActive(true);
        }

        if (resetToInitialPos)
            tr.position = _startPosByObject[targetObject];

        if (useSequence && sequence != null && sequence.Count > 0)
        {
            if (debugLog) Debug.Log($"[TriggerStep_HandDrop] sequence mode start count={sequence.Count}");

            HandDropMoveDirection lastDir = HandDropMoveDirection.Down;
            for (int i = 0; i < sequence.Count; i++)
            {
                var seg = sequence[i];
                if (seg.duration <= 0f || Mathf.Approximately(seg.distance, 0f))
                {
                    if (debugLog) Debug.Log($"[TriggerStep_HandDrop] seg[{i}] skipped");
                    continue;
                }

                lastDir = seg.direction;
                Vector3 from = tr.position;
                Vector3 to = from + DirectionToVector(seg.direction) * seg.distance;

                if (debugLog) Debug.Log($"[TriggerStep_HandDrop] seg[{i}] dir={seg.direction} from={from} to={to}");

                if (canDriveAnimator && anim)
                {
                    ApplyDirection(anim, seg.direction, true);
                    yield return null; // AnyState 전이 인지 보장
                }

                float t = 0f;
                while (t < seg.duration)
                {
                    float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    t += dt;
                    float u = Mathf.Clamp01(t / seg.duration);
                    float k = (ease != null) ? ease.Evaluate(u) : u;
                    tr.position = Vector3.LerpUnclamped(from, to, k);

                    if (canDriveAnimator && anim)
                        ApplyDirection(anim, seg.direction, true);

                    yield return null;
                }

                tr.position = to;
            }

            if (canDriveAnimator && anim && setIdleAtEnd)
                SetIdle(anim, lastDir);

            yield break;
        }

        // Legacy 단일 이동
        Vector3 legacyFrom = tr.position;
        Vector3 legacyTo = legacyFrom + new Vector3(distanceX, distanceY, 0f);

        // 시작 위치 스냅
        tr.position = legacyFrom;

        if (debugLog) Debug.Log($"[TriggerStep_HandDrop] legacy from={legacyFrom} to={legacyTo}");

        HandDropMoveDirection legacyDir = ResolveLegacyDirection(distanceX, distanceY);
        if (canDriveAnimator && anim)
        {
            ApplyDirection(anim, legacyDir, true);
            yield return null;
        }

        float legacyT = 0f;
        float effectiveDuration = Mathf.Max(0.01f, duration);
        while (legacyT < effectiveDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            legacyT += dt;

            float u = Mathf.Clamp01(legacyT / effectiveDuration);
            float k = (ease != null) ? ease.Evaluate(u) : u;
            tr.position = Vector3.LerpUnclamped(legacyFrom, legacyTo, k);

            if (canDriveAnimator && anim)
                ApplyDirection(anim, legacyDir, true);

            yield return null;
        }

        tr.position = legacyTo;

        if (canDriveAnimator && anim && setIdleAtEnd)
            SetIdle(anim, legacyDir);
    }

    private static Vector3 DirectionToVector(HandDropMoveDirection dir)
    {
        switch (dir)
        {
            case HandDropMoveDirection.Up: return Vector3.up;
            case HandDropMoveDirection.Down: return Vector3.down;
            case HandDropMoveDirection.Left: return Vector3.left;
            case HandDropMoveDirection.Right: return Vector3.right;
            default: return Vector3.zero;
        }
    }

    private static HandDropMoveDirection ResolveLegacyDirection(float x, float y)
    {
        if (Mathf.Abs(x) >= Mathf.Abs(y))
            return x >= 0f ? HandDropMoveDirection.Right : HandDropMoveDirection.Left;
        return y >= 0f ? HandDropMoveDirection.Up : HandDropMoveDirection.Down;
    }

    private void ApplyDirection(Animator anim, HandDropMoveDirection dir, bool isChange)
    {
        int h = 0;
        int v = 0;
        switch (dir)
        {
            case HandDropMoveDirection.Up: v = 1; break;
            case HandDropMoveDirection.Down: v = -1; break;
            case HandDropMoveDirection.Left: h = -1; break;
            case HandDropMoveDirection.Right: h = 1; break;
        }

        anim.SetInteger(paramHAxisRaw, h);
        anim.SetInteger(paramVAxisRaw, v);
        anim.SetBool(paramIsChange, isChange);
    }

    private void SetIdle(Animator anim, HandDropMoveDirection _)
    {
        anim.SetInteger(paramHAxisRaw, 0);
        anim.SetInteger(paramVAxisRaw, 0);
        anim.SetBool(paramIsChange, false);
    }


    private Animator ResolveAnimator(GameObject targetObject, Animator overrideAnimator)
    {
        if (overrideAnimator) return overrideAnimator;
        if (!targetObject) return null;

        Animator anim = targetObject.GetComponent<Animator>();
        if (anim) return anim;

        anim = targetObject.GetComponentInChildren<Animator>(true);
        if (anim) return anim;

        return targetObject.GetComponentInParent<Animator>();
    }

    private void CacheStartIfNeeded(GameObject targetObject)
    {
        if (!targetObject) return;
        if (_startPosByObject.ContainsKey(targetObject)) return;
        _startPosByObject[targetObject] = targetObject.transform.position;
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
