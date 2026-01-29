// Assets/Script/Scene/VisitEffect/SceneVisitEffect_Fade.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class SceneVisitEffect_Fade : SceneVisitEffectBase
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup group;

    [Header("Enter (Scene Start)")]
    [SerializeField] private float enterFromAlpha = 1f;
    [SerializeField] private float enterToAlpha = 0f;
    [Min(0f)][SerializeField] private float enterDuration = 0.35f;

    [Header("Exit (Before Load Next Scene)")]
    [SerializeField] private float exitToAlpha = 1f;
    [Min(0f)][SerializeField] private float exitDuration = 0.35f;

    [Header("Input Block")]
    [SerializeField] private bool blockRaycastsWhileVisible = true;

    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();

        // 기본: 화면 정상 상태(투명)로 시작시키고 싶으면
        // Runner에서 Enter 전에 enterFromAlpha로 세팅하니까 여기서는 건드리지 않아도 됨.
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public override IEnumerator PlayEnter()
    {
        if (!group) yield break;

        // 시작 알파 세팅 (보통 1)
        group.alpha = Mathf.Clamp01(enterFromAlpha);
        ApplyBlockRaycasts();

        yield return CoLerpAlpha(enterToAlpha, enterDuration);

        // 완전 투명해지면 입력 막을 필요 없음
        ApplyBlockRaycasts();
    }

    public override IEnumerator PlayExit()
    {
        if (!group) yield break;

        // 나갈 때는 화면을 덮어야 하니까 raycast 막기
        ApplyBlockRaycasts(forceBlock: true);

        yield return CoLerpAlpha(exitToAlpha, exitDuration);

        // exit는 보통 1f에서 끝나고 바로 씬 로드하니까 이후는 의미 없음
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

    private IEnumerator CoLerpAlpha(float target, float duration)
    {
        target = Mathf.Clamp01(target);
        float start = group.alpha;

        if (duration <= 0f)
        {
            group.alpha = target;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime;
            float a = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(start, target, a);
            yield return null;
        }

        group.alpha = target;
    }
}
