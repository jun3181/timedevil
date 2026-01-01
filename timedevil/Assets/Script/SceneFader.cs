using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class SceneFader : MonoBehaviour
{
    public static SceneFader instance;

    public CanvasGroup canvasGroup;
    public float fadeDuration = 1f;

    // ★ 추가: 페이드 인 완료 알림 (CameraFollowRebinder가 구독)
    public static event Action OnFadeInComplete;

    private void Awake()
    {
        // 싱글톤 생성
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 씬 로드될 때마다 자동 페이드인 실행
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (canvasGroup == null)
                Debug.LogWarning("[SceneFader] CanvasGroup이 비어 있습니다. 프리팹에서 연결하세요.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    // 첫 씬 로드 전 SceneFader 프리팹 항상 보장
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoLoadFaderPrefab()
    {
        if (FindObjectOfType<SceneFader>() != null) return;

        SceneFader prefab = Resources.Load<SceneFader>("SceneFader");
        if (prefab == null)
        {
            Debug.LogError("Resources/SceneFader.prefab 을 찾을 수 없음");
            return;
        }

        Instantiate(prefab);
    }

    //-------------------------------------------------------------------
    // 씬 로드 후 자동 페이드 인
    //-------------------------------------------------------------------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != null)
            instance.StartCoroutine(instance.Fade(0f)); // 투명으로
    }

    //-------------------------------------------------------------------
    // 페이드 함수
    //-------------------------------------------------------------------
    public IEnumerator Fade(float target)
    {
        if (canvasGroup == null) yield break;

        float start = canvasGroup.alpha;
        float time = 0f;

        // 페이드 중에는 입력 차단
        canvasGroup.blocksRaycasts = true;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;

        // 완전 투명(페이드 인) 완료 시에는 입력 허용 + 이벤트 발행
        if (Mathf.Approximately(target, 0f))
        {
            canvasGroup.blocksRaycasts = false;
            OnFadeInComplete?.Invoke();
        }
        // target == 1f(페이드 아웃)일 때는 씬 로드 직전 상태이므로 차단 유지
    }

    //-------------------------------------------------------------------
    // 씬 전환
    //-------------------------------------------------------------------
    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        // 어둡게
        yield return StartCoroutine(Fade(1f));

        // 씬 로드 (로드 후 자동 페이드 인은 OnSceneLoaded에서 처리)
        SceneManager.LoadScene(sceneName);
    }
}
