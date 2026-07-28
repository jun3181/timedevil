using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerStep_EndingDeterminer : TriggerStepBase
{
    public static EndingState State
    {
        get
        {
            return state;
        }
    }

    private static EndingState state = EndingState.None;

    private static TriggerStep_EndingDeterminer instance;

    [SerializeField]
    [Header("7개의 주요 카드")]
    private List<BaseCardSO> primaryCards = new();

    void Start() {
        bool killSwitch = false;
        if(instance || state!=EndingState.None) {
            Debug.LogWarning("이미 EndingDeterminer가 생성되었거나 실행되었습니다.");
            killSwitch = true;
        } else if(!PlayerDataRuntime.Instance) {
            Debug.LogError("PlayerDataRuntime의 인스턴스가 존재하지 않습니다.");
            killSwitch = true;
        } else if(!CardStateRuntime.Instance) {
            Debug.LogError("CardStateRuntime의 인스턴스가 존재하지 않습니다.");
            killSwitch = true;
        } else if(primaryCards.Count != 7 || primaryCards.Contains(null)) {
            Debug.LogError("설정된 주요 카드의 개수가 7개가 아닙니다.");
            killSwitch = true;
        }

        if(killSwitch) {
            Destroy(gameObject);
        } else {
            instance = this;
        }
    }

    void OnDestroy() {
        instance = null;
    }

    public override IEnumerator Execute(TriggerContext ctx) {
        int positiveEmotion = PlayerDataRuntime.Instance.Data.emotionPositive;
        int negativeEmotion = PlayerDataRuntime.Instance.Data.emotionNegative;

        if(negativeEmotion >= positiveEmotion) {
            state = EndingState.Bad;
            Debug.Log(state);
            yield break;
        }

        foreach(BaseCardSO primaryCard in primaryCards) {
            if(!CardStateRuntime.Instance.Data.owned.Contains(primaryCard.id)) {
                state = EndingState.Normal;
                Debug.Log(state);
                yield break;
            }
        }

        state = EndingState.Good;
        Debug.Log(state);
        yield break;
    }
}
