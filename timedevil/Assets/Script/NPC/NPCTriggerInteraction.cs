using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCTriggerInteraction : MonoBehaviour, IInteractable
{
    [Header("TriggerRouter")]
    private TriggerRouter router;

    [Header("Route Key")]
    private string key;

    private WanderingNPCMove wanderingNpcMove;
    private RoutingNPCMove routingNpcMove;

    private bool interactionLocked = false;

    private GameObject playerObject;

    void Start()
    {
        wanderingNpcMove = GetComponent<WanderingNPCMove>();
        routingNpcMove = GetComponent<RoutingNPCMove>();

        if(wanderingNpcMove && routingNpcMove) {
            Debug.LogWarning($"{gameObject.name}내에 WanderingNPCMove와 RoutingNPCMove가 같이 존재해서는 안됩니다.");
            interactionLocked = true;
        }

        playerObject = GameObject.FindWithTag("Player");
    }

    public void Interact() {
        if(interactionLocked) {
            Debug.LogWarning($"{gameObject.name}내에 WanderingNPCMove와 RoutingNPCMove가 같이 존재하여 Interact가 실행되지 않습니다.");
            return;
        }

        if(wanderingNpcMove) {
            wanderingNpcMove.Idle();
        } else if(routingNpcMove) {
            routingNpcMove.StopRouting();
        }

        if(playerObject == null) 
            playerObject = GameObject.FindWithTag("Player");

        TriggerContext ctx = new(
            trigger: null,
            router: router,
            instigator: playerObject,
            instigatorCollider: playerObject.GetComponent<Collider2D>(),
            playerMove: playerObject.GetComponent<PlayerMove>()
            );

        router.RequestRoute(key, ctx);
    }
}
