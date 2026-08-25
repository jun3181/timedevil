using UnityEngine;

[System.Serializable]
public class TriggerRouteStage
{
    [Tooltip("이 스테이지에서 실행할 TriggerRouter Route Key입니다.")]
    public string routeKey = "Trigger1";

    [Min(0)]
    [Tooltip("0은 무제한 반복입니다. 1 이상이면 해당 횟수만큼 성공 실행 후 다음 스테이지로 넘어갑니다.")]
    public int maxCalls = 1;
}
