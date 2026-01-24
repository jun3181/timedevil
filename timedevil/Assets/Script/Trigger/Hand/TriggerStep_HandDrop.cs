// Assets/Script/Trigger/Steps/TriggerStep_HandDrop.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_HandDrop : MonoBehaviour, ITriggerStep
{
    [Header("Hand")]
    [SerializeField] private GameObject handObject;   // 손 오브젝트(비활성 시작 권장)
    [SerializeField] private Transform hand;          // 비워두면 handObject.transform 사용

    [Header("Move")]
    [Tooltip("Y로 얼마나 내려갈지(월드 기준). 예: 3이면 아래로 3 내려감")]
    [SerializeField] private float dropDistanceY = 3f;

    [Header("Timing")]
    [Min(0.01f)][SerializeField] private float dropDuration = 0.12f;
    [Min(0f)][SerializeField] private float holdSeconds = 0.35f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Easing")]
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Options")]
    [SerializeField] private bool forceDeactivateThenActivate = true;
    [SerializeField] private bool deactivateOnEnd = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    public IEnumerator Execute(TriggerContext ctx)
    {
        if (!handObject && !hand)
        {
            Debug.LogWarning("[TriggerStep_HandDrop] handObject/hand가 비어있습니다.");
            yield break;
        }

        if (!hand && handObject) hand = handObject.transform;

        // 시작 위치 = 씬에 배치된 hand의 현재 위치
        // (비활성 상태여도 transform.position 읽히니까 여기서 잡아두면 됨)
        Vector3 from = hand.position;
        Vector3 to = from + Vector3.down * Mathf.Abs(dropDistanceY);

        // 1) 비활성 -> 활성
        if (handObject)
        {
            if (forceDeactivateThenActivate)
            {
                handObject.SetActive(false);
                handObject.SetActive(true);
            }
            else
            {
                if (!handObject.activeSelf) handObject.SetActive(true);
            }
        }

        // 시작 위치 스냅(혹시 이전 실행에서 내려가있던 상태 방지)
        hand.position = from;

        if (debugLog) Debug.Log($"[TriggerStep_HandDrop] {hand.name} from={from} to={to}");

        // 2) 빠르게 내려감
        float t = 0f;
        while (t < dropDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float u = Mathf.Clamp01(t / dropDuration);
            float k = (ease != null) ? ease.Evaluate(u) : u;

            hand.position = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        hand.position = to;

        // 3) 유지 후 비활성화
        if (holdSeconds > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(holdSeconds);
            else yield return new WaitForSeconds(holdSeconds);
        }

        if (deactivateOnEnd && handObject)
            handObject.SetActive(false);
    }
}
