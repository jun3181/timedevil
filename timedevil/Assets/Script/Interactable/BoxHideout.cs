using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class BoxHideout : MonoBehaviour, IInteractable
{
    [Header("상자가 닫혔을 때 | 상자가 열렸을 때")]
    [SerializeField] private Sprite openedBoxSprite;
    [Tooltip("지정하지 않을 경우 현재 적용된 스프라이트 사용")]
    [SerializeField] private Sprite closedBoxSprite;

    [SerializeField]
    [Header("디버그 메시지")]
    private bool debuged = false;

    private SpriteRenderer spriteRenderer;

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
    }

    public void Interact() {
        Debug.Log($"{gameObject.name}은 발동되었습니다. State: {enabled}");
    }
}
