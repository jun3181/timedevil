using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndingDeterminer: MonoBehaviour
{
    public static EndingDeterminer Instance { get; private set; }

    public static EndingState State {
        get
        {
            return state;
        }
    }

    private static EndingState state = EndingState.None;

    [SerializeField]
    [Header("카드 데이터배이스")]
    private CardDatabaseSO db;

    [SerializeField]
    [Header("7개의 주요 카드")]
    private List<BaseCardSO> primaryCards = new();

    void Awake() {
        bool killSwitch = false;
        if(Instance) {
            Debug.LogWarning("이미 EndingDeterminer가 생성되었습니다.");
            killSwitch = true;
        } else if(!PlayerDataRuntime.Instance) {
            Debug.LogError("PlayerDataRuntime의 인스턴스가 존재하지 않습니다.");
            killSwitch = true;
        } else if(!CardStateRuntime.Instance) {
            Debug.LogError("CardStateRuntime의 인스턴스가 존재하지 않습니다.");
            killSwitch = true;
        } else if(primaryCards.Count!=7 || primaryCards.Contains(null)) {
            Debug.LogError("설정된 주요 카드의 개수가 7개가 아닙니다.");
            killSwitch = true;
        }

        if(killSwitch) {
            Destroy(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public EndingState Determine() {
        int positiveEmotion = PlayerDataRuntime.Instance.Data.emotionPositive;
        int negativeEmotion = PlayerDataRuntime.Instance.Data.emotionNegative;

        if(negativeEmotion >= positiveEmotion) {
            state = EndingState.Bad;
            return state;
        }

        foreach(BaseCardSO primaryCard in primaryCards) {
            if(!CardStateRuntime.Instance.Data.owned.Contains(primaryCard.id)) {
                state = EndingState.Normal;
                return state;
            }
        }

        state = EndingState.Good;
        return state;
    }
}
