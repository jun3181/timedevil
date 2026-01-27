// Assets/Script/Scene/SceneFader.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class SceneFader : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Scene Start Policy (씬마다 고정)")]
    [Tooltip("이 씬에 들어올 때 자동으로 페이드 인(검정->투명) 할지")]
    [SerializeField] private bool fadeInOnSceneStart = true;

    [Tooltip("씬 시작 시 알파 값(보통 1). fadeInOnSceneStart가 true일 때 적용")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 1f;

    [SerializeField] private float fadeInDuration = 1f;

    [Header("Optional: FadeOut for scene change")]
    [Tooltip("이 스크립트로 씬 전환 전 FadeOut을 하고 싶으면 사용")]
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true;

    // 예전 코드 호환: 페이드 인 완료 알림
    public static event Action OnFadeInComplete;

    private Coroutine _running;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

        // 씬 전용이므로 DontDestroyOnLoad / 싱글톤 / AutoLoad 제거
        // 시작 상태 세팅
        if (fadeInOnSceneStart)
            SetImmediate(startAlpha);
        else
            SetImmediate(0f);

        canvasGroup.interactable = false;
    }

    private void Start()
    {
        if (fadeInOnSceneStart)
        {
            StartCoroutine(FadeTo(0f, fadeInDuration));
        }
    }

    public void SetImmediate(float alpha)
    {
        if (!canvasGroup) return;

        canvasGroup.alpha = Mathf.Clamp01(alpha);
        canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.0001f;
        canvasGroup.interactable = false;
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (!canvasGroup) yield break;

        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _running = StartCoroutine(CoFade(targetAlpha, Mathf.Max(0f, duration)));
        yield return _running;
    }

    private IEnumerator CoFade(float targetAlpha, float duration)
    {
        targetAlpha = Mathf.Clamp01(targetAlpha);

        float start = canvasGroup.alpha;
        float t = 0f;

        // 페이드 중 입력 차단
        canvasGroup.blocksRaycasts = true;

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

        // 완전 투명해지면 입력 차단 해제 + 이벤트
        if (Mathf.Approximately(canvasGroup.alpha, 0f))
        {
            canvasGroup.blocksRaycasts = false;
            OnFadeInComplete?.Invoke();
        }

        _running = null;
    }

    // -------------------------
    // Optional: 씬 전환용(현재 씬에서만 사용)
    // -------------------------
    public void LoadSceneWithFadeOut(string sceneName)
    {
        StartCoroutine(CoFadeOutAndLoad(sceneName));
    }

    private IEnumerator CoFadeOutAndLoad(string sceneName)
    {
        yield return FadeTo(1f, fadeOutDuration);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
