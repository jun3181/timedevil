// Assets/Script/Player/PlayerInteractor.cs
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMove move;

    [Header("CircleCast")]
    [SerializeField] private float castOffset = 0.25f;
    [SerializeField] private float castDistance = 0.35f;
    [SerializeField] private float castRadius = 0.18f;

    [Header("Layer (Dialog/teleport/item_get/Object)")]
    [SerializeField] private LayerMask interactMask;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool debugDraw = true;

    private Collider2D currentHit;
    private GameObject currentTarget;
    private IInteractable currentInteractable;

    private void Reset()
    {
        move ??= GetComponent<PlayerMove>();

        // ✅ Object 추가
        if (interactMask.value == 0)
            interactMask = LayerMask.GetMask("Dialog", "teleport", "item_get", "Object");
    }

    private void Awake()
    {
        if (!move) move = GetComponent<PlayerMove>();

        // ✅ Object 추가
        if (interactMask.value == 0)
            interactMask = LayerMask.GetMask("Dialog", "teleport", "item_get", "Object");
    }

    private void Update()
    {
        ScanTarget();
    }

    private void ScanTarget()
    {
        if (!move) { ClearTarget(); return; }

        Vector2 dir = (Vector2)move.Facing;
        Vector2 origin = (Vector2)transform.position + dir * castOffset;

        var hit = Physics2D.CircleCast(origin, castRadius, dir, castDistance, interactMask);

        var nextHit = hit.collider;
        var nextTarget = nextHit ? nextHit.gameObject : null;

        if (nextTarget != currentTarget)
        {
            currentHit = nextHit;
            currentTarget = nextTarget;

            currentInteractable = currentHit
                ? (currentHit.GetComponent<IInteractable>() ?? currentHit.GetComponentInParent<IInteractable>())
                : null;

            if (debugLog)
            {
                if (currentTarget)
                {
                    string layerName = LayerMask.LayerToName(currentTarget.layer);
                    Debug.Log($"[PlayerInteractor] Target -> {currentTarget.name} (layer={layerName}) IInteractable={(currentInteractable != null ? "YES" : "NO")}");
                }
                else
                {
                    Debug.Log("[PlayerInteractor] Target -> (none)");
                }
            }
        }

        if (debugDraw)
            Debug.DrawRay(origin, dir * castDistance, currentTarget ? Color.yellow : Color.green);
    }

    private void ClearTarget()
    {
        currentHit = null;
        currentTarget = null;
        currentInteractable = null;
    }

    public bool TryInteract()
    {
        if (debugLog)
        {
            Debug.Log($"[TryInteract] target={(currentTarget ? currentTarget.name : "null")} activeDialogue={(DialogueManager.instance && DialogueManager.instance.isDialogueActive)}");
        }

        if (!currentTarget) return false;

        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return false;

        if (currentInteractable != null)
        {
            currentInteractable.Interact();
            return true;
        }

        if (debugLog)
        {
            string layerName = LayerMask.LayerToName(currentTarget.layer);
            Debug.LogWarning($"[TryInteract] FAIL: '{currentTarget.name}' (layer={layerName}) has no IInteractable (on hit or parent).");
        }
        return false;
    }
}
