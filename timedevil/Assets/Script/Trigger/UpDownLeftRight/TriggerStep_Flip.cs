using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_Flip : TriggerStepBase
{
    public enum FlipAxis
    {
        Horizontal,
        Vertical,
        Both
    }

    public enum FlipMode
    {
        Toggle,
        ForceFlipped,
        ForceNormal
    }

    [Header("Targets")]
    [Tooltip("반전을 적용할 오브젝트들")]
    [SerializeField] private List<Transform> targets = new();

    [Header("Flip")]
    [SerializeField] private FlipAxis axis = FlipAxis.Horizontal;
    [SerializeField] private FlipMode mode = FlipMode.Toggle;

    [Header("Options")]
    [Tooltip("대상 목록이 비어 있으면 Trigger 실행 오브젝트(transform)를 대상으로 사용")]
    [SerializeField] private bool useSelfWhenTargetsEmpty = true;

    [SerializeField] private bool debugLog = false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        var runTargets = ResolveTargets();

        for (int i = 0; i < runTargets.Count; i++)
        {
            var tr = runTargets[i];
            if (!tr) continue;

            Vector3 s = tr.localScale;

            switch (axis)
            {
                case FlipAxis.Horizontal:
                    s.x = ResolveScaleSign(s.x);
                    break;

                case FlipAxis.Vertical:
                    s.y = ResolveScaleSign(s.y);
                    break;

                case FlipAxis.Both:
                    s.x = ResolveScaleSign(s.x);
                    s.y = ResolveScaleSign(s.y);
                    break;
            }

            tr.localScale = s;

            if (debugLog)
                Debug.Log($"[TriggerStep_Flip] target={tr.name}, axis={axis}, mode={mode}, scale={s}");
        }

        yield break;
    }

    private float ResolveScaleSign(float value)
    {
        float abs = Mathf.Abs(value);
        if (abs <= 0.0001f) abs = 1f;

        switch (mode)
        {
            case FlipMode.Toggle:
                return -value;

            case FlipMode.ForceFlipped:
                return -abs;

            case FlipMode.ForceNormal:
                return abs;

            default:
                return value;
        }
    }

    private List<Transform> ResolveTargets()
    {
        var list = new List<Transform>();

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
