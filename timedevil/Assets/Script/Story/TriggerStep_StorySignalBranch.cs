using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class TriggerStep_StorySignalBranch : TriggerStepBase
{
    [Header("Signal")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private string signalKey = "lucas.met";
    [SerializeField] private bool useActiveSceneWhenTargetEmpty = true;
    [SerializeField] private bool consumeSignal = true;

    [Header("Run When Signal Exists")]
    [SerializeField] private List<TriggerStepBase> steps = new();

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        string sceneName = ResolveSceneName();
        if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(signalKey))
        {
            if (debugLog)
                Debug.LogWarning("[TriggerStep_StorySignalBranch] sceneName or signalKey is empty.", this);
            yield break;
        }

        bool hasSignal = consumeSignal
            ? StorySignalContext.TryConsume(sceneName, signalKey)
            : StorySignalContext.Has(sceneName, signalKey);

        if (!hasSignal)
        {
            if (debugLog)
                Debug.Log($"[TriggerStep_StorySignalBranch] skipped: '{signalKey}' not found for scene '{sceneName}'", this);
            yield break;
        }

        if (debugLog)
            Debug.Log($"[TriggerStep_StorySignalBranch] matched: '{signalKey}' for scene '{sceneName}'", this);

        if (steps == null)
            yield break;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step == null)
                continue;

            IEnumerator routine = null;
            try
            {
                routine = step.Execute(ctx);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TriggerStep_StorySignalBranch] step[{i}] Execute() threw: {e}", step);
            }

            if (routine != null)
                yield return routine;
        }
    }

    private string ResolveSceneName()
    {
        if (!string.IsNullOrWhiteSpace(targetSceneName))
            return targetSceneName;

        return useActiveSceneWhenTargetEmpty
            ? SceneManager.GetActiveScene().name
            : string.Empty;
    }
}
