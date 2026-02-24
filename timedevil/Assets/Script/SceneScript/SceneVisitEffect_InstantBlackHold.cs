// Assets/Script/Scene/VisitEffect/SceneVisitEffect_InstantBlackHold.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class SceneVisitEffect_InstantBlackHold : SceneVisitEffectBase
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup group;

    [Header("Enter (Scene Start)")]
    [Tooltip("이 Effect를 가진 씬에서 시작할 때는 기본적으로 화면 정상(투명) 상태로 둠")]
    [SerializeField] private float enterAlpha = 0f;

    [Header("Exit (Before Load Next Scene)")]
    [Tooltip("Exit 시작하자마자 바로 이 알파로 세팅 (보통 1 = 완전 검정)")]
    [Range(0f, 1f)]
    [SerializeField] private float exitAlpha = 1f;

    [Tooltip("검정 유지 시간(초) 최소")]
    [Min(0f)]
    [SerializeField] private float holdMinSeconds = 2f;

    [Tooltip("검정 유지 시간(초) 최대")]
    [Min(0f)]
    [SerializeField] private float holdMaxSeconds = 4f;

    [Header("Input Block")]
    [Tooltip("검정이 보이는 동안 입력(레이캐스트)을 막을지")]
    [SerializeField] private bool blockRaycastsWhileVisible = true;

    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public override IEnumerator PlayEnter()
    {
        if (!group) yield break;

        group.alpha = Mathf.Clamp01(enterAlpha);
        ApplyBlockRaycasts();
        yield break;
    }

    public override IEnumerator PlayExit()
    {
        if (!group) yield break;

        // 1) 즉시 검정
        group.alpha = Mathf.Clamp01(exitAlpha);
        ApplyBlockRaycasts(forceBlock: true);

        // 2) 2~4초(설정 범위) 유지
        float min = Mathf.Max(0f, holdMinSeconds);
        float max = Mathf.Max(min, holdMaxSeconds);
        float hold = Random.Range(min, max);

        if (hold > 0f)
            yield return WaitSeconds(hold);
    }

    private void ApplyBlockRaycasts(bool? forceBlock = null)
    {
        if (!group) return;

        bool block;
        if (forceBlock.HasValue) block = forceBlock.Value;
        else
        {
            if (!blockRaycastsWhileVisible) block = false;
            else block = group.alpha > 0.0001f;
        }

        group.blocksRaycasts = block;
        group.interactable = false;
    }

    private IEnumerator WaitSeconds(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += DeltaTime; // SceneVisitEffectBase의 시간 정책(보통 unscaled) 따름
            yield return null;
        }
    }
}
