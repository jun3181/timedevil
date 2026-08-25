using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_SetPartyFollower : TriggerStepBase
{
    [Header("Follower")]
    [SerializeField] private PartyFollower2D follower;
    [SerializeField] private bool startFollowing = true;
    [SerializeField] private Transform targetOverride;

    [Header("Timing")]
    [SerializeField] private bool waitOneFrameAfterApply = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!follower)
            follower = GetComponent<PartyFollower2D>();

        if (!follower)
        {
            Debug.LogWarning("[TriggerStep_SetPartyFollower] Follower is missing.", this);
            yield break;
        }

        Transform target = targetOverride;
        if (!target && ctx != null)
            target = ctx.player;

        follower.SetFollowing(startFollowing, target);

        if (debugLog)
            Debug.Log($"[TriggerStep_SetPartyFollower] {(startFollowing ? "Start" : "Stop")} '{follower.name}'", follower);

        if (waitOneFrameAfterApply)
            yield return null;
    }
}
