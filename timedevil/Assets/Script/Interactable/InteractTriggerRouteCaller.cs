// Assets/Script/Interactable/InteractTriggerRouteCaller.cs
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerRouterInteraction : MonoBehaviour, IInteractable
{
    [Header("Router (미지정 시 자동 탐색)")]
    [SerializeField] private TriggerRouter router;

    [Header("Route Key (필수)")]
    [SerializeField] private string routeKey = "Trigger1";

    [Header("Policy")]
    [SerializeField] private bool blockIfDialogueActive = true;
    [SerializeField] private bool blockIfGameActionLocked = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        if (!router) router = FindObjectOfType<TriggerRouter>(true);
    }

    public void Interact()
    {
        if (string.IsNullOrWhiteSpace(routeKey))
        {
            if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] routeKey가 비어있습니다.", this);
            return;
        }

        if (!router)
        {
            router = FindObjectOfType<TriggerRouter>(true);
            if (!router)
            {
                if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] TriggerRouter를 찾지 못했습니다.", this);
                return;
            }
        }

        if (blockIfDialogueActive && DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return;

        if (blockIfGameActionLocked && GameManager.Instance != null && GameManager.Instance.isAction)
            return;

        var pm = FindObjectOfType<PlayerMove>(true);
        if (!pm)
        {
            if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] PlayerMove를 찾지 못했습니다.", this);
            return;
        }

        var col = pm.GetComponent<Collider2D>();

        var ctx = new TriggerContext(
            trigger: null,
            router: router,
            instigator: pm.gameObject,
            instigatorCollider: col,
            playerMove: pm
        );

        bool accepted = router.RequestRoute(routeKey, ctx);

        if (debugLog)
        {
            Debug.Log(
                accepted
                    ? $"[TriggerRouterInteraction] RequestRoute ACCEPT key='{routeKey}' by='{pm.name}'"
                    : $"[TriggerRouterInteraction] RequestRoute REJECT key='{routeKey}' by='{pm.name}'",
                this
            );
        }
    }
}
