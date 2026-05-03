using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_Angle : TriggerStepBase
{
    public enum RotateDirection
    {
        Clockwise,
        CounterClockwise
    }

    [Header("Targets")]
    [Tooltip("각도 조정을 적용할 오브젝트들")]
    [SerializeField] private List<Transform> targets = new();

    [Header("Rotation")]
    [Tooltip("회전 방향")]
    [SerializeField] private RotateDirection direction = RotateDirection.Clockwise;

    [Min(0f)]
    [Tooltip("회전할 각도(도)")]
    [SerializeField] private float angleDegrees = 90f;

    [Min(0f)]
    [Tooltip("회전에 걸리는 시간(초)")]
    [SerializeField] private float duration = 0.5f;

    [Tooltip("로컬 Z축 기준 회전(localRotation), 끄면 월드 Z축 기준(rotation)")]
    [SerializeField] private bool useLocalRotation = true;

    [Tooltip("true면 Time.unscaledDeltaTime 사용")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Options")]
    [Tooltip("대상 목록이 비어 있으면 자기 자신(transform)을 대상으로 사용")]
    [SerializeField] private bool useSelfWhenTargetsEmpty = true;

    [SerializeField] private bool debugLog = false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        List<Transform> runTargets = ResolveTargets();
        if (runTargets.Count == 0)
            yield break;

        float signedAngle = Mathf.Abs(angleDegrees);
        if (direction == RotateDirection.Clockwise)
            signedAngle *= -1f;

        if (duration <= 0f || Mathf.Approximately(signedAngle, 0f))
        {
            for (int i = 0; i < runTargets.Count; i++)
            {
                Transform tr = runTargets[i];
                if (!tr) continue;
                ApplyDelta(tr, signedAngle);
            }

            if (debugLog)
                Debug.Log($"[TriggerStep_Angle] instant rotate count={runTargets.Count}, angle={signedAngle:0.###}");
            yield break;
        }

        Quaternion[] fromRot = new Quaternion[runTargets.Count];
        Quaternion[] toRot = new Quaternion[runTargets.Count];

        for (int i = 0; i < runTargets.Count; i++)
        {
            Transform tr = runTargets[i];
            if (!tr) continue;

            Quaternion start = useLocalRotation ? tr.localRotation : tr.rotation;
            Quaternion end = start * Quaternion.Euler(0f, 0f, signedAngle);

            fromRot[i] = start;
            toRot[i] = end;

            if (debugLog)
                Debug.Log($"[TriggerStep_Angle] target={tr.name}, fromZ={start.eulerAngles.z:0.###}, toZ={end.eulerAngles.z:0.###}, dur={duration:0.###}");
        }

        float t = 0f;
        while (t < duration)
        {
            float ratio = Mathf.Clamp01(t / duration);

            for (int i = 0; i < runTargets.Count; i++)
            {
                Transform tr = runTargets[i];
                if (!tr) continue;

                Quaternion q = Quaternion.Slerp(fromRot[i], toRot[i], ratio);
                if (useLocalRotation) tr.localRotation = q;
                else tr.rotation = q;
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            yield return null;
        }

        for (int i = 0; i < runTargets.Count; i++)
        {
            Transform tr = runTargets[i];
            if (!tr) continue;

            if (useLocalRotation) tr.localRotation = toRot[i];
            else tr.rotation = toRot[i];
        }

        if (debugLog)
            Debug.Log($"[TriggerStep_Angle] complete count={runTargets.Count}, angle={signedAngle:0.###}, duration={duration:0.###}");
    }

    private void ApplyDelta(Transform tr, float signedAngle)
    {
        Quaternion baseRot = useLocalRotation ? tr.localRotation : tr.rotation;
        Quaternion nextRot = baseRot * Quaternion.Euler(0f, 0f, signedAngle);

        if (useLocalRotation) tr.localRotation = nextRot;
        else tr.rotation = nextRot;
    }

    private List<Transform> ResolveTargets()
    {
        List<Transform> list = new List<Transform>();

        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                    list.Add(targets[i]);
            }
        }

        if (list.Count == 0 && useSelfWhenTargetsEmpty)
            list.Add(transform);

        return list;
    }
}
