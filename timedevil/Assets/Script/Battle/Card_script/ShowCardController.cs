using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShowCardController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image useImage;                 // ShowCard 안의 이미지
    [SerializeField] private CardDatabaseSO cardDatabase;
    [SerializeField] private string resourcesFolder = "my_asset";
    [SerializeField] private Vector2 maxPreviewSize = new Vector2(130f, 200f);

    [Header("Fade")]
    [SerializeField] private float fadeIn = 0.25f;
    [SerializeField] private float fadeOut = 0.25f;

    private CanvasGroup cg;
    private RectTransform imageRect;

    private void EnsureCardDatabase()
    {
        if (cardDatabase) return;

        var orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (orchestrator && orchestrator.CardDatabase)
            cardDatabase = orchestrator.CardDatabase;
    }

    private Sprite ResolveSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        EnsureCardDatabase();
        BaseCardSO card = cardDatabase ? cardDatabase.GetById(id) : null;
        if (card && card.mainArtwork) return card.mainArtwork;

        Sprite sprite = Resources.Load<Sprite>($"{resourcesFolder}/{id}");
        if (sprite) return sprite;

        string typeFolder = GetCardTypeFolder(id);
        return !string.IsNullOrEmpty(typeFolder)
            ? Resources.Load<Sprite>($"{resourcesFolder}/{typeFolder}/{id}")
            : null;
    }

    private static string GetCardTypeFolder(string id)
    {
        if (id.StartsWith("AttackCard")) return "AttackCard";
        if (id.StartsWith("DrawCard")) return "DrawCard";
        if (id.StartsWith("MoveCard")) return "MoveCard";
        return null;
    }

    void Reset()
    {
        if (!useImage) useImage = GetComponentInChildren<Image>(true);
    }

    void Awake()
    {
        if (!useImage) useImage = GetComponentInChildren<Image>(true);
        EnsureCardDatabase();
        if (useImage) imageRect = useImage.rectTransform;
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        if (useImage)
        {
            useImage.preserveAspect = true;
            useImage.enabled = false;
        }
    }

    public IEnumerator PreviewById(string id, float totalSeconds = 3f)
    {
        if (!useImage) yield break;

        Sprite sp = ResolveSprite(id);
        useImage.sprite = sp;
        useImage.preserveAspect = true;
        useImage.enabled = sp != null;
        FitPreviewRect(sp);

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

    private void FitPreviewRect(Sprite sprite)
    {
        if (!imageRect || !sprite) return;

        Vector2 maxSize = maxPreviewSize;
        if (maxSize.x <= 0f || maxSize.y <= 0f)
            maxSize = imageRect.sizeDelta;
        if (maxSize.x <= 0f || maxSize.y <= 0f) return;

        float spriteAspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        float boxAspect = maxSize.x / Mathf.Max(1f, maxSize.y);

        Vector2 size = maxSize;
        if (spriteAspect > boxAspect)
            size.y = maxSize.x / spriteAspect;
        else
            size.x = maxSize.y * spriteAspect;

        imageRect.sizeDelta = size;
    }
}
