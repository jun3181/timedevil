using UnityEngine;

[System.Serializable]
public class TriggerItemCompletionCondition
{
    [Tooltip("체크하면 아이템 수량이 충분할 때 Complete Route Key를 우선 실행합니다.")]
    [SerializeField] private bool useItemCondition = false;

    [Tooltip("검사할 ItemRuntime 아이템 id입니다.")]
    [SerializeField] private string itemId = "";

    [Tooltip("조건 만족에 필요한 최소 수량입니다.")]
    [Min(1)]
    [SerializeField] private int requiredQuantity = 1;

    [Tooltip("아이템 조건을 만족했을 때 실행할 TriggerRouter Route Key입니다.")]
    [SerializeField] private string completeRouteKey = "";

    [Tooltip("체크하면 완료 Route를 실행할 때 Required Quantity만큼 아이템을 소비합니다. 체크 해제하면 아이템은 남깁니다.")]
    [SerializeField] private bool consumeItemsOnComplete = false;

    public bool IsEnabled => useItemCondition;
    public string CompleteRouteKey => completeRouteKey;

    public TriggerItemRouteDecision Evaluate()
    {
        if (!useItemCondition)
            return TriggerItemRouteDecision.NoCondition();

        int required = Mathf.Max(1, requiredQuantity);
        int current = 0;

        if (ItemRuntime.Instance != null && !string.IsNullOrEmpty(itemId))
            current = ItemRuntime.Instance.GetQuantity(itemId);

        bool isMet = current >= required;

        return new TriggerItemRouteDecision(
            usesCondition: true,
            isMet: isMet,
            shouldConsumeItems: isMet && consumeItemsOnComplete,
            itemId: itemId,
            currentQuantity: current,
            requiredQuantity: required
        );
    }

    public void ConsumeRequiredItemsIfNeeded(TriggerItemRouteDecision decision)
    {
        if (!decision.shouldConsumeItems)
            return;

        if (ItemRuntime.Instance == null || string.IsNullOrEmpty(itemId))
            return;

        ItemRuntime.Instance.AddQuantity(itemId, -decision.requiredQuantity);
    }
}

public readonly struct TriggerItemRouteDecision
{
    public readonly bool usesCondition;
    public readonly bool isMet;
    public readonly bool shouldConsumeItems;
    public readonly string itemId;
    public readonly int currentQuantity;
    public readonly int requiredQuantity;

    public bool AllowsRoute => !usesCondition || isMet;

    public TriggerItemRouteDecision(
        bool usesCondition,
        bool isMet,
        bool shouldConsumeItems,
        string itemId,
        int currentQuantity,
        int requiredQuantity)
    {
        this.usesCondition = usesCondition;
        this.isMet = isMet;
        this.shouldConsumeItems = shouldConsumeItems;
        this.itemId = itemId;
        this.currentQuantity = currentQuantity;
        this.requiredQuantity = requiredQuantity;
    }

    public static TriggerItemRouteDecision NoCondition()
    {
        return new TriggerItemRouteDecision(
            usesCondition: false,
            isMet: true,
            shouldConsumeItems: false,
            itemId: string.Empty,
            currentQuantity: 0,
            requiredQuantity: 1
        );
    }
}
