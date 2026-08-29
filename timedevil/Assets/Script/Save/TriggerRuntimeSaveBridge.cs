using UnityEngine;

public static class TriggerRuntimeSaveBridge
{
    public static TriggerRuntimeSaveData Capture()
    {
        return new TriggerRuntimeSaveData
        {
            triggerGet = TriggerGet.CaptureRuntimeProgress(),
            interaction = TriggerRouterInteraction.CaptureRuntimeProgress()
        };
    }

    public static void Restore(TriggerRuntimeSaveData data)
    {
        if (data == null)
            data = new TriggerRuntimeSaveData();

        TriggerGet.RestoreRuntimeProgress(data.triggerGet);
        TriggerRouterInteraction.RestoreRuntimeProgress(data.interaction);
        WorldNPCStateService.Instance?.ClearAllTriggerRouteProgress();

#if UNITY_EDITOR
        int triggerGetCount = CountEntries(data.triggerGet);
        int interactionCount = CountEntries(data.interaction);
        Debug.Log($"[TriggerRuntimeSaveBridge] Restored trigger snapshot. TriggerGet={triggerGetCount}, Interaction={interactionCount}");
#endif
    }

    public static void ClearRuntime()
    {
        TriggerGet.ClearRuntimeProgress();
        TriggerRouterInteraction.ClearRuntimeProgress();
        WorldNPCStateService.Instance?.ClearAllTriggerRouteProgress();
    }

    private static int CountEntries(TriggerComponentSaveData data)
    {
        if (data == null) return 0;

        int count = 0;
        count += data.callCounts?.Count ?? 0;
        count += data.stageProgress?.Count ?? 0;
        count += data.completedIds?.Count ?? 0;
        return count;
    }
}
