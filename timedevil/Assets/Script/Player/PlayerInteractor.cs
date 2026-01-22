using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMove move;

    [Header("CircleCast (짧은 레이저 + 두께)")]
    [SerializeField] private float castOffset = 0.25f;
    [SerializeField] private float castDistance = 0.35f;
    [SerializeField] private float castRadius = 0.18f;

    [Header("Layer (Dialog/teleport/item_get)")]
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

        // 이름 정확히: 네 프로젝트 레이어 이름이 "teleport", "item_get", "Dialog" 이거 맞아야 함(대소문자 포함)
        if (interactMask.value == 0)
            interactMask = LayerMask.GetMask("Dialog", "teleport", "item_get");
    }

    private void Awake()
    {
        if (!move) move = GetComponent<PlayerMove>();

        if (interactMask.value == 0)
            interactMask = LayerMask.GetMask("Dialog", "teleport", "item_get");
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

        // ✅ 핵심: Dialog만이 아니라 interactMask 전체를 스캔
        var hit = Physics2D.CircleCast(origin, castRadius, dir, castDistance, interactMask);

        var nextHit = hit.collider;
        var nextTarget = nextHit ? nextHit.gameObject : null;

        if (nextTarget != currentTarget)
        {
            currentHit = nextHit;
            currentTarget = nextTarget;

            // ✅ 스크립트가 부모에 달린 경우가 흔해서 InParent까지
            currentInteractable = currentHit
                ? (currentHit.GetComponent<IInteractable>() ?? currentHit.GetComponentInParent<IInteractable>())
                : null;

            if (debugLog)
            {
                if (currentTarget)
                {
                    string layerName = LayerMask.LayerToName(currentTarget.layer);
                    Debug.Log($"[PlayerInteractor] Target -> {currentTarget.name} (layer={layerName}) " +
                              $"IInteractable={(currentInteractable != null ? "YES" : "NO")} " +
                              $"hitCollider={currentHit.GetType().Name}");
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
            Debug.Log($"[TryInteract] target={(currentTarget ? currentTarget.name : "null")} " +
                      $"activeDialogue={(DialogueManager.instance && DialogueManager.instance.isDialogueActive)}");
        }

        if (!currentTarget) return false;

        // 대화 중이면(넘기기는 PlayerMainManager에서 처리)
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return false;

        if (currentInteractable != null)
        {
            if (debugLog) Debug.Log($"[TryInteract] Interact -> {((MonoBehaviour)currentInteractable).name}");
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
