using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class BoxHideout : MonoBehaviour, IInteractable
{
    private static GameObject player = null;
    private static SpriteRenderer playerSpriteRenderer = null;
    private static Collider2D playerCollider2D = null;
    private static PlayerMove playerMove = null;

    [Header("상자가 열려있을 때 | 닫혀있을 때")]
    [SerializeField] private Sprite openedBoxSprite;
    [Tooltip("지정하지 않을 경우 현재 적용된 스프라이트 사용")]
    [SerializeField] private Sprite closedBoxSprite;

    [Header("상자를 여는 소리")]
    [SerializeField] private AudioClip openingBoxSound;

    [SerializeField]
    [Header("디버그 메시지")]
    private bool debuged = false;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private IEnumerator stealthingCoroutine;

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
        if(player == null){
            player = GameObject.FindWithTag("Player");
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
            playerCollider2D = player.GetComponent<Collider2D>();
            playerMove = player.GetComponent<PlayerMove>();
        }
    }

    public void Interact() {
        if(!enabled || stealthingCoroutine!=null) return;

        stealthingCoroutine = Stealth();
        StartCoroutine(stealthingCoroutine);
    }

    private IEnumerator Stealth() {
        float origin_speed = playerMove.speed;
        playerMove.speed = 0f;
        playerCollider2D.enabled = false;
        playerSpriteRenderer.enabled = false;

        while(true) {
            yield return null;
            if(Input.GetKeyDown(KeyCode.E)) break;
        }

        playerMove.speed = origin_speed;
        playerCollider2D.enabled = true;
        playerSpriteRenderer.enabled = true;
        stealthingCoroutine = null;
        yield break;
    }
}
