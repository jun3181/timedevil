// SupportCardSO.cs
using UnityEngine;

public enum SupportAction { Debuff, Buff, AntiCost }
public enum StatKind { HP, ATK, DEF }
public enum SupportPositionTarget { Self, Foe, Player, Enemy, Any }

[CreateAssetMenu(menuName = "Cards/Support Card", fileName = "SupportCard")]
public class SupportCardSO : BaseCardSO
{
    [Header("Support")]
    public SupportAction action;

    [Header("Debuff (action==Debuff)")]
    public StatKind debuffStat;
    public int debuffAmount;
    public int debuffTurn;
    [HideInInspector]
    public int debuffTickDamage;

    [Header("Buff (action==Buff)")]
    public StatKind buffStat;
    public int buffAmount;
    public int buffTurn;

    [Header("AntiCost (action==AntiCost)")]
    public int antiCostAmount = 1;
    public int antiCostTurn = 1;

    [Header("Position Debug")]
    public bool enablePositionDebug;
    public SupportPositionTarget positionTarget = SupportPositionTarget.Any;
    [Tooltip("16 chars, row-major top to bottom. Example: 0001001001001000")]
    public string positionPattern16 = "0000000000000000";
    public bool positionDebugOneShot = true;
}
