using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(AudioSource))]
public sealed class BoxHideout : BaseHideout, IInteractable
{
    [Header("상자가 열려있을 때 | 닫혀있을 때")]
    [SerializeField] private Sprite openedBoxSprite;
    [Tooltip("지정하지 않을 경우 현재 적용된 스프라이트 사용")]
    [SerializeField] private Sprite closedBoxSprite;

    [Header("상자가 열리고 닫히는 시간")]
    [SerializeField] private float changingBoxRoutineInterval = 1.5f;
    private WaitForSeconds changingBoxRoutineIntervalWFS;

    [Header("상자를 여는 소리")]
    [SerializeField] private AudioClip openingBoxSound;

    [SerializeField]
    [Header("디버그 메시지")]
    private bool debuged = false;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private IEnumerator stealthingRoutine;
    private IEnumerator changingBoxRoutine;

    protected override void Awake()
    {
        base.Awake();
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
        } else {
            if(spriteRenderer.sprite==null) {
                spriteRenderer.sprite = closedBoxSprite;
            }
        }
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = openingBoxSound;

        changingBoxRoutineIntervalWFS = new(changingBoxRoutineInterval);
    }

    public void Interact() {
        if(!enabled || stealthingRoutine!=null) return;

        stealthingRoutine = Stealth();
        StartCoroutine(stealthingRoutine);
    }

    private IEnumerator Stealth() {
        RaiseStealthingEnterEvent(gameObject.name);

        float origin_speed = playerMove.speed;
        playerMove.speed = 0f;
        playerCollider2D.enabled = false;
        playerSpriteRenderer.enabled = false;

        if(changingBoxRoutine!=null) {
            StopCoroutine(changingBoxRoutine);
        }
        changingBoxRoutine = ChangeBox();
        StartCoroutine(changingBoxRoutine);

        // Escape
        while(true) {
            yield return null;
            if(Input.GetKeyDown(KeyCode.E)) break;
        }

        if(changingBoxRoutine != null) {
            StopCoroutine(changingBoxRoutine);
        }
        changingBoxRoutine = ChangeBox();
        StartCoroutine(changingBoxRoutine);

        playerMove.speed = origin_speed;
        playerCollider2D.enabled = true;
        playerSpriteRenderer.enabled = true;
        stealthingRoutine = null;

        RaiseStealthingExitEvent(gameObject.name);
        yield break;
    }

    private IEnumerator ChangeBox() {
        if(spriteRenderer.sprite!=openedBoxSprite){
            spriteRenderer.sprite = openedBoxSprite;
            audioSource.Play();
        }

        yield return changingBoxRoutineIntervalWFS;

        spriteRenderer.sprite = closedBoxSprite;
        changingBoxRoutine = null;
    }
}
