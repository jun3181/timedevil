using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerStep_Card : TriggerStepBase
{
    [System.Serializable]
    public enum CardType {
        Attack, Move, Draw
    }

    [System.Serializable]
    public struct CardInfo {
        public CardType type;
        public uint index;
    }

    [SerializeField]
    [Header("ID")]
    [Tooltip("트리거 작동시 지급될 카드들의 타입과 이름들")]
    private List<CardInfo> cardInfos = new();

    private List<string> cardIds = new();

    void Awake() {
        foreach(CardInfo info in cardInfos) {
            if(info.index == 0) continue;
            cardIds.Add(info.type.ToString() + "Card" + info.index.ToString());
        }
    }

    public override IEnumerator Execute(TriggerContext ctx) {
        if(CardStateRuntime.Instance == null) yield break;
        foreach(string id in cardIds) {
            CardStateRuntime.Instance.AddOwned(id);
        }
        yield break;
    }
}
