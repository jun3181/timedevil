using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerStep_Card : TriggerStepBase
{

    [SerializeField]
    [Header("DB")]
    [Tooltip("설정한 카드들이 유효한 지 검증용")]
    private CardDatabaseSO db;

    [SerializeField]
    [Header("Cards")]
    [Tooltip("트리거 작동시 지급될 카드들의 타입과 이름들")]
    private List<BaseCardSO> cards = new();
    
    void Awake() {
        HashSet<BaseCardSO> registered_cards = new();
        for(int i = 0; i < cards.Count; i++) {
            if(cards[i]==null || !db.GetById(cards[i].id)) {
                cards.RemoveAt(i);
                i--;
                continue;
            }
            registered_cards.Add(cards[i]);
        }

        cards = new List<BaseCardSO>(registered_cards);
    }

    public override IEnumerator Execute(TriggerContext ctx) {
        if(CardStateRuntime.Instance == null) yield break;
        foreach(var card in cards) {
            CardStateRuntime.Instance.AddOwned(card.id);
        }
        yield break;
    }
}
