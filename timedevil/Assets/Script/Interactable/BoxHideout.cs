using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class BoxHideout : MonoBehaviour, IInteractable
{
    private static PlayerMove playerMove = null;

    [Header("상자가 열려있을 때 | 닫혀있을 때")]
    [SerializeField] private Sprite openedBoxSprite;
    [Tooltip("지정하지 않을 경우 현재 적용된 스프라이트 사용")]
    [SerializeField] private Sprite closedBoxSprite;

    [Header("상자를 여는 소리 | 닫는 소리")]
    [SerializeField] private AudioClip openingBoxSound;
    [SerializeField] private AudioClip closingBoxSound;

    [SerializeField]
    [Header("디버그 메시지")]
    private bool debuged = false;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    void Awake()
    {
        if(openedBoxSprite==null) {
            if(debuged) Debug.LogError($"{gameObject.name}.BoxHideout.openedSprite는 반드시 지정되어야합니다.");
            enabled = false;
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if(closedBoxSprite==null) {
            if(spriteRenderer.sprite==null) {
                if(debuged) Debug.LogError($"{gameObject.name}.BoxHideout.closedBoxSprite와 {gameObject.name}.SpriteRenderer.sprite 둘 중 하나는 반드시 지정되어야합니다.");
                enabled = false;
                return;
            }
            closedBoxSprite = spriteRenderer.sprite;
        }
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable() {
        if(playerMove == null)
            playerMove = GameObject.FindWithTag("Player").GetComponent<PlayerMove>();
    }

    public void Interact() {
        if(!enabled) return;
    }
}
