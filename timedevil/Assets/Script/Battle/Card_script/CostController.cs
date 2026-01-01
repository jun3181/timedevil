using TMPro;
using UnityEngine;

public class CostController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text costText;    // "Cost :" 텍스트

    [Header("Rule")]
    [SerializeField] private int maxPerTurn = 10;

    public int Current { get; private set; }
    public int Max => maxPerTurn;

    public System.Action<int, int> onCostChanged;

    void Reset()
    {
        if (!costText) costText = GetComponentInChildren<TMP_Text>(true);
    }

    void Awake()
    {
        if (!costText) costText = GetComponentInChildren<TMP_Text>(true);
        ResetTurn(); // 첫 표시
    }

    public void SetMax(int max)
    {
        maxPerTurn = Mathf.Max(0, max);
        Current = Mathf.Min(Current, maxPerTurn);
        RefreshUI();
    }

    public void ResetTurn()
    {
        Current = maxPerTurn;
        RefreshUI();
    }

    public bool TryPay(int amount)
    {
        if (amount <= 0) return true;
        if (Current < amount) return false;

        Current -= amount;
        RefreshUI();
        return true;
    }

    public void Refund(int amount)
    {
        if (amount <= 0) return;
        Current = Mathf.Min(maxPerTurn, Current + amount);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (costText)
            costText.text = $"Cost : {Current}/{maxPerTurn}";
        onCostChanged?.Invoke(Current, maxPerTurn);
    }
}
