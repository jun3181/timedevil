using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class DarkOverlay : MonoBehaviour
{
    public static DarkOverlay Instance { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private bool dontDestroyOnLoad = true;

    private Coroutine co;

    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!group) group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    public float Alpha => group ? group.alpha : 0f;

    public void SetAlpha(float target, float duration = 0f)
    {
        if (!group) return;

        target = Mathf.Clamp01(target);

        if (co != null) StopCoroutine(co);
        if (duration <= 0f)
        {
            group.alpha = target;
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
            t += Time.unscaledDeltaTime;              // 텔레포트/메뉴 등 TimeScale 영향 X
            float a = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(start, target, a);
            yield return null;
        }

        group.alpha = target;
        co = null;
    }
}
