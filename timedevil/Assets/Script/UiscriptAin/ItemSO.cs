using UnityEngine;

public enum ItemType
{
    Consumable,
    Key,
    Etc
}

[CreateAssetMenu(menuName = "Items/Item", fileName = "NewItem")]
public class ItemSO : ScriptableObject
{
    [Header("ID & Meta")]
    public string id;            // JSON에서 참조할 고유 ID (예: "potion")
    public string displayName;   // 인벤토리에서 보이는 이름
    public ItemType type;

    [Header("설명 & 비주얼")]
    [TextArea] public string description; // 설명
    public Sprite icon;                   // 아이콘

    [Header("기본값 (선택 사항)")]
    public int defaultQuantity = 0;       // 새 게임 시작 시 기본 수량 (안 써도 됨)
}
