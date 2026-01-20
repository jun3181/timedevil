using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMove move;
    [SerializeField] private NextScene nextScene;
    [SerializeField] private GetManager getManager;
    [SerializeField] private GameManager gameManager;

    [Header("Raycast")]
    [SerializeField] private float raycastOffset = 0.5f;
    [SerializeField] private float normalRayDistance = 0.01f;
    [SerializeField] private float downRayDistance = 0.25f;
    [SerializeField] private LayerMask mask;

    [Header("Debug")]
    [SerializeField] private bool debugRay = false;

    private GameObject scanObject;

    private void Reset()
    {
        move ??= GetComponent<PlayerMove>();
        gameManager ??= GameManager.Instance;
        nextScene ??= FindObjectOfType<NextScene>(true);
        getManager ??= FindObjectOfType<GetManager>(true);

        if (mask.value == 0) mask = LayerMask.GetMask("Object", "teleport", "item_get");
    }

    private void Awake()
    {
        if (!move) move = GetComponent<PlayerMove>();
        if (!gameManager) gameManager = GameManager.Instance;
        if (!nextScene) nextScene = FindObjectOfType<NextScene>(true);
        if (!getManager) getManager = FindObjectOfType<GetManager>(true);

        if (mask.value == 0) mask = LayerMask.GetMask("Object", "teleport", "item_get");
    }

    private void Update()
    {
        Scan();
    }

    private void Scan()
    {
        if (!move) { scanObject = null; return; }

        Vector3 dir = move.Facing;
        float dist = (dir == Vector3.down) ? downRayDistance : normalRayDistance;
        Vector2 origin = (Vector2)transform.position + (Vector2)dir * raycastOffset;

        if (debugRay) Debug.DrawRay(origin, (Vector2)dir * dist, Color.green);

        var hit = Physics2D.Raycast(origin, dir, dist, mask);
        scanObject = hit.collider ? hit.collider.gameObject : null;
    }

    public bool TryInteract()
    {
        if (!scanObject) return false;

        // 대화 중엔 상호작용 금지
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
            return false;

        // 1) IInteractable 우선
        var interactable = scanObject.GetComponent<IInteractable>();
        if (interactable != null)
        {
            Debug.Log($"[PlayerInteractor] Interact -> {scanObject.name} (IInteractable)", this);
            interactable.Interact();
            return true;
        }

        // 2) 레거시 폴백
        int layer = scanObject.layer;

        if (layer == LayerMask.NameToLayer("teleport"))
        {
            Debug.Log($"[PlayerInteractor] teleport -> {scanObject.name}", this);
            nextScene?.LoadBattleScene(scanObject);
            return true;
        }

        if (layer == LayerMask.NameToLayer("item_get"))
        {
            Debug.Log($"[PlayerInteractor] item_get -> {scanObject.name}", this);
            getManager?.Action(scanObject);
            return true;
        }

        Debug.Log($"[PlayerInteractor] GameManager.Action -> {scanObject.name}", this);
        gameManager?.Action(scanObject);
        return true;
    }
}
