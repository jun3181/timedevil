using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardTemplateView : MonoBehaviour
{
    [Header("Text Slots")]
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text effectText;

    [Header("Image Slots")]
    [SerializeField] private Image mainArtworkImage;
    [SerializeField] private Image arrowImage;

    [Header("Fallback")]
    [SerializeField] private Image fullCardImage;
    [SerializeField] private bool hideEmptyImages = true;

    public void Bind(BaseCardSO card, Sprite fallbackFullCardSprite = null)
    {
        if (card == null)
        {
            Clear();
            SetImage(fullCardImage, fallbackFullCardSprite);
            return;
        }

        if (costText) costText.text = card.cost.ToString();
        if (nameText) nameText.text = string.IsNullOrWhiteSpace(card.displayName) ? card.id : card.displayName;
        if (effectText) effectText.text = card.EffectText ?? string.Empty;

        bool hasArtworkSlot = mainArtworkImage && card.mainArtwork;
        SetImage(mainArtworkImage, card.mainArtwork);
        SetImage(arrowImage, card.arrowArtwork);
        SetImage(fullCardImage, hasArtworkSlot ? null : fallbackFullCardSprite);
    }

    public void Clear()
    {
        if (costText) costText.text = string.Empty;
        if (nameText) nameText.text = string.Empty;
        if (effectText) effectText.text = string.Empty;

        SetImage(mainArtworkImage, null);
        SetImage(arrowImage, null);
        SetImage(fullCardImage, null);
    }

    private void SetImage(Image image, Sprite sprite)
    {
        if (!image) return;

        image.sprite = sprite;
        if (hideEmptyImages) image.enabled = sprite != null;
        image.preserveAspect = true;
    }
}
