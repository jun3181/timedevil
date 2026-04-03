using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NPCMove : MonoBehaviour
{
    private static int NPCCount = 0;

    [Header("NPC 이동 속력")]
    public float Speed = 1f;

    public bool Moving { get; private set; }

    private Rigidbody2D rb;
    private int movementPriority;

    private Vector2 startPos;
    private Vector2 velocity;
    private float takingTime;
    void Awake() {
        movementPriority = ++NPCCount;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void FixedUpdate() {
        if(Moving) {
            Vector2 newPos = rb.position + Time.fixedDeltaTime * velocity;
            float takenTime = (newPos - startPos).magnitude / Speed;

            if(takenTime >= takingTime || newPos==rb.position) {
                rb.MovePosition(startPos + velocity * takingTime);
                takingTime = 0;
                Moving = false;
            } else {
                rb.MovePosition(newPos);
            }
        }
    }

    public void SetMovementPriority(int priority) {
        movementPriority = priority;
    }

    // 주어진 좌표 만큼 이동
    public void MoveBy(Vector2 offset) {
        if(Moving) return;

        startPos = rb.position;
        velocity = offset.normalized * Speed;
        takingTime = offset.magnitude / Speed;

        Moving = true;
    }

    // 주어진 좌표로 이동
    public void MoveTo(Vector2 pos) {
        MoveBy(pos - rb.position);
    }

    // NPC 일시정지
    public void Idle() {
        Moving = false;
    }

    // NPC 움직임 재게
    public void Resume() {
        if(Moving || takingTime==0) return;
        Moving = true;
    }
}
