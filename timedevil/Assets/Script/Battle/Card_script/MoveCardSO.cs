// MoveCardSO.cs
using UnityEngine;

public enum MoveMode { UpMove, AntiMove }
public enum Dir4 { Up, Down, Left, Right }

[CreateAssetMenu(menuName = "Cards/Move Card", fileName = "MoveCard")]
public class MoveCardSO : BaseCardSO
{
    public MoveMode moveMode = MoveMode.UpMove;
    public Dir4 where = Dir4.Up;
    public int amount = 1;      // ¸î Ä­
}
