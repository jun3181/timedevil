// Assets/Script/Trigger/TriggerContext.cs
using UnityEngine;

public sealed class TriggerContext
{
    public readonly TriggerGet trigger;
    public readonly TriggerRouter router;

    public readonly GameObject instigator;
    public readonly Collider2D instigatorCollider;

    public readonly PlayerMove playerMove;
    public readonly Transform player;

    public TriggerContext(
        TriggerGet trigger,
        TriggerRouter router,
        GameObject instigator,
        Collider2D instigatorCollider,
        PlayerMove playerMove
    )
    {
        this.trigger = trigger;
        this.router = router;
        this.instigator = instigator;
        this.instigatorCollider = instigatorCollider;
        this.playerMove = playerMove;
        this.player = playerMove ? playerMove.transform : null;
    }
}
