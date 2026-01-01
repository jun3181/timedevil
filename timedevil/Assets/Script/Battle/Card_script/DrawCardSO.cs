// DrawCardSO.cs
using UnityEngine;

public enum DrawMode { UpDraw, AntiDraw }

[CreateAssetMenu(menuName = "Cards/Draw Card", fileName = "DrawCard")]
public class DrawCardSO : BaseCardSO
{
    public DrawMode drawMode = DrawMode.UpDraw;
    public int amount = 1;      // UpDraw: 내가 드로우할 장수 / AntiDraw: 상대 버릴 장수
}
