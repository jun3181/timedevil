using UnityEngine;

public class GridCardSpawnDebugZone : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MoveController moveController;

    [Header("Trigger")]
    [SerializeField] private Faction target = Faction.Player;
    [SerializeField] private Vector2Int triggerRC = new Vector2Int(1, 1);
    [SerializeField] private bool oneShot = true;

    [Header("Future Card Spawn")]
    [SerializeField] private string cardIdToCreate = "Card1";

    private bool triggered;

    private void Awake()
    {
        if (!moveController) moveController = FindObjectOfType<MoveController>(true);
    }

    private void OnEnable()
    {
        if (!moveController) moveController = FindObjectOfType<MoveController>(true);
        if (moveController != null) moveController.OnGridChanged += HandleGridChanged;
    }

    private void OnDisable()
    {
        if (moveController != null) moveController.OnGridChanged -= HandleGridChanged;
    }

    private void Start()
    {
        if (moveController != null)
            HandleGridChanged(target, moveController.GetGrid(target));
    }

    private void HandleGridChanged(Faction movedFaction, Vector2Int rc)
    {
        if (oneShot && triggered) return;
        if (movedFaction != target) return;
        if (rc != triggerRC) return;

        triggered = true;
        Debug.Log($"[GridCardSpawnDebugZone] {target} reached {triggerRC}. Card spawn candidate='{cardIdToCreate}'.");
    }
}