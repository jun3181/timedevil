using UnityEngine;

public class BedStateController : MonoBehaviour, IInteractable
{
    [Header("상태 오브젝트 연결")]
    public GameObject normalStateObject;    // '보통 침대' 오브젝트
    public GameObject lyingDownStateObject; // '누워있는 침대' 오브젝트

    private bool isLyingDown = false;
    private SpriteRenderer playerSprite;

    public void Interact()
    {
        if (playerSprite == null)
        {
            PlayerAction player = FindObjectOfType<PlayerAction>();
            if (player != null)
                playerSprite = player.GetComponent<SpriteRenderer>();
            else return;
        }

        if (!isLyingDown)
        {
            playerSprite.enabled = false;
            normalStateObject.SetActive(false);
            lyingDownStateObject.SetActive(true);
            isLyingDown = true;
        }
        else
        {
            playerSprite.enabled = true;
            normalStateObject.SetActive(true);
            lyingDownStateObject.SetActive(false);
            isLyingDown = false;
        }
    }
}