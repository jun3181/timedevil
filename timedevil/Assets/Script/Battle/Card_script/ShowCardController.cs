using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShowCardController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image useImage;                 // Legacy fallback image holder.
    [SerializeField] private CardTemplateView cardTemplate;
    [SerializeField] private CardTemplateView templateSource;
    [SerializeField] private CardDatabaseSO cardDatabase;
    [SerializeField] private string resourcesFolder = "my_asset";
    [SerializeField] private Vector2 maxPreviewSize = new Vector2(130f, 200f);

    [Header("Fade")]
    [SerializeField] private float fadeIn = 0.25f;
    [SerializeField] private float fadeOut = 0.25f;

    private CanvasGroup cg;
    private RectTransform previewRect;

    private void EnsureCardDatabase()
    {
        if (cardDatabase) return;

        var orchestrator = FindObjectOfType<CardUseOrchestrator>(true);
        if (orchestrator && orchestrator.CardDatabase)
            cardDatabase = orchestrator.CardDatabase;
    }

    private BaseCardSO ResolveCard(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        EnsureCardDatabase();
        return cardDatabase ? cardDatabase.GetById(id) : null;
    }

    private Sprite ResolveLegacySprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

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
        if (id.StartsWith("SupportCard")) return "SupportCard";
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
        EnsureCardTemplate();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        HidePreview();
    }

    public IEnumerator PreviewById(string id, float totalSeconds = 3f)
    {
        EnsureCardDatabase();
        EnsureCardTemplate();

        BaseCardSO card = ResolveCard(id);
        Sprite legacySprite = card != null ? null : ResolveLegacySprite(id);
        if (!card && !legacySprite)
        {
            HidePreview();
            yield break;
        }

        BindPreview(card, legacySprite);

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
        HidePreview();
    }

    private void EnsureCardTemplate()
    {
        if (!cardTemplate && useImage)
            cardTemplate = useImage.GetComponent<CardTemplateView>();

        if (!cardTemplate)
            cardTemplate = GetComponentInChildren<CardTemplateView>(true);

        if (!cardTemplate)
        {
            if (!useImage)
                useImage = CreatePreviewImage();

            if (useImage)
                cardTemplate = useImage.gameObject.AddComponent<CardTemplateView>();
        }

        if (!templateSource)
            templateSource = FindTemplateSource();

        if (cardTemplate && templateSource)
            cardTemplate.CopyVisualSettingsFrom(templateSource);

        previewRect = cardTemplate != null
            ? cardTemplate.transform as RectTransform
            : useImage != null ? useImage.rectTransform : null;

        if (useImage)
        {
            useImage.sprite = null;
            useImage.color = new Color(1f, 1f, 1f, 0f);
            useImage.preserveAspect = false;
            useImage.raycastTarget = false;
        }
    }

    private Image CreatePreviewImage()
    {
        GameObject go = new GameObject("CardPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        return go.GetComponent<Image>();
    }

    private CardTemplateView FindTemplateSource()
    {
        HandUI hand = FindObjectOfType<HandUI>(true);
        if (hand && hand.CardTemplateSource)
            return hand.CardTemplateSource;

        EnemyHandUI enemyHand = FindObjectOfType<EnemyHandUI>(true);
        if (enemyHand && enemyHand.CardTemplateSource)
            return enemyHand.CardTemplateSource;

        CardTemplateView[] views = FindObjectsOfType<CardTemplateView>(true);
        for (int i = 0; i < views.Length; i++)
        {
            CardTemplateView view = views[i];
            if (view && view != cardTemplate)
                return view;
        }

        return null;
    }

    private void BindPreview(BaseCardSO card, Sprite legacySprite)
    {
        if (cardTemplate)
        {
            cardTemplate.gameObject.SetActive(true);
            cardTemplate.Bind(card, legacySprite);
            FitPreviewRect(legacySprite, card != null);
            return;
        }

        if (!useImage) return;

        useImage.sprite = legacySprite;
        useImage.preserveAspect = true;
        useImage.enabled = legacySprite != null;
        FitPreviewRect(legacySprite, false);
    }

    private void HidePreview()
    {
        if (cg) cg.alpha = 0f;

        if (cardTemplate)
        {
            cardTemplate.Clear();
            cardTemplate.gameObject.SetActive(false);
        }

        if (useImage)
        {
            useImage.sprite = null;
            useImage.enabled = cardTemplate == null;
        }
    }

    private void FitPreviewRect(Sprite sprite, bool useCardAspect)
    {
        if (!previewRect) return;

        Vector2 maxSize = maxPreviewSize;
        if (maxSize.x <= 0f || maxSize.y <= 0f) maxSize = previewRect.sizeDelta;
        if (maxSize.x <= 0f || maxSize.y <= 0f) return;

        float spriteAspect = useCardAspect
            ? 2f / 3f
            : sprite != null ? sprite.rect.width / Mathf.Max(1f, sprite.rect.height) : 2f / 3f;
        float boxAspect = maxSize.x / Mathf.Max(1f, maxSize.y);

        Vector2 size = maxSize;
        if (spriteAspect > boxAspect)
            size.y = maxSize.x / spriteAspect;
        else
            size.x = maxSize.y * spriteAspect;

        previewRect.sizeDelta = size;
    }
}
