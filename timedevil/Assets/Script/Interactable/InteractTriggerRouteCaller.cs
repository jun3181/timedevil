// Assets/Script/Interactable/TriggerRouterInteraction.cs
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerRouterInteraction : MonoBehaviour, IInteractable
{
    [Header("Router (비우면 씬에서 자동 탐색)")]
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

        // PlayerMove 기반으로 TriggerContext 구성
        var pm = FindObjectOfType<PlayerMove>(true);
        if (!pm)
        {
            if (debugLog) Debug.LogWarning("[TriggerRouterInteraction] PlayerMove를 찾지 못했습니다.", this);
            return;
        }

        var col = pm.GetComponent<Collider2D>(); // 없어도 ctx에는 null로 들어가도 됨

        var ctx = new TriggerContext(
            trigger: null,                 // TriggerGet 기반이 아니라서 null
            router: router,
            instigator: pm.gameObject,     // 상호작용 주체 = 플레이어
            instigatorCollider: col,
            playerMove: pm
        );

        if (debugLog)
            Debug.Log($"[TriggerRouterInteraction] RequestRoute key='{routeKey}' by='{pm.name}'", this);

        router.RequestRoute(routeKey, ctx);
    }
}
