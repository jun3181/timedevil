using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D),typeof(Collider2D))]
public class NPCMover : MonoBehaviour
{
    [SerializeField]
    [Header("이동 속력")]
    private float speed = 1f;

    [SerializeField]
    [Header("충돌 무시 여부")]
    private bool ignoringCollision = true;

    protected Rigidbody2D rb2d;
    protected Collider2D cd2d;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private Vector2 velocityPerRoutine;
    private float estimatedTime;
    private float takingTime;

    private static readonly float coroutineIntervalTime = 0.02f;
    private static readonly WaitForSeconds coroutineIntervalWFS = new(coroutineIntervalTime);

    private IEnumerator movementCoroutine = null;
    protected virtual void Awake() {
        rb2d = GetComponent<Rigidbody2D>();
        cd2d = GetComponent<Collider2D>();
    }

    protected IEnumerator MoveTo(Vector2 dest) {
        if(movementCoroutine != null || speed==0) yield break;

        if(ignoringCollision)
            movementCoroutine = MoveIgnoringCollision();
        else
            movementCoroutine = Move();

        startPoint = rb2d.position;
        endPoint = dest;
        estimatedTime = (endPoint - startPoint).magnitude;
        velocityPerRoutine = (endPoint - startPoint).normalized * speed * coroutineIntervalTime;
        takingTime = 0f;

        yield return movementCoroutine;
    }

    protected IEnumerator Resume() {
        if(movementCoroutine != null || estimatedTime == 0f) yield break;

        if(ignoringCollision)
            movementCoroutine = MoveIgnoringCollision();
        else
            movementCoroutine = Move();

        yield return movementCoroutine;
    }

    protected void Stop() {
        movementCoroutine = null;
        estimatedTime = 0f;
    }

    protected void Idle() {
        movementCoroutine = null;
    }

    private IEnumerator MoveIgnoringCollision() {
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
        movementCoroutine = null;

        yield break;
    }

    private IEnumerator Move() {
        yield break;
    }
}