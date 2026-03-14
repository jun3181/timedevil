// Assets/Script/UI/DarkOverlay.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class DarkOverlay : MonoBehaviour
{
    //  사용 방식 유지: DarkOverlay.Instance.SetAlpha(...)
    private static DarkOverlay _instance;
    public static DarkOverlay Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<DarkOverlay>(true);
            return _instance;
        }
        private set => _instance = value;
    }

    [SerializeField] private CanvasGroup group;

    [Header("Policy")]
    [Tooltip("true면 씬 전용. 씬이 바뀌면(살아있다면) 자동으로 파괴됩니다.")]
    [SerializeField] private bool sceneLocal = true;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine co;
    private string ownerSceneName;

    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;
    }

    private void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        ownerSceneName = gameObject.scene.name;

        //  기존 인스턴스가 남아있다면(특히 DontDestroy로 살아있던 경우) 제거
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this) Instance = null;

        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //  씬 로컬 정책: 내 씬이 아닌 씬이 로드됐는데 내가 살아있다?
        // = DontDestroy로 살아남았던 케이스 -> 즉시 제거
        if (sceneLocal && scene.name != ownerSceneName)
        {
            Destroy(gameObject);
        }
    }

    public float Alpha => group ? group.alpha : 0f;

    public void SetImmediate(float target)
    {
        if (!group) return;
        group.alpha = Mathf.Clamp01(target);
    }

    public void SetAlpha(float target, float duration = 0f)
    {
        if (!group) return;

        target = Mathf.Clamp01(target);

        if (co != null) StopCoroutine(co);

        if (duration <= 0f)
        {
            group.alpha = target;
            co = null;
            return;
        }

        co = StartCoroutine(CoLerp(target, duration));
    }

    private IEnumerator CoLerp(float target, float duration)
    {
        float start = group.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(start, target, a);
            yield return null;
        }

        group.alpha = target;
        co = null;
    }
}
