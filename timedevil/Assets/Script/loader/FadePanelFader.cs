using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class FadePanelFader : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Default")]
    [SerializeField] private float defaultDuration = 0.25f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine running;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

        // 시작은 투명(=화면 정상)
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void SetImmediate(float alpha)
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = Mathf.Clamp01(alpha);
        canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.0001f;
        canvasGroup.interactable = false;
    }

    public IEnumerator FadeTo(float targetAlpha, float duration = -1f)
    {
        if (!canvasGroup) yield break;

        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        float dur = (duration < 0f) ? defaultDuration : Mathf.Max(0f, duration);
        running = StartCoroutine(CoFade(targetAlpha, dur));
        yield return running;
    }

    private IEnumerator CoFade(float targetAlpha, float duration)
    {
        targetAlpha = Mathf.Clamp01(targetAlpha);

        float start = canvasGroup.alpha;
        float t = 0f;

        // 페이드 중에는 클릭 등 막기
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            while (t < duration)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }

        // 완전히 투명해지면 입력 차단 해제
        if (Mathf.Approximately(canvasGroup.alpha, 0f))
            canvasGroup.blocksRaycasts = false;

        running = null;
    }
}
