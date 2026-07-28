using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObstacleHideout : BaseHideout
{
    [SerializeField]
    [Range(0f, 1f)]
    [Header("은신한 플래이어의 투명화 정도")]
    private float transparentRatio = 0.7f;

    protected override void Awake() {
        base.Awake();

        cd2d.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player")) {
            RaiseStealthingEnterEvent(gameObject.name);

            Color new_color = playerSpriteRenderer.color;
            new_color.a = 1-transparentRatio;

            playerSpriteRenderer.color = new_color;
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if(other.CompareTag("Player")) {
            RaiseStealthingExitEvent(gameObject.name);

            Color new_color = playerSpriteRenderer.color;
            new_color.a = 1;

            playerSpriteRenderer.color = new_color;
        }
    }
}
