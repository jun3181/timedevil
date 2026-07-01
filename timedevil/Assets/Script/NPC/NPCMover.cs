using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D),typeof(Collider2D))]
public class NPCMover : MonoBehaviour
{
    [Header("이동 속력")]
    public float Speed = 1f;

    [Header("노클립")]
    public bool isNoclip = false;

    protected Rigidbody2D rb2d;
    protected Collider2D cd2d;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private Vector2 velocityPerRoutine;
    private float estimatedTime;
    private float takingTime;

    private static float coroutineIntervalTime = Time.fixedDeltaTime;
    private static WaitForSeconds coroutineIntervalWFS = new(coroutineIntervalTime);

    private IEnumerator movementCoroutine = null;
    protected virtual void Awake() {
        rb2d = GetComponent<Rigidbody2D>();
        cd2d = GetComponent<Collider2D>();
    }

    protected bool MoveTo(Vector2 dest) {
        if(movementCoroutine != null) return false;

        if(isNoclip)
            movementCoroutine = MoveNoclip();
        else
            movementCoroutine = Move();

        if(estimatedTime==0f) {
            startPoint = rb2d.position;
            endPoint = dest;
            estimatedTime = (endPoint - startPoint).magnitude;
            velocityPerRoutine = (endPoint - startPoint).normalized * Speed * coroutineIntervalTime;
            takingTime = 0f;
        }

        StartCoroutine(movementCoroutine);

        return true;
    }

    private IEnumerator MoveNoclip() {
        Vector2 nextPoint;
        while(true) {
            nextPoint = rb2d.position + velocityPerRoutine;
            takingTime = (nextPoint - startPoint).magnitude;
            if(takingTime<estimatedTime) {
                rb2d.MovePosition(nextPoint);
            } else {
                rb2d.MovePosition(endPoint);
                break;
            }
            yield return coroutineIntervalWFS;
        }

        estimatedTime = 0f;

        yield break;
    }

    private IEnumerator Move() {
        yield break;
    }
}