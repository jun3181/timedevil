using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShowCardController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image useImage;                 // ShowCard 안의 이미지
    [SerializeField] private string resourcesFolder = "my_asset";

    [Header("Fade")]
    [SerializeField] private float fadeIn = 0.25f;
    [SerializeField] private float fadeOut = 0.25f;

    private CanvasGroup cg;

    void Reset()
    {
        if (!useImage) useImage = GetComponentInChildren<Image>(true);
    }

    void Awake()
    {
        if (!useImage) useImage = GetComponentInChildren<Image>(true);
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        if (useImage) useImage.enabled = false;
    }

    public IEnumerator PreviewById(string id, float totalSeconds = 3f)
    {
        if (!useImage) yield break;

        Sprite sp = string.IsNullOrEmpty(id) ? null : Resources.Load<Sprite>($"{resourcesFolder}/{id}");
        useImage.sprite = sp;
        useImage.enabled = sp != null;

        float fin = Mathf.Max(0f, fadeIn);
        float fout = Mathf.Max(0f, fadeOut);
        float body = Mathf.Max(0f, totalSeconds - fin - fout);

        // fade in
        if (fin > 0f)
        {
            float t = 0f;
            while (t < fin) { t += Time.unscaledDeltaTime; cg.alpha = Mathf.Lerp(0f, 1f, t / fin); yield return null; }
        }
        else cg.alpha = 1f;

        // hold
        if (body > 0f)
        {
            float t = 0f;
            while (t < body) { t += Time.unscaledDeltaTime; yield return null; }
        }

        // fade out
        if (fout > 0f)
        {
            float t = 0f;
            while (t < fout) { t += Time.unscaledDeltaTime; cg.alpha = Mathf.Lerp(1f, 0f, t / fout); yield return null; }
        }
        else cg.alpha = 0f;

        // cleanup
        cg.alpha = 0f;
        useImage.enabled = false;
        useImage.sprite = null;
    }
}
