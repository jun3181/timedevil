// BaseCardSO.cs
using UnityEngine;

public abstract class BaseCardSO : ScriptableObject
{
    [Header("ID & Meta")]
    public string id;           // Hand에 들어있는 문자열 id와 동일해야 함
    public string displayName;  // 표시용 이름
    public CardType type;
    [TextArea] public string display; // 설명문
    [TextArea] public string explanation;  // ✅ 발동 대사(우선 노출)


    [Header("Cost & Rating")]
    public int cost = 1;
    [Range(0, 10)] public int positive = 0;
    [Range(0, 10)] public int negative = 0;
}
