// SupportCardSO.cs
using UnityEngine;

public enum SupportAction { Debuff, Buff }
public enum StatKind { HP, ATK, DEF }

[CreateAssetMenu(menuName = "Cards/Support Card", fileName = "SupportCard")]
public class SupportCardSO : BaseCardSO
{
    [Header("Support")]
    public SupportAction action;

    [Header("Debuff (action==Debuff)")]
    public StatKind debuffStat;
    public int debuffAmount;       // HP면 데미지/지속틱, ATK/DEF면 감소량
    public int debuffTurn;         // 상대 턴 n번 지속
    public int debuffTickDamage;   // antiHP일 때 상대턴마다 줄 데미지

    [Header("Buff (action==Buff)")]
    public StatKind buffStat;
    public int buffAmount;         // HP면 즉시 회복량, ATK/DEF면 증가량
    public int buffTurn;           // 내 턴 n번 지속
}
