using System.Collections;
using UnityEngine;

public class TriggerRouter : MonoBehaviour
{
    [Header("Input Lock (GameManager.isAction 사용)")]
    [SerializeField] private bool lockInputWhileRunning = true;

    [Header("Debug Sequence (테스트용 대기)")]
    [SerializeField] private float testDuration = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public bool IsRunning { get; private set; }

    public void StartSequence(GameObject instigator)
    {
        if (IsRunning)
        {
            if (debugLog) Debug.Log("[TriggerRouter] Already running -> ignore");
            return;
        }

        StartCoroutine(CoSequence(instigator));
    }

    private IEnumerator CoSequence(GameObject instigator)
    {
        IsRunning = true;

        if (debugLog)
        {
            string who = instigator ? instigator.name : "(null)";
            Debug.Log($"[TriggerRouter] START by={who} router='{name}'");
        }

        if (lockInputWhileRunning && GameManager.Instance != null)
        {
            GameManager.Instance.isAction = true;
            if (debugLog) Debug.Log("[TriggerRouter] InputLock ON (GameManager.isAction=true)");
        }

        // ====== 여기서부터 나중에 "Step"으로 바뀔 자리 ======
        // 지금은 테스트로 시간만 끌고 로그 찍음
        float t = 0f;
        while (t < testDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }
        // ===============================================

        if (lockInputWhileRunning && GameManager.Instance != null)
        {
            GameManager.Instance.isAction = false;
            if (debugLog) Debug.Log("[TriggerRouter] InputLock OFF (GameManager.isAction=false)");
        }

        if (debugLog) Debug.Log($"[TriggerRouter] END router='{name}'");

        IsRunning = false;
    }
}
