using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CostController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text costText;    // "Cost :" 텍스트
    [SerializeField] private Slider costBar;

    [Header("Rule")]
    [SerializeField] private int maxPerTurn = 10;

    public int Current { get; private set; }
    public int Max => maxPerTurn;

    public System.Action<int, int> onCostChanged;

    void Reset()
    {
        if (!costText) costText = GetComponentInChildren<TMP_Text>(true);
        if (!costBar) costBar = GetComponentInChildren<Slider>(true);
    }

    void Awake()
    {
        if (!costText) costText = GetComponentInChildren<TMP_Text>(true);
        ResetTurn();
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

    public int ReduceCurrent(int amount)
    {
        amount = Mathf.Max(0, amount);
        int reduced = Mathf.Min(Current, amount);
        Current -= reduced;
        RefreshUI();
        return reduced;
    }

    public int GainCurrent(int amount, bool allowOverMax = false)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return 0;

        int before = Current;
        Current = allowOverMax ? Current + amount : Mathf.Min(maxPerTurn, Current + amount);
        RefreshUI();
        return Current - before;
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

        if (costBar)
        {
            costBar.minValue = 0f;
            costBar.maxValue = Mathf.Max(1, maxPerTurn);
            costBar.value = Mathf.Clamp(Current, 0, maxPerTurn);
        }

        onCostChanged?.Invoke(Current, maxPerTurn);
    }
}
