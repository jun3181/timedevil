// Assets/Script/Trigger/Steps/TriggerStep_SetActiveSwap.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_SetActiveSwap : TriggerStepBase
{
    [Header("Disable These")]
    [SerializeField] private List<GameObject> disableObjects = new();

    [Header("Enable These")]
    [SerializeField] private List<GameObject> enableObjects = new();

    [Header("Options")]
    [Tooltip("true면 Disable 먼저 하고 Enable을 합니다.")]
    [SerializeField] private bool disableFirst = true;

    [Tooltip("원하면 한 프레임 쉬고 적용(간헐적 타이밍 문제 방지용)")]
    [SerializeField] private bool waitOneFrameBeforeApply = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (waitOneFrameBeforeApply)
            yield return null;

        if (disableFirst)
        {
            ApplyList(disableObjects, false);
            ApplyList(enableObjects, true);
        }
        else
        {
            ApplyList(enableObjects, true);
            ApplyList(disableObjects, false);
        }

        yield break;
    }

    private void ApplyList(List<GameObject> list, bool active)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var go = list[i];
            if (!go) continue;

            go.SetActive(active);

            if (debugLog)
                Debug.Log($"[TriggerStep_SetActiveSwap] {(active ? "Enable" : "Disable")} -> {go.name}");
        }
    }
}
