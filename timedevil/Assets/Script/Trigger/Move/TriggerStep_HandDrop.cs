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
    [SerializeField] private bool strictAnimatorParamCheck = true;
    [SerializeField] private string paramIsChange = "isChange";
    [SerializeField] private string paramHAxisRaw = "hAxisRaw";
    [SerializeField] private string paramVAxisRaw = "vAxisRaw";
    [SerializeField] private bool setIdleAtEnd = true;

    [Header("Options")]
    [SerializeField] private bool forceDeactivateThenActivate = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private bool _cachedStart;
    private Vector3 _startPos;

    private void CacheStartIfNeeded()
    {
        if (_cachedStart) return;
        if (!handObject) return;

        _startPos = handObject.transform.position;   // 씬에 배치된 시작 위치
        _cachedStart = true;
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!handObject)
        {
            Debug.LogWarning("[TriggerStep_HandDrop] handObject가 비어있습니다.");
            yield break;
        }

        CacheStartIfNeeded();

        var tr = handObject.transform;
        Animator anim = handObject.GetComponent<Animator>();

        bool canDriveAnimator = driveAnimatorLikePlayerMove;
        if (canDriveAnimator && (!anim || (strictAnimatorParamCheck && !HasRequiredParams(anim))))
        {
            if (!anim)
                Debug.LogWarning("[TriggerStep_HandDrop] Animator를 찾지 못했습니다. Animator 구동만 건너뜁니다.");
            else
                Debug.LogWarning($"[TriggerStep_HandDrop] Animator 파라미터 누락: '{paramIsChange}', '{paramHAxisRaw}', '{paramVAxisRaw}'. Animator 구동만 건너뜁니다.");

            canDriveAnimator = false;
        }

        // 1) 비활성 -> 활성
        if (forceDeactivateThenActivate)
        {
            bool isSelfObject = ReferenceEquals(handObject, gameObject);
            if (isSelfObject)
            {
                Debug.LogWarning("[TriggerStep_HandDrop] handObject가 자기 자신입니다. SetActive(false) 시 코루틴이 중단되어 비활성/활성 토글을 건너뜁니다.");
                if (!handObject.activeSelf) handObject.SetActive(true);
            }
            else
            {
                handObject.SetActive(false);
                handObject.SetActive(true);
            }
        }
        else
        {
            if (!handObject.activeSelf) handObject.SetActive(true);
        }

        // 시작 위치 스냅
        tr.position = _startPos;

        if (useMoveSequence && moveSequence != null && moveSequence.Count > 0)
        {
            if (debugLog) Debug.Log($"[TriggerStep_HandDrop] sequence mode start count={moveSequence.Count}");

            HandDropMoveDirection lastDir = HandDropMoveDirection.Down;
            for (int i = 0; i < moveSequence.Count; i++)
            {
                var seg = moveSequence[i];
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
                        ApplyDirection(anim, seg.direction, false);

                    yield return null;
                }

                tr.position = to;
            }

            if (canDriveAnimator && anim && setIdleAtEnd)
                SetIdle(anim, lastDir);

            yield break;
        }

        // Legacy 단일 이동
        Vector3 legacyFrom = _startPos;
        Vector3 legacyTo = legacyFrom + new Vector3(moveDistanceX, moveDistanceY, 0f);

        // 시작 위치 스냅
        tr.position = legacyFrom;

        if (debugLog) Debug.Log($"[TriggerStep_HandDrop] legacy from={legacyFrom} to={legacyTo}");

        HandDropMoveDirection legacyDir = ResolveLegacyDirection(moveDistanceX, moveDistanceY);
        if (canDriveAnimator && anim)
        {
            ApplyDirection(anim, legacyDir, true);
            yield return null;
        }

        float legacyT = 0f;
        while (legacyT < dropDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            legacyT += dt;

            float u = Mathf.Clamp01(legacyT / dropDuration);
            float k = (ease != null) ? ease.Evaluate(u) : u;
            tr.position = Vector3.LerpUnclamped(legacyFrom, legacyTo, k);

            if (canDriveAnimator && anim)
                ApplyDirection(anim, legacyDir, false);

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
