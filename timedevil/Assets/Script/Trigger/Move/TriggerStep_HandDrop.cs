// Assets/Script/Trigger/Steps/TriggerStep_HandDrop.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerStep_HandDrop : TriggerStepBase
{
    [Header("Hand")]
    [SerializeField] private GameObject handObject;   // 손 오브젝트(비활성 시작 권장)

    [Header("Move (X/Y)")]
    [Tooltip("X로 얼마나 이동할지(월드 기준). +면 오른쪽, -면 왼쪽")]
    [SerializeField] private float moveDistanceX = 0f;

    [Tooltip("Y로 얼마나 이동할지(월드 기준). +면 위, -면 아래")]
    [SerializeField] private float moveDistanceY = -3f;

    [Header("Timing")]
    [Min(0.01f)][SerializeField] private float dropDuration = 0.12f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Easing")]
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Options")]
    [SerializeField] private bool forceDeactivateThenActivate = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private bool _cachedStart;
    private Vector3 _startPos;

    private void CacheStartIfNeeded()
    {
        if (_cachedStart) return;
        if (!handObject) return;

        _startPos = handObject.transform.position;   // 씬에 배치된 시작 위치
        _cachedStart = true;
    }

    public override IEnumerator Execute(TriggerContext ctx)
    {
        if (!handObject)
        {
            Debug.LogWarning("[TriggerStep_HandDrop] handObject가 비어있습니다.");
            yield break;
        }

        CacheStartIfNeeded();

        var tr = handObject.transform;

        Vector3 from = _startPos;
        Vector3 to = from + new Vector3(moveDistanceX, moveDistanceY, 0f); //  부호 그대로

        // 1) 비활성 -> 활성
        if (forceDeactivateThenActivate)
        {
            handObject.SetActive(false);
            handObject.SetActive(true);
        }
        else
        {
            if (!handObject.activeSelf) handObject.SetActive(true);
        }

        // 시작 위치 스냅
        tr.position = from;

        if (debugLog) Debug.Log($"[TriggerStep_HandDrop] from={from} to={to}");

        // 2) 이동
        float t = 0f;
        while (true)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float u = t / dropDuration;

            if (u >= 1f)
            {
                tr.position = to;
                break;
            }

            float k = (ease != null) ? ease.Evaluate(u) : u;
            tr.position = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
    }
}
