using UnityEngine;
using UnityEngine.UI;

public class CardSlot : MonoBehaviour
{
    public string cardId;
    public Image image;

    public void Setup(string id, Sprite sprite)
    {
        cardId = id;
        if (!image) image = GetComponent<Image>();
        image.sprite = sprite;
        image.enabled = (sprite != null);
    }
}
