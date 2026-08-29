// SupportCardSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum SupportEffectCategory { Cost, HP, Stat, Trap, Guard }

public enum SupportTarget
{
    Self,
    Opponent,
    Both
}

public enum SupportPanelSide
{
    SelfPanel,
    OpponentPanel
}

public enum SupportCostEffectType
{
    NextCardFree,
    GainCostByPayingHpPercent,
    GainCostByDiscardHand
}

public enum SupportHpEffectType
{
    InstantChange,
    TurnStartChange
}

public enum SupportStatEffectType
{
    AttackChange,
    DefenseChange
}

[Serializable]
public class SupportGridMask
{
    [SerializeField] private bool[] cells = new bool[16];

    public bool Contains(Vector2Int rc)
    {
        int index = ToIndex(rc);
        return index >= 0 && cells != null && cells.Length > index && cells[index];
    }

    public bool IsEmpty()
    {
        if (cells == null) return true;
        for (int i = 0; i < cells.Length; i++)
            if (cells[i]) return false;
        return true;
    }

    public string ToPattern16()
    {
        EnsureSize();
        char[] pattern = new char[16];
        for (int i = 0; i < pattern.Length; i++)
            pattern[i] = cells[i] ? '1' : '0';
        return new string(pattern);
    }

    public void EnsureSize()
    {
        if (cells != null && cells.Length == 16)
            return;

        bool[] resized = new bool[16];
        if (cells != null)
        {
            int copyCount = Mathf.Min(cells.Length, resized.Length);
            for (int i = 0; i < copyCount; i++)
                resized[i] = cells[i];
        }
        cells = resized;
    }

    public static int ToIndex(Vector2Int rc)
    {
        if (rc.x < 1 || rc.x > 4 || rc.y < 1 || rc.y > 4)
            return -1;
        return (rc.x - 1) * 4 + (rc.y - 1);
    }
}

[Serializable]
public class SupportTrapPlacement
{
    public SupportPanelSide panelSide = SupportPanelSide.OpponentPanel;
    public SupportGridMask gridMask = new SupportGridMask();

    [Header("Trap Visual")]
    public GameObject trapPrefab;
    public Vector3 trapPrefabOffset = Vector3.zero;
    public float trapPrefabZ = -5f;
    [Min(0f)] public float trapPrefabScale = 1f;
}

[Serializable]
public class SupportEffect
{
    public SupportEffectCategory category = SupportEffectCategory.HP;

    public SupportTarget target = SupportTarget.Self;

    public SupportCostEffectType costType = SupportCostEffectType.NextCardFree;
    [Min(1)] public int freeCardCount = 1;
    [Range(0f, 100f)] public float hpCostPercent = 10f;
    [Min(0)] public int costGainAmount = 1;
    [Min(1)] public int discardHandCount = 1;
    public bool allowCostOverMax = false;
    public bool hpPaymentCanDefeat = false;

    public SupportHpEffectType hpType = SupportHpEffectType.InstantChange;
    public int hpAmount = -1;
    [Min(1)] public int hpTurns = 1;

    public SupportStatEffectType statType = SupportStatEffectType.AttackChange;
    public int statAmount = 1;
    [Min(1)] public int statTurns = 1;

    public List<SupportTrapPlacement> trapPlacements = new List<SupportTrapPlacement>
    {
        new SupportTrapPlacement()
    };
    [Min(0)] public int trapDamage = 1;
    [Min(1)] public int trapDurationTurns = 1;
    public bool triggerImmediatelyIfOccupied = true;
    public bool removeAfterTrigger = true;

    [Min(1)] public int guardTurns = 1;
}

[CreateAssetMenu(menuName = "Cards/Support Card", fileName = "SupportCard")]
public class SupportCardSO : BaseCardSO
{
    [Header("Support Effects")]
    public List<SupportEffect> effects = new List<SupportEffect>
    {
        new SupportEffect()
    };

#if UNITY_EDITOR
    private void OnValidate()
    {
        type = CardType.Support;

        if (effects == null) return;
        foreach (SupportEffect effect in effects)
        {
            if (effect == null || effect.trapPlacements == null) continue;
            foreach (SupportTrapPlacement placement in effect.trapPlacements)
            {
                if (placement == null) continue;
                placement.gridMask?.EnsureSize();
                if (placement.trapPrefabScale < 0f) placement.trapPrefabScale = 0f;
            }
        }
    }
#endif
}
