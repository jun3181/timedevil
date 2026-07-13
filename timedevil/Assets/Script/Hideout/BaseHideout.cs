using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BaseHideout : MonoBehaviour
{
    public delegate void HideoutEventHandler();
    public static event HideoutEventHandler OnStealthingEnter;
    public static event HideoutEventHandler OnStealthingExit;
    
    public static bool Hiding
    {
        get; private set;
    }

    protected static GameObject player = null;
    protected static SpriteRenderer playerSpriteRenderer = null;
    protected static Collider2D playerCollider2D = null;
    protected static PlayerMove playerMove = null;

    protected Collider2D cd2d;

    protected virtual void Awake() {
        cd2d = GetComponent<Collider2D>();
    }

    protected virtual void OnEnable() {
        if(player == null) {
            player = GameObject.FindWithTag("Player");
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
            playerCollider2D = player.GetComponent<Collider2D>();
            playerMove = player.GetComponent<PlayerMove>();
        }
    }

    protected void RaiseStealthingEnterEvent(string name) {
        Hiding = true;
        OnStealthingEnter?.Invoke();
    }

    protected void RaiseStealthingExitEvent(string name) {
        Hiding = false;
        OnStealthingExit?.Invoke();
    }
}
