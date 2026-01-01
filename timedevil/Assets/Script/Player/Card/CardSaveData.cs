using System;
using System.Collections.Generic;

[Serializable]
public class CardSaveData
{
    // 보유 카드 ID들 (예: "Card1", "Card2")
    public List<string> owned = new List<string>();

    // 현재 덱 편성 (순서 유지)
    public List<string> deck = new List<string>();
}
