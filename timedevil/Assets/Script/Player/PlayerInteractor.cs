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

    [Header("Layer (always included)")]
    [SerializeField] private LayerMask interactMask;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool debugDraw = true;

    private Collider2D currentHit;
    private GameObject currentTarget;
    private IInteractable[] currentInteractables;
    private bool isInteractionSequenceRunning;

    private static readonly string[] RequiredLayers =
        { "Dialog", "teleport", "item_get", "Object", "Save" };

    private void Reset()
    {
        move ??= GetComponent<PlayerMove>();
        EnsureMaskIncludesRequired();
    }

    private void Awake()
    {
        if (!move) move = GetComponent<PlayerMove>();
        EnsureMaskIncludesRequired();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 인스펙터에서 레이어 마스크가 꼬여도 자동으로 포함되게
        EnsureMaskIncludesRequired();
    }
#endif

    private void EnsureMaskIncludesRequired()
    {
        int requiredMask = 0;

        for (int i = 0; i < RequiredLayers.Length; i++)
        {
            string layerName = RequiredLayers[i];
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                if (debugLog)
                    Debug.LogWarning($"[PlayerInteractor] Layer '{layerName}' not found. (Project Settings > Tags and Layers 확인)");
                continue;
            }
            requiredMask |= (1 << layer);
        }

        //  인스펙터에 뭐가 들어있든, 요구 레이어는 "항상" 포함
        interactMask |= requiredMask;
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

            currentInteractables = currentHit
                ? GetInteractablesInCallOrder(currentHit)
                : null;

            if (debugLog)
            {
                if (currentTarget)
                {
                    string layerName = LayerMask.LayerToName(currentTarget.layer);
                    Debug.Log($"[PlayerInteractor] Target -> {currentTarget.name} (layer={layerName}) IInteractable={(HasInteractables() ? "YES" : "NO")}");
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
        currentInteractables = null;
    }

    public bool TryInteract()
    {
        if (debugLog)
        {
            Debug.Log($"[TryInteract] target={(currentTarget ? currentTarget.name : "null")} activeDialogue={(DialogueManager.instance && DialogueManager.instance.isDialogueActive)}");
        }

        if (!currentTarget || isInteractionSequenceRunning) return false;

        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return false;

        if (HasInteractables())
        {
            StartCoroutine(InteractSequentially(currentInteractables));
            return true;
        }

        if (debugLog)
        {
            string layerName = LayerMask.LayerToName(currentTarget.layer);
            Debug.LogWarning($"[TryInteract] FAIL: '{currentTarget.name}' (layer={layerName}) has no IInteractable (on hit or parent).");
        }
        return false;
    }

    private bool HasInteractables()
    {
        return currentInteractables != null && currentInteractables.Length > 0;
    }

    private static IInteractable[] GetInteractablesInCallOrder(Collider2D hit)
    {
        IInteractable[] interactables = hit.GetComponents<IInteractable>();
        if (interactables != null && interactables.Length > 0)
            return interactables;

        return hit.GetComponentsInParent<IInteractable>();
    }

    private System.Collections.IEnumerator InteractSequentially(IInteractable[] interactables)
    {
        isInteractionSequenceRunning = true;

        for (int i = 0; i < interactables.Length; i++)
        {
            interactables[i]?.Interact();

            if (i < interactables.Length - 1 && DialogueManager.instance != null)
                yield return new WaitWhile(() => DialogueManager.instance != null && DialogueManager.instance.isDialogueActive);
        }

        isInteractionSequenceRunning = false;
    }
}