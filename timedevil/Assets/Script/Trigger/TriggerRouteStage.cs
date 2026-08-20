using UnityEngine;

[System.Serializable]
public class TriggerRouteStage
{
    [Tooltip("TriggerRouter route key to request for this stage.")]
    public string routeKey = "Trigger1";

    [Min(0)]
    [Tooltip("0 repeats this stage forever. Positive values advance to the next stage after that many calls.")]
    public int maxCalls = 1;
}
