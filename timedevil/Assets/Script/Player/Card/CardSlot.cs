using UnityEngine;
using UnityEngine.UI;

public class CardSlot : MonoBehaviour
{
    public string cardId;
    public Image image;
    public CardTemplateView templateView;
    public BaseCardSO Card { get; private set; }

    public void Setup(string id, BaseCardSO card)
    {
        cardId = id;
        Card = card;

        if (!image) image = GetComponent<Image>();

        if (!templateView)
            templateView = GetComponentInChildren<CardTemplateView>(true);
        if (!templateView)
            templateView = gameObject.AddComponent<CardTemplateView>();

        if (templateView)
        {
            templateView.Bind(card);
            if (image) image.raycastTarget = true;
            return;
        }

        if (!image) image = gameObject.AddComponent<Image>();
        image.sprite = null;
        image.enabled = false;
    }

    public void Clear()
    {
        Card = null;
        if (templateView) templateView.Clear();
        if (image)
        {
            image.sprite = null;
            image.enabled = false;
        }
    }
}
