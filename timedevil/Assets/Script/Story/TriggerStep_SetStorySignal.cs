using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_SetStorySignal : TriggerStepBase
{
    [Header("Signal")]
    [SerializeField] private string targetSceneName = "chapter2";
    [SerializeField] private string signalKey = "lucas.met";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        StorySignalContext.SetNext(targetSceneName, signalKey);

        if (debugLog)
            Debug.Log($"[TriggerStep_SetStorySignal] Set '{signalKey}' for scene '{targetSceneName}'");

        yield break;
    }
}
