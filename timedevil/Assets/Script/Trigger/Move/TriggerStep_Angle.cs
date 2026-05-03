using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_Angle : TriggerStepBase
{
    public enum RotateDirection
    {
        Clockwise,
        CounterClockwise
    }

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool useLocalRotation = true;

    [Header("Rotation")]
    [Min(0f)][SerializeField] private float duration = 1f;
    [Min(0f)][SerializeField] private float angleDegrees = 90f;
    [SerializeField] private RotateDirection direction = RotateDirection.Clockwise;

    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private AnimationCurve easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        Transform tr = ResolveTarget(ctx);
        if (tr == null)
        {
            if (debugLog) Debug.LogWarning("[TriggerStep_Angle] target이 비어있습니다.");
            yield break;
        }

        float signedAngle = (direction == RotateDirection.Clockwise) ? -angleDegrees : angleDegrees;

        Vector3 startEuler = useLocalRotation ? tr.localEulerAngles : tr.eulerAngles;
        float startZ = startEuler.z;
        float endZ = startZ + signedAngle;

        if (duration <= 0f || Mathf.Approximately(angleDegrees, 0f))
        {
            ApplyRotation(tr, endZ);
            if (debugLog)
                Debug.Log($"[TriggerStep_Angle] instant rotate target={tr.name}, from={startZ:0.###} to={endZ:0.###}");
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = easing != null ? easing.Evaluate(t) : t;
            float z = Mathf.LerpUnclamped(startZ, endZ, eased);

            ApplyRotation(tr, z);
            yield return null;
        }

        ApplyRotation(tr, endZ);

        if (debugLog)
            Debug.Log($"[TriggerStep_Angle] done target={tr.name}, from={startZ:0.###} to={endZ:0.###}, dur={duration:0.###}");
    }

    private Transform ResolveTarget(TriggerContext ctx)
    {
        if (target != null) return target;
        if (ctx != null && ctx.instigator != null) return ctx.instigator.transform;
        return null;
    }

    private void ApplyRotation(Transform tr, float z)
    {
        if (useLocalRotation)
        {
            Vector3 e = tr.localEulerAngles;
            e.z = z;
            tr.localEulerAngles = e;
        }
        else
        {
            Vector3 e = tr.eulerAngles;
            e.z = z;
            tr.eulerAngles = e;
        }
    }
}
